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
		readonly RunConfiguration _config = new RunConfiguration();
		uint? _start;
		uint? _stop;
		uint? _seed;
		bool _init;
		JobWatcher _watcher;

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
				"init",
				"Initialize a new job",
				v => _init = true
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
			if (!string.IsNullOrEmpty(_query))
			{
				RunQuery();
				return;
			}

			if (args.Count == 0)
				throw new SyntaxException("Missing <pit> argument.");

			if (_init)
				InitJob(args.First());
			else
				RunJob(args.First());
		}

		Job InitJob(string pitFile)
		{
			if (!_guid.HasValue)
				_guid = Guid.NewGuid();

			using (var db = new JobDatabase(_guid.Value))
			{
				var job = db.GetJob(_guid.Value);
				if (job != null)
					throw new Exception("Job has already been initialized.");

				// this code should be identical to JobRunner.Start()
				job = new Job
				{
					Guid = _guid.Value,
					Name = Path.GetFileNameWithoutExtension(pitFile),
					StartDate = DateTime.UtcNow,
					Status = JobStatus.StartPending,
					Mode = JobMode.Starting,

					RangeStart = _start.HasValue ? _start.Value : 0,
					RangeStop = _stop,
					Seed = _seed,
				};
				db.InsertJob(job);
				return job;
			}
		}

		void RunJob(string pitFile)
		{
			if (!_guid.HasValue)
				_guid = Guid.NewGuid();

			Job job;
			using (var db = new JobDatabase(_guid.Value))
			{
				job = db.GetJob(_guid.Value) ?? InitJob(pitFile);
			}

			_config.pitFile = pitFile;

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

			_watcher = new JobWatcher(job, _config);
			var engine = new Engine(_watcher);
			var engineTask = Task.Factory.StartNew(() => engine.startFuzzing(dom, _config));

			Loop(engineTask);
		}

		protected override string UsageLine
		{
			get
			{
				var name = Assembly.GetEntryAssembly().GetName();
				return "Usage: {0} [OPTION]... <pit> [<name>]".Fmt(name.Name);
			}
		}

		private void Loop(Task engineTask)
		{
			Console.WriteLine("OK");

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
						_watcher.Stop();
						engineTask.Wait();
						return;
					case "pause":
						Console.WriteLine("OK");
						_watcher.Pause();
						break;
					case "continue":
						Console.WriteLine("OK");
						_watcher.Continue();
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
			if (!_guid.HasValue)
				throw new SyntaxException("The '--guid' argument is required.");

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
