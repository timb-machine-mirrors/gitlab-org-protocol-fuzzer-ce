﻿using System;
using System.Diagnostics;
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
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.Core.Storage;
using Logger = NLog.Logger;
using Random = System.Random;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;

namespace Peach.Pro.Test.Core.WebServices
{
	class BaseJobMonitorTests<T> where T : IJobMonitor, new()
	{
		protected IJobMonitor _monitor;
		protected ManualResetEvent _doneEvt;

		protected const string PitXml =
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

		protected bool WaitUntil(params JobStatus[] status)
		{
			// waits up to 20 seconds
			for (var i = 0; i < 200; i++)
			{
				var job = _monitor.GetJob();
				Assert.IsNotNull(job);
				if (status.Contains(job.Status))
					return true;

				Thread.Sleep(100);
			}
			return false;
		}

		protected void WaitForFinish()
		{
			Console.WriteLine("Waiting for finish");
			Assert.IsTrue(_doneEvt.WaitOne(TimeSpan.FromSeconds(20)), "Timeout waiting for job to finish");
		}

		protected void VerifyDatabase(Job job, bool required = true)
		{
			using (var db = new NodeDatabase())
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}

			if (required)
			{
				Assert.IsTrue(File.Exists(job.DatabasePath));
			}
		}
	}

	[TestFixture(typeof(InternalJobMonitor))]
	[TestFixture(typeof(ExternalJobMonitor))]
	[Peach]
	[Quick]
	class JobMonitorTests<T> : BaseJobMonitorTests<T>
		where T : IJobMonitor, new()
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected TempFile _tmp;
		protected TempDirectory _tmpDir;
		private LoggingConfiguration _loggingConfig;
		private LogLevel _oldLogLevel;

		[SetUp]
		public void SetUp()
		{
			Logger.Trace(">>> Setup");

			_oldLogLevel = Configuration.LogLevel;
			Configuration.LogLevel = LogLevel.Debug;

			Configuration.UseAsyncLogging = false;

			var target = new ConsoleTarget
			{
				Layout = "${time} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			var rule = new LoggingRule("*", LogLevel.Trace, target);
			config.AddTarget("console", target);
			config.LoggingRules.Add(rule);
			LogManager.Configuration = config;

			_doneEvt = new ManualResetEvent(false);

			_monitor = new T
			{
				InternalEvent = (s, a) =>
				{
					Logger.Trace("InternalEvent");
					_doneEvt.Set();
				}
			};

			_tmp = new TempFile();
			File.WriteAllText(_tmp.Path, PitXml);

			_tmpDir = new TempDirectory();
			Configuration.LogRoot = _tmpDir.Path;

			_loggingConfig = LogManager.Configuration;

			Logger.Trace("<<< Setup");
		}

		[TearDown]
		public void TearDown()
		{
			Logger.Trace(">>> TearDown");

			_monitor.Dispose();
			_tmp.Dispose();
			_tmpDir.Dispose();

			LogManager.Configuration = _loggingConfig;
			Configuration.UseAsyncLogging = true;
			Configuration.LogLevel = _oldLogLevel;

			Logger.Trace("<<< TearDown");
		}

		[Test]
		[Repeat(10)]
		public void TestBasic()
		{
			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.IsNotNull(job.DatabasePath);
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			VerifyDatabase(job);
		}

		[Test]
		[Repeat(10)]
		public void TestStop()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var duration = new Random().Next(1000);
			Logger.Trace("Sleep: {0}ms", duration);
			Thread.Sleep(duration);

			_monitor.Stop();

			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			VerifyDatabase(job, false);
		}

		[Test]
		public void TestPauseContinue()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.IsTrue(_monitor.Pause());
			Assert.IsTrue(WaitUntil(JobStatus.Paused), "Timeout waiting for Paused");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			Assert.IsTrue(_monitor.Continue());
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			VerifyDatabase(job);
		}

		[Test]
		[Repeat(30)]
		public void TestKill()
		{
			Configuration.LogLevel = LogLevel.Trace;
			Logger.Trace("TestKill");

			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var duration = new Random().Next(1000);
			Logger.Trace("Sleep: {0}ms", duration);
			Thread.Sleep(duration);

			Assert.IsTrue(_monitor.Kill());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			VerifyDatabase(job, false);
		}

		[Test]
		[Repeat(2)]
		public void TestPitTester()
		{
			var jobRequest = new JobRequest
			{
				DryRun = true,
			};

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);

				DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
				{
					new TestEvent(1, job.Guid, TestStatus.Pass, "Loading pit file", 
						"Loading pit file '{0}'".Fmt(_tmp.Path), null),
					new TestEvent(2, job.Guid, TestStatus.Pass, "Starting fuzzing engine", 
						"Starting fuzzing engine", null),
					new TestEvent(3, job.Guid, TestStatus.Pass, "Running iteration", 
						"Running the initial control record iteration", null),
					new TestEvent(4, job.Guid, TestStatus.Pass, 
						"Flushing logs.", "Flushing logs.", null),
				});
			}

			Assert.IsTrue(File.Exists(job.DebugLogPath));
			Console.Write(File.ReadAllText(job.DebugLogPath));
		}

		[Test]
		public void TestPitParseFailureDuringTest()
		{
			using (var xmlFile = new TempFile())
			{
				File.WriteAllText(xmlFile.Path, PitXml + "xxx");

				var jobRequest = new JobRequest
				{
					DryRun = true,
				};

				var job = _monitor.Start(xmlFile.Path, xmlFile.Path, jobRequest);
				Assert.IsNotNull(job);
				WaitForFinish();

				job = _monitor.GetJob();
				Assert.AreEqual(JobStatus.Stopped, job.Status);

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
					{
						new TestEvent(
							1, 
							job.Guid, 
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
								job.Guid, 
								TestStatus.Pass, 
								"Flushing logs.", 
								"Flushing logs.", 
								null),
					});

					job = db.GetJob(job.Guid);
					Assert.IsNotNull(job);

					var logs = db.GetJobLogs(job.Guid).ToList();
					Assert.AreEqual(2, logs.Count, "Missing JobLogs");
					foreach (var log in logs)
						Console.WriteLine(log.Message);
				}

				Assert.IsFalse(File.Exists(job.DatabasePath), "job.DatabasePath should not exist");
				Assert.IsFalse(File.Exists(job.DebugLogPath), "job.DebugLogPath should not exist");
			}
		}

		[Test]
		public void TestPitParseFailureDuringRun()
		{
			using (var xmlFile = new TempFile())
			{
				File.WriteAllText(xmlFile.Path, PitXml + "xxx");

				var jobRequest = new JobRequest();
				var job = _monitor.Start(xmlFile.Path, xmlFile.Path, jobRequest);
				Assert.IsNotNull(job);
				WaitForFinish();

				job = _monitor.GetJob();
				Assert.AreEqual(JobStatus.Stopped, job.Status);

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
					{
						 new TestEvent(
							1, 
							job.Guid, 
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
								job.Guid, 
								TestStatus.Pass, 
								"Flushing logs.", 
								"Flushing logs.", 
								null),
					});

					job = db.GetJob(job.Guid);
					Assert.IsNotNull(job);

					var logs = db.GetJobLogs(job.Guid).ToList();
					Assert.AreEqual(2, logs.Count, "Missing JobLogs");
					foreach (var log in logs)
						Console.WriteLine(log.Message);
				}

				Assert.IsFalse(File.Exists(job.DatabasePath), "job.DatabasePath should not exist");
				Assert.IsFalse(File.Exists(job.DebugLogPath), "job.DebugLogPath should not exist");
			}
		}

		[Test]
		public void TestPid()
		{
			// The pid should always be set to the pid of the process that controls
			// the engine.  For the InternalJobMonitor it will be the same process
			// as the engine, but for the ExternalJobMonitor it will be the process
			// that manages the worker process.

			int pid;
			using (var p = Process.GetCurrentProcess())
				pid = p.Id;

			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.AreEqual(pid, job.Pid);
			Assert.GreaterOrEqual(job.HeartBeat, job.StartDate);

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			VerifyDatabase(job);
		}

		[Test]
		public void TestHeartBeat()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp.Path, _tmp.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.GreaterOrEqual(job.HeartBeat, job.StartDate);

			Assert.IsTrue(_monitor.Pause());
			Assert.IsTrue(WaitUntil(JobStatus.Paused), "Timeout waiting for Paused");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			var time = job.HeartBeat;

			Thread.Sleep(2000);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			// Heartbeat should go up when we are paused
			Assert.Greater(job.HeartBeat, time);

			time = job.HeartBeat;

			Thread.Sleep(1000);

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			// Heartbeat should advance when job stops
			Assert.Greater(job.HeartBeat, time);
			Assert.GreaterOrEqual(job.HeartBeat, job.StopDate);

			VerifyDatabase(job);
		}
	}

	[TestFixture]
	[Peach]
	[Quick]
	class ExternalJobMonitorTests : BaseJobMonitorTests<ExternalJobMonitor>
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected TempDirectory _tmpDir;
		protected TempFile _tmp1;
		protected TempFile _tmp2;
		LoggingConfiguration _loggingConfig;

		protected const string CrashPitXml =
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

	<Agent name='LocalAgent'>
		<Monitor class='RandoFaulter'>
			<Param name='CrashAfter' value='2000'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
		<Agent ref='LocalAgent' />
	</Test>
