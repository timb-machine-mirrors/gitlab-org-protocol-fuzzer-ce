﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.Runtime
{
	public class Worker : Program
	{
		string _pitLibraryPath;
		string _query;
		Guid? _guid;
		uint? _start;
		uint? _stop;
		uint? _seed;
		bool _init;
		bool? _test;

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
			options.Add(
				"logRoot=",
				"The root directory for output files",
				v => Configuration.LogRoot = v
			);
			options.Add(
				"test",
				"Run a single dry iteration to test a pit",
				v => _test = true
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

			Configuration.LogLevel = LogLevel;
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

			using (var db = new NodeDatabase())
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
					IsTest = _test.HasValue && _test.Value,
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
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(_guid.Value) ?? InitJob(pitFile);
			}

			var runner = new JobRunner(job, _pitLibraryPath, pitFile);
			var engineTask = Task.Factory.StartNew(runner.Run);
			Loop(runner, engineTask);
		}

		private void Loop(JobRunner runner, Task engineTask)
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
				}

				switch (readerTask.Result)
				{
					case "help":
						ShowHelp();
						break;
					case "stop":
						Console.WriteLine("OK");
						runner.Stop();
						engineTask.Wait();
						return;
					case "pause":
						Console.WriteLine("OK");
						runner.Pause();
						break;
					case "continue":
						Console.WriteLine("OK");
						runner.Continue();
						break;
					default:
						Console.WriteLine("Invalid command");
						break;
				}
			}
		}

		private void ShowHelp()
		{
			Console.WriteLine("Available commands:");
			Console.WriteLine("    help");
			Console.WriteLine("    stop");
			Console.WriteLine("    pause");
			Console.WriteLine("    continue");
		}

		protected override string UsageLine
		{
			get
			{
				var name = Assembly.GetEntryAssembly().GetName();
				return "Usage: {0} [OPTION]... <pit> [<name>]".Fmt(name.Name);
			}
		}

		private void RunQuery()
		{
			if (!_guid.HasValue)
				throw new SyntaxException("The '--guid' argument is required.");

			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(_guid.Value);
			}

			if (job == null || !File.Exists(job.DatabasePath))
				throw new Exception("Job not found");

			using (var db = new JobDatabase(job.DatabasePath))
			{
				switch (_query.ToLower())
				{
					case "states":
						Database.Dump(db.LoadTable<StateMetric>());
						break;
					case "iterations":
						Database.Dump(db.LoadTable<IterationMetric>());
						break;
					case "buckets":
						Database.Dump(db.LoadTable<BucketMetric>());
						break;
					case "buckettimeline":
						Database.Dump(db.LoadTable<BucketTimelineMetric>());
						break;
					case "mutators":
						Database.Dump(db.LoadTable<MutatorMetric>());
						break;
					case "elements":
						Database.Dump(db.LoadTable<ElementMetric>());
						break;
					case "datasets":
						Database.Dump(db.LoadTable<DatasetMetric>());
						break;
				}
			}
		}
	}
}
