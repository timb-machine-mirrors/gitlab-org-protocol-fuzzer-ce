using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Peach.Enterprise.WebServices.Models;
using System.Reflection;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Collections.Concurrent;

namespace Peach.Enterprise.WebServices
{
	public class PitTester
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private class EventCollector : Peach.Core.Watcher
		{
			private PitTester tester;

			public EventCollector(PitTester tester)
			{
				this.tester = tester;

				Status = TestStatus.Active;
				Events = new List<TestEvent>();
			}

			private void AddEvent(string name, string desc, TestStatus status = TestStatus.Active)
			{
				lock (tester.mutex)
				{
					Events.Add(new TestEvent()
					{
						Id = (uint)Events.Count,
						Status = TestStatus.Active,
						Short = name,
						Description = desc,
						Resolve = "",
					});
				}
			}

			#region Agent Events

			protected override void Agent_AgentConnect(Core.RunContext context, Core.Agent.AgentClient agent)
			{
				AddEvent("Connecting to agent", "Connecting to agent '" + agent.url + "'");
			}

			protected override void Agent_AgentDisconnect(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_CreatePublisher(Core.RunContext context, Core.Agent.AgentClient agent, string cls, Dictionary<string, Peach.Core.Variant> args)
			{
			}

			protected override void Agent_StartMonitor(Core.RunContext context, Core.Agent.AgentClient agent, string name, string cls, Dictionary<string, Peach.Core.Variant> args)
			{
				EventSuccess();
				AddEvent("Starting monitor", "Starting monitor '" + cls + "'");
			}

			protected override void Agent_StopAllMonitors(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_SessionStarting(Core.RunContext context, Core.Agent.AgentClient agent)
			{
				EventSuccess();
			}

			protected override void Agent_SessionFinished(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_IterationStarting(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_IterationFinished(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_DetectedFault(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_GetMonitorData(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_MustStop(Core.RunContext context, Core.Agent.AgentClient agent)
			{
			}

			protected override void Agent_Message(Core.RunContext context, Core.Agent.AgentClient agent, string name, Peach.Core.Variant data)
			{
			}

			#endregion


			protected override void Engine_TestStarting(Core.RunContext context)
			{
				AddEvent("Starting fuzzing engine", "Starting fuzzing engine");

				// Before we get iteration start, we will get AgentConnect & SessionStart
			}

			protected override void Engine_TestFinished(Core.RunContext context)
			{
			}

			protected override void Engine_IterationStarting(Core.RunContext context, uint currentIteration, uint? totalIterations)
			{
				lock (tester.mutex)
				{
					// Pass all events up to now
					foreach (var e in Events)
						e.Status = TestStatus.Pass;

					// Add event for the iteration running
					Events.Add(new TestEvent()
					{
						Id = (uint)Events.Count,
						Status = TestStatus.Active,
						Short = "Running iteration",
						Description = "Running the initial control record iteration",
						Resolve = "",
					});
				}
			}

			protected override void Engine_IterationFinished(Core.RunContext context, uint currentIteration)
			{
			}

			protected override void StateModelStarting(Core.RunContext context, Core.Dom.StateModel model)
			{
			}

			protected override void StateModelFinished(Core.RunContext context, Core.Dom.StateModel model)
			{
			}

			private void EventSuccess()
			{
				lock (tester.mutex)
				{
					Events[Events.Count - 1].Status = TestStatus.Pass;
				}
			}

			private void EventFail(string resolve)
			{
				lock (tester.mutex)
				{
					var last = Events[Events.Count - 1];

					last.Status = TestStatus.Fail;
					last.Resolve = resolve;
				}
			}

			private Dictionary<string, object> ParseConfig()
			{
				var args = new Dictionary<string, object>();
				var pitConfig = tester.pitFile + ".config";

				// It is ok if a .config doesn't exist
				if (System.IO.File.Exists(pitConfig))
				{
					try
					{
						AddEvent("Loading pit config", "Loading configuration file '" + pitConfig + "'");

						var defs = PitDatabase.ParseConfig(tester.pitLibraryPath, pitConfig);
						args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

						EventSuccess();
					}
					catch (Exception ex)
					{
						EventFail(ex.Message);

						throw;
					}
				}
				else
				{
					// ParseConfig allows non-existant config files
					var defs = PitDatabase.ParseConfig(tester.pitLibraryPath, pitConfig);
					args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;
				}

				return args;
			}

			public Peach.Core.Dom.Dom ParsePit()
			{
				var args = ParseConfig();
				var parser = new Godel.Core.GodelPitParser();

				AddEvent("Loading pit file", "Loading pit file '" + tester.pitFile + "'");

				try
				{
					var dom = parser.asParser(args, tester.pitFile);

					EventSuccess();

					return dom;
				}
				catch (Exception ex)
				{
					EventFail(ex.Message);

					throw;
				}
			}

			public TestStatus Status { get; set; }
			public List<TestEvent> Events { get; set; }
		}

		private class LoggingTarget : TargetWithLayout
		{
			private BlockingCollection<string> logs = new BlockingCollection<string>();

			public IEnumerable<string> Logs
			{
				get
				{
					return logs;
				}
			}

			protected override void Write(LogEventInfo logEvent)
			{
				var msg = this.Layout.Render(logEvent);

				logs.Add(msg);
			}
		}

		private object mutex;
		private EventCollector watcher;
		private string pitLibraryPath;
		private string pitFile;
		private Thread thread;
		private LoggingTarget target;

		// Filter these loggers to the info level since they are spammy at debug
		private static string[] FilteredLoggers =
		{
			"Peach.Core.Dom.Array",
			"Peach.Core.Dom.Choice",
			"Peach.Core.Dom.DataElement",
			"Peach.Core.Cracker.DataCracker",
		};

		private void Run()
		{
			try
			{
				var nconfig = new LoggingConfiguration();
				nconfig.AddTarget("target", target);

				// Disable the data cracker logs to keep the size of the test log down
				foreach (var item in FilteredLoggers)
				{
					var r = new LoggingRule(item, LogLevel.Info, target) { Final = true };
					nconfig.LoggingRules.Add(r);
				}

				var rule = new LoggingRule("*", LogLevel.Debug, target);
				nconfig.LoggingRules.Add(rule);

				LogManager.Configuration = nconfig;

				var dom = watcher.ParsePit();
				var e = new Peach.Core.Engine(watcher);
				var config = new Peach.Core.RunConfiguration()
				{
					// Run pit for a single iteration
					singleIteration = true,
					pitFile = pitFile,
				};

				e.startFuzzing(dom, config);

				lock (mutex)
				{
					// Pass the last event
					watcher.Events[watcher.Events.Count - 1].Status = TestStatus.Pass;
					// Pass the whole test
					watcher.Status = TestStatus.Pass;
				}
			}
			catch (Exception ex)
			{
				lock (mutex)
				{
					foreach (var item in watcher.Events)
					{
						// Fail any active items
						if (item.Status == TestStatus.Active)
							item.Status = TestStatus.Fail;
						//Fail the whole test
						watcher.Status = TestStatus.Fail;
					}
				}

				logger.Error(ex.Message);
			}
			finally
			{
				LogManager.Flush();
				LogManager.Configuration = null;

				thread = null;
			}
		}

		public PitTester(string pitLibraryPath, string pitFile)
		{
			Guid = System.Guid.NewGuid().ToString();

			this.mutex = new object();
			this.watcher = new EventCollector(this);
			this.pitLibraryPath = pitLibraryPath;
			this.pitFile = pitFile;
			this.target = new LoggingTarget() { Layout = "${logger} ${message}" };
			this.thread = new Thread(Run);

			thread.Start();
		}

		public string Guid
		{
			get;
			private set;
		}

		public TestStatus Status
		{
			get
			{
				lock (mutex)
				{
					return watcher.Status;
				}
			}
		}

		public TestResult Result
		{
			get
			{
				lock (mutex)
				{
					return new TestResult()
					{
						Status = watcher.Status,
						Events = watcher.Events.ToList(),
						Log = string.Join("\n", Log),
					};
				}
			}
		}

		public IEnumerable<string> Log
		{
			get
			{
				return target.Logs;
			}
		}
	}
}
