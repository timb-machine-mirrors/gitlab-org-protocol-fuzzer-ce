using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.Runtime
{
	public class Worker : Program
	{
		string _pitLibraryPath;
		string _query;
		Guid? _guid;
		bool _shouldStop;
		readonly RunConfiguration _config = new RunConfiguration();
		readonly ManualResetEvent _pausedEvt = new ManualResetEvent(true);
		uint? _start;
		uint? _stop;
		uint? _seed;

		public Worker()
		{
			_config.shouldStop = ShouldStop;
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add(
				"pits=",
				"The path to the pit library",
				v => _pitLibraryPath = v
			);
			options.Add(
				"guid=",
				"The guid that identifies a job",
				(Guid v) => _guid = v
			);
			options.Add(
				"seed=",
				"The seed used by the random number generator",
				(uint v) => _seed = v
			);
			options.Add(
				"start=",
				"The iteration to start fuzzing",
				(uint v) => _start = v
			);
			options.Add(
				"stop=",
				"The iteration to stop fuzzing",
				(uint v) => _stop = v
			);
			options.Add(
				"query=",
				v => _query = v
			);
		}

		protected override void ConfigureLogging()
		{
			// Override logging so that we force messages to stderr instead of stdout
			var consoleTarget = new ConsoleTarget
			{
				Layout = "${logger} ${message}",
				Error = true,
			};
			var rule = new LoggingRule("*", LogLevel, consoleTarget);

			var nconfig = new LoggingConfiguration();
			nconfig.RemoveTarget("console");
			nconfig.AddTarget("console", consoleTarget);
			nconfig.LoggingRules.Add(rule);
			LogManager.Configuration = nconfig;
		}

		protected override void OnRun(List<string> args)
		{
			if (!_guid.HasValue)
				throw new SyntaxException("The '--guid' argument is required.");

			if (!string.IsNullOrEmpty(_query))
			{
				RunQuery();
				return;
			}

			if (string.IsNullOrEmpty(_pitLibraryPath))
				throw new SyntaxException("The '--pits' argument is required.");

			if (args.Count == 0)
				throw new SyntaxException("Missing <pit> argument.");

			_config.pitFile = args.First();

			Job job;

			if (_start.HasValue)
				job = CreateJobFromArgs();
			else
				job = ResumeJob();

			RunJob(job);
		}

		Job CreateJobFromArgs()
		{
			if (_seed.HasValue)
				_config.randomSeed = _seed.Value;

			if (_stop.HasValue)
			{
				_config.range = true;
				_config.rangeStart = _start.Value;
				_config.rangeStop = _stop.Value;
			}
			else
			{
				_config.skipToIteration = _start.Value;
			}

			var job = new Job
			{
				Id = _guid.Value,
				Name = Path.GetFileNameWithoutExtension(_config.pitFile),
				RangeStart = _config.rangeStart,
				RangeStop = _config.rangeStop,
				IterationCount = 0,
				Seed = _config.randomSeed,
				StartDate = DateTime.UtcNow,
				HasMetrics = true,
			};

			using (var db = new NodeDatabase())
			{
				db.InsertJob(job);
			}

			return job;
		}

		Job ResumeJob()
		{
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(_guid.Value);

				if (job.Seed.HasValue)
					_config.randomSeed = (uint)job.Seed.Value;

				if (job.RangeStop.HasValue)
				{
					_config.range = true;
					_config.rangeStart = (uint)job.RangeStart + (uint)job.IterationCount;
					_config.rangeStop = (uint)job.RangeStop.Value;
				}
				else
				{
					_config.skipToIteration = (uint)job.RangeStart + (uint)job.IterationCount;
				}

				return job;
			}
		}

		protected override string UsageLine
		{
			get
			{
				var name = Assembly.GetEntryAssembly().GetName();
				return "Usage: {0} [OPTION]... <pit> [<name>]".Fmt(name.Name);
			}
		}

		private bool ShouldStop()
		{
			_pausedEvt.WaitOne();
			return _shouldStop;
		}

		public void RunJob(Job job)
		{
			var pitConfig = _config.pitFile + ".config";
			var defs = PitDatabase.ParseConfig(_pitLibraryPath, pitConfig);
			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(args, _config.pitFile);

			// REVIEW: is this required?
			foreach (var test in dom.tests)
			{
				test.loggers.Clear();
			}

			var engine = new Engine(new JobWatcher(job));
			var engineTask = Task.Factory.StartNew(() => engine.startFuzzing(dom, _config));

			Loop(engineTask);

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
				{
					// this causes any unhandled exceptions to be thrown
					engineTask.Wait();
					return;
				}

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
			using (var db = new JobDatabase(_guid.Value))
			{
				switch (_query.ToLower())
				{
					case "states":
						Database.Dump(db.QueryStates());
						break;
					case "iterations":
						Database.Dump(db.QueryIterations());
						break;
					case "buckets":
						Database.Dump(db.QueryBuckets());
						break;
					case "buckettimeline":
						Database.Dump(db.QueryBucketTimeline());
						break;
					case "mutators":
						Database.Dump(db.QueryMutators());
						break;
					case "elements":
						Database.Dump(db.QueryElements());
						break;
					case "datasets":
						Database.Dump(db.QueryDatasets());
						break;
				}
			}
		}
	}
}
