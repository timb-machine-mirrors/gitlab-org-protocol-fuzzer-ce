using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Runtime;
using Peach.Pro.Core.WebServices;
using System.IO;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.Runtime.Enterprise
{
	public class Worker
	{
		OptionSet _options;
		string _pitLibraryPath;
		string _query;
		Guid? _guid;
		bool _shouldStop;
		Exception _caught;
		readonly RunConfiguration _config = new RunConfiguration();
		readonly ManualResetEvent _pausedEvt = new ManualResetEvent(true);

		public Worker()
		{
			_config.shouldStop = ShouldStop;
		}

		public int Run(string[] args)
		{
			try
			{
				_Run(args);
				return 0;
			}
			catch (OptionException ex)
			{
				return ReportError(true, ex);
			}
			catch (SyntaxException ex)
			{
				return ReportError(ex.ShowUsage, ex);
			}
			catch (Exception ex)
			{
				if (GetLogLevel() == LogLevel.Trace)
					Console.Error.WriteLine(ex.InnerException ?? ex);
				else
					Console.Error.WriteLine(ex.InnerException != null ?
						ex.InnerException.Message : ex.Message);
				return 1;
			}
		}

		private int ReportError(bool showUsage, Exception ex)
		{
			if (!string.IsNullOrEmpty(ex.Message))
			{
				Console.Error.WriteLine(ex.Message);
				Console.Error.WriteLine();
			}
			if (showUsage)
				ShowUsage();
			return string.IsNullOrEmpty(ex.Message) ? 0 : 2;
		}

		private LogLevel GetLogLevel()
		{
			switch (_config.debug)
			{
				case 0:
					return LogLevel.Info;
				case 1:
					return LogLevel.Debug;
				default:
					return LogLevel.Trace;
			}
		}

		private void _Run(IEnumerable<string> args)
		{
			AssertWriter.Register();

			if (!Runtime.Program.VerifyCompatibility())
				throw new PeachException("");

			uint? start = null;
			uint? stop = null;
			uint? seed = null;

			_options = new OptionSet
			{
				{
					"h|help", 
					"Display this help and exit",
					v => { throw new SyntaxException(true); }
				},
				{
					"V|version", 
					"Display version information and exit",
					v => ShowVersion()
				},
				{
					"v|verbose", 
					"Increase verbosity, can use multiple times",
					v => _config.debug++
				},
				{ 
					"pits=", 
					"The path to the pit library",
					v => _pitLibraryPath = v
				},
				{ 
					"guid=", 
					"The guid that identifies a job",
					(Guid v) => _guid = v
				},
				{ 
					"seed=", 
					"The seed used by the random number generator",
					(uint v) => seed = v
				},
				{ 
					"start=", 
					"The iteration to start fuzzing",
					(uint v) => start = v
				},
				{ 
					"stop=", 
					"The iteration to stop fuzzing",
					(uint v) => stop = v
				},
				{
					"query=",
					v => _query = v
				},
			};

			var extra = _options.Parse(args);

			// Override logging so that we force messages to stderr instead of stdout
			var consoleTarget = new ConsoleTarget
			{
				Layout = "${logger} ${message}",
				Error = true,
			};
			var rule = new LoggingRule("*", GetLogLevel(), consoleTarget);

			var nconfig = new LoggingConfiguration();
			nconfig.RemoveTarget("console");
			nconfig.AddTarget("console", consoleTarget);
			nconfig.LoggingRules.Add(rule);
			LogManager.Configuration = nconfig;

			// Load the platform assembly
			Runtime.Program.LoadPlatformAssembly();

			if (string.IsNullOrEmpty(_pitLibraryPath))
				throw new SyntaxException("The '--pits' argument is required.");
			if (!seed.HasValue)
				throw new SyntaxException("The '--seed' argument is required.");
			if (!start.HasValue)
				throw new SyntaxException("The '--start' argument is required.");

			if (!_guid.HasValue)
				_guid = Guid.NewGuid();

			if (stop.HasValue)
			{
				_config.range = true;
				_config.rangeStart = start.Value;
				_config.rangeStop = stop.Value;
			}
			else
			{
				_config.skipToIteration = start.Value;
			}

			if (!string.IsNullOrEmpty(_query))
			{
				RunQuery();
			}
			else
			{
				RunJob(extra);
			}
		}

		private void ShowUsage()
		{
			var name = Assembly.GetEntryAssembly().GetName();

			var usage = new[]
			{
				"ShowUsage: {0} [OPTION]... <pit> [<name>]".Fmt(name.Name),
				"Valid options:",
			};

			Console.WriteLine(string.Join(Environment.NewLine, usage));
			_options.WriteOptionDescriptions(Console.Out);
		}

		private void ShowVersion()
		{
			var name = Assembly.GetEntryAssembly().GetName();
			Console.WriteLine("{0}: ShowVersion {1}".Fmt(name.Name, name.Version));
			throw new SyntaxException();
		}

		private bool ShouldStop()
		{
			_pausedEvt.WaitOne();
			return _shouldStop;
		}

		public void RunJob(IList<string> extra)
		{
			if (extra.Count == 0)
				throw new SyntaxException("Missing <pit> argument.");

			if (extra.Count > 0)
				_config.pitFile = extra[0];

			if (extra.Count > 1)
				_config.runName = extra[1];

			var pitConfig = _config.pitFile + ".config";
			var defs = PitDatabase.ParseConfig(_pitLibraryPath, pitConfig);
			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(args, _config.pitFile);

			foreach (var test in dom.tests)
			{
				test.loggers.Clear();
			}

			var engine = new Engine(new JobWatcher(_guid.Value));
			var engineTask = Task.Factory.StartNew(() =>
			{
				try
				{
					engine.startFuzzing(dom, _config);
				}
				catch (Exception ex)
				{
					_caught = ex;
				}
			});

			Loop(engineTask);

			if (_caught != null)
				throw new Exception("Engine exception", _caught);

			Console.WriteLine("Done");
		}

		private void Loop(Task engineTask)
		{
			while (true)
			{
				Console.Write("> ");
				var readerTask = Task.Factory.StartNew<string>(Console.ReadLine);
				var index = Task.WaitAny(engineTask, readerTask);
				if (index == 0)
					return;

				switch (readerTask.Result)
				{
					case "help":
						ShowShellHelp();
						break;
					case "stop":
						Console.WriteLine("OK");
						_shouldStop = true;
						_pausedEvt.Set();
						engineTask.Wait();
						return;
					case "pause":
						Console.WriteLine("OK");
						_pausedEvt.Reset();
						break;
					case "continue":
						Console.WriteLine("OK");
						_pausedEvt.Set();
						break;
					default:
						Console.WriteLine("Invalid command");
						break;
				}
			}
		}

		private void ShowShellHelp()
		{
			Console.WriteLine("Available commands:");
			Console.WriteLine("    help");
			Console.WriteLine("    stop");
			Console.WriteLine("    pause");
			Console.WriteLine("    continue");
		}

		private void RunQuery()
		{
			var logRoot = Utilities.GetAppResourcePath("db");
			var logPath = Path.Combine(logRoot, _guid.ToString());
			var dbPath = Path.Combine(logPath, "peach.db");

			using (var db = new JobContext(dbPath))
			{
				switch (_query.ToLower())
				{
					case "states":
						db.QueryStates();
						break;
					case "iterations":
						db.QueryIterations();
						break;
					case "buckets":
						db.QueryBuckets();
						break;
					case "buckettimeline":
						db.QueryBucketTimeline();
						break;
					case "mutators":
						db.QueryMutators();
						break;
					case "elements":
						db.QueryElements();
						break;
					case "datasets":
						db.QueryDatasets();
						break;
				}
			}
		}
	}
}
