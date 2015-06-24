﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.Core.Storage;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;

namespace Peach.Pro.Test.Core.Runtime
{
	[TestFixture]
	[Peach]
	[Quick]
	class JobRunnerTests
	{
		TempDirectory _tmpDir;
		TempFile _tmp;
		LoggingConfiguration _loggingConfig;

		const string PitXml =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>

	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempFile();
			File.WriteAllText(_tmp.Path, PitXml);

			_tmpDir = new TempDirectory();
			Configuration.LogRoot = _tmpDir.Path;

			_loggingConfig = LogManager.Configuration;

			var target = new ConsoleTarget
			{
				Layout = "${time} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			var rule = new LoggingRule("*", LogLevel.Trace, target);
			config.AddTarget("console", target);
			config.LoggingRules.Add(rule);
			LogManager.Configuration = config;
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
			_tmp.Dispose();

			LogManager.Configuration = _loggingConfig;
		}

		class SafeRunner : IDisposable
		{
			readonly Job _job;
			public JobRunner JobRunner { get; private set; }
			readonly Thread _thread;
			Exception _caught;

			public SafeRunner(string xmlFile, JobRequest jobRequest)
			{
				var evtReady = new AutoResetEvent(false);
				_job = new Job(jobRequest, xmlFile);
				JobRunner = new JobRunner(_job, "", xmlFile);
				_thread = new Thread(() =>
				{
					try
					{
						JobRunner.Run(evtReady);
					}
					catch (Exception ex)
					{
						if (ex.GetBaseException() is ThreadAbortException)
							Thread.ResetAbort();
						_caught = ex;
					}
				});
				_thread.Start();
				if (!evtReady.WaitOne(1000))
					throw new PeachException("Timeout waiting for job to start");
			}

			public Guid Id { get { return _job.Guid; } }

			public void WaitUntil(params JobStatus[] status)
			{
				Console.WriteLine("WaitUntil({0})", string.Join(",", status));

				// waits up to 20 seconds
				for (var i = 0; i < 40; i++)
				{
					var job = GetJob();
					Assert.IsNotNull(job);
					if (status.Contains(job.Status))
						return;

					Thread.Sleep(500);
				}
				Console.WriteLine("Timeout");
				Assert.Fail("Timeout");
			}

			public void WaitForFinish()
			{
				Console.WriteLine("Waiting for finish");
				var ret = _thread.Join(TimeSpan.FromSeconds(20));
				Console.WriteLine("Done");
				Assert.IsTrue(ret, "Timeout waiting for job to finish");
				Assert.AreEqual(JobStatus.Stopped, GetJob().Status);
				if (_caught != null)
					throw new AggregateException(_caught);
			}

			public Job GetJob()
			{
				using (var db = new NodeDatabase())
				{
					var job = db.GetJob(Id);
					Assert.IsNotNull(job);
					return job;
				}
			}

			public void VerifyDatabase(int expectedLogs)
			{
				using (var db = new NodeDatabase())
				{
					var job = db.GetJob(Id);
					Assert.IsNotNull(job);

					var logs = db.GetJobLogs(job.Guid).ToList();
					Assert.AreEqual(expectedLogs, logs.Count, "JobLog mismatch");
				}
			}

			public void Dispose()
			{
				_thread.Abort();
			}
		}

		[Test]
		public void TestBasic()
		{
			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};
			using (var runner = new SafeRunner(_tmp.Path, jobRequest))
			{
				runner.WaitForFinish();
				runner.VerifyDatabase(1);
			}
		}

		[Test]
		public void TestStop()
		{
			var jobRequest = new JobRequest();
			using (var runner = new SafeRunner(_tmp.Path, jobRequest))
			{
				runner.WaitUntil(JobStatus.Running);
				Console.WriteLine("Stop");
				runner.JobRunner.Stop();
				runner.WaitForFinish();
				Console.WriteLine("VerifyDatabase");
				runner.VerifyDatabase(0);
			}
		}

		[Test]
		public void TestPauseContinue()
		{
			var jobRequest = new JobRequest();
			using (var runner = new SafeRunner(_tmp.Path, jobRequest))
			{
				runner.WaitUntil(JobStatus.Running);
				runner.JobRunner.Pause();
				runner.WaitUntil(JobStatus.Paused);
				runner.JobRunner.Continue();
				runner.WaitUntil(JobStatus.Running);
				runner.JobRunner.Stop();
				runner.WaitForFinish();
				runner.VerifyDatabase(0);
			}
		}

		[Test]
		[Repeat(2)]
		public void TestPitTester()
		{
			var jobRequest = new JobRequest
			{
				IsControlIteration = true,
			};
			using (var runner = new SafeRunner(_tmp.Path, jobRequest))
			{
				runner.WaitForFinish();

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(runner.Id), new[]
					{
						new TestEvent(1, runner.Id, TestStatus.Pass, 
							"Loading pit file", "Loading pit file '{0}'".Fmt(_tmp.Path), null),
						new TestEvent(2, runner.Id, TestStatus.Pass, 
							"Starting fuzzing engine", "Starting fuzzing engine", null),
						new TestEvent(3, runner.Id, TestStatus.Pass, 
							"Running iteration", "Running the initial control record iteration", null),
						new TestEvent(4, runner.Id, TestStatus.Pass, 
							"Flushing logs.", "Flushing logs.", null),
					});
				}

				var job = runner.GetJob();
				Assert.IsTrue(File.Exists(job.DebugLogPath));
				Console.Write(File.ReadAllText(job.DebugLogPath));
			}
		}

		[Test]
		public void TestPitParseFailure()
		{
			using (var xmlFile = new TempFile())
			{
				File.WriteAllText(xmlFile.Path, PitXml + "xxx");

				var jobRequest = new JobRequest
				{
					IsControlIteration = true,
				};
				using (var runner = new SafeRunner(xmlFile.Path, jobRequest))
				{
					runner.WaitForFinish();

					using (var db = new NodeDatabase())
					{
						DatabaseTests.AssertResult(db.GetTestEventsByJob(runner.Id), new[]
						{
							new TestEvent(
								1, 
								runner.Id, 
								TestStatus.Fail, 
								"Loading pit file", 
								"Loading pit file '{0}'".Fmt(xmlFile.Path), 
#if MONO
								"Error: XML Failed to load: Text node cannot appear in this state.  Line 21, position 1."),
#else
								"Error: XML Failed to load: Data at the root level is invalid. Line 21, position 1."),
#endif
							new TestEvent(
								2, 
								runner.Id, 
								TestStatus.Pass, 
								"Flushing logs.", 
								"Flushing logs.", 
								null),
						});

						var logs = db.GetJobLogs(runner.Id).ToList();
						Assert.AreEqual(2, logs.Count, "Missing JobLogs");
						foreach (var log in logs)
							Console.WriteLine(log.Message);
					}

					var job = runner.GetJob();
					Assert.IsFalse(File.Exists(job.DebugLogPath));
				}
			}
		}
	}
}