</Peach>
";

		[SetUp]
		public void SetUp()
		{
			Logger.Trace(">>> Setup");

			Configuration.UseAsyncLogging = false;

			_doneEvt = new ManualResetEvent(false);
			_monitor = new ExternalJobMonitor
			{
				InternalEvent = (s, a) =>
				{
					Logger.Trace("InternalEvent");
					_doneEvt.Set();
				}
			};

			_tmp1 = new TempFile();
			_tmp2 = new TempFile();
			File.WriteAllText(_tmp1.Path, PitXml);
			File.WriteAllText(_tmp2.Path, CrashPitXml);

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

			Logger.Trace("<<< Setup");
		}

		[TearDown]
		public void TearDown()
		{
			Logger.Trace(">>> TearDown");

			_monitor.Dispose();
			_tmp1.Dispose();
			_tmp2.Dispose();
			_tmpDir.Dispose();

			LogManager.Configuration = _loggingConfig;
			Configuration.UseAsyncLogging = true;

			Logger.Trace("<<< TearDown");
		}

		[Test]
		public void TestRestart()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			var count = job.IterationCount;

			// kill the worker without setting _pendingKill
			((ExternalJobMonitor)_monitor).Terminate();

			Thread.Sleep(TimeSpan.FromSeconds(5));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.Greater(job.IterationCount, count);

			VerifyDatabase(job);
		}

		[Test]
		public void TestCrash()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp2.Path, _tmp2.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var count = job.IterationCount;

			Thread.Sleep(TimeSpan.FromSeconds(5));

			// should have crashed at least once by now

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.Greater(job.IterationCount, count);

			Assert.IsTrue(_monitor.Kill(), "Kill failed");
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			var logPath = Path.Combine(Configuration.LogRoot, job.Id, "error.log");
			Assert.IsTrue(File.Exists(logPath));
			var log = File.ReadAllText(logPath);
			Console.Write(log);

			VerifyDatabase(job);
		}
	}
}
