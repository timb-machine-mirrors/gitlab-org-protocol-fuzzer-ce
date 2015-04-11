using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core;
using System.Linq;
using Peach.Pro.Test.Core.Storage;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.WebServices
{
	class BaseJobMonitorTests<T> where T : IJobMonitor, new()
	{
		protected TempFile _tmp1;
		protected TempFile _tmp2;
		protected IJobMonitor _monitor;

		protected const string PitXml =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Deja Vu Security, LLC'
       version='0.0.1'>

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
			{0}
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
			_monitor = new T();

			_tmp1 = new TempFile();
			_tmp2 = new TempFile();
			File.WriteAllText(_tmp1.Path, PitXml.Fmt(""));
			const string param = "<Param name='CrashAfter' value='2000'/>";
			File.WriteAllText(_tmp2.Path, PitXml.Fmt(param));

			if (File.Exists(NodeDatabase.GetDatabasePath()))
				File.Delete(NodeDatabase.GetDatabasePath());
		}

		[TearDown]
		public void TearDown()
		{
			_tmp2.Dispose();
			_tmp1.Dispose();
			_monitor.Dispose();
		}

		protected bool WaitUntil(params JobStatus[] status)
		{
			// waits up to 10 seconds
			for (var i = 0; i < 100; i++)
			{
				var job = _monitor.GetJob();
				Assert.IsNotNull(job);
				if (status.Contains(job.Status))
					return true;

				Thread.Sleep(100);
			}
			return false;
		}

		protected void VerifyDatabase(Job job)
		{
			using (var db = new NodeDatabase())
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}

			using (var db = new JobDatabase(job.DatabasePath))
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
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
		[Test]
		[Repeat(10)]
		public void TestBasic()
		{
			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running, JobStatus.Stopped));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.IsNotNull(job.DatabasePath);

			VerifyDatabase(job);
		}

		[Test]
		public void TestStop()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.IsTrue(_monitor.Stop());
			Assert.IsTrue(WaitUntil(JobStatus.Stopped));

			VerifyDatabase(job);
		}

		[Test]
		public void TestPauseContinue()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.IsTrue(_monitor.Pause());
			Assert.IsTrue(WaitUntil(JobStatus.Paused));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			Assert.IsTrue(_monitor.Continue());
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			VerifyDatabase(job);
		}

		[Test]
		public void TestKill()
		{
			Configuration.LogLevel = NLog.LogLevel.Trace;

			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.IsTrue(_monitor.Kill());
			Assert.IsTrue(WaitUntil(JobStatus.Stopped));

			VerifyDatabase(job);
		}

		[Test]
		[Repeat(2)]
		public void TestPitTester()
		{
			var jobRequest = new JobRequest
			{
				IsControlIteration = true,
			};

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Stopped));

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);

				DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
				{
					 new TestEvent(1, job.Guid, TestStatus.Pass, "Loading pit file", 
						 "Loading pit file '{0}'".Fmt(_tmp1.Path), null),
					 new TestEvent(2, job.Guid, TestStatus.Pass, "Starting fuzzing engine", 
						 "Starting fuzzing engine", null),
					 new TestEvent(3, job.Guid, TestStatus.Pass, "Connecting to agent", 
						 "Connecting to agent 'local://'", null),
					 new TestEvent(4, job.Guid, TestStatus.Pass, "Starting monitor", 
						 "Starting monitor 'RandoFaulter'", null),
					 new TestEvent(5, job.Guid, TestStatus.Pass, "Starting fuzzing session", 
						 "Notifying agent 'local://' that the fuzzing session is starting", null),
					 new TestEvent(6, job.Guid, TestStatus.Pass, "Running iteration", 
						 "Running the initial control record iteration", null),
				});
			}

			using (var db = new JobDatabase(job.DatabasePath))
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}

			Assert.IsTrue(File.Exists(job.DebugLogPath));
			Console.Write(File.ReadAllText(job.DebugLogPath));
		}

		[Test]
		public void TestPitParseFailureDuringTest()
		{
			using (var xmlFile = new TempFile())
			{
				File.WriteAllText(xmlFile.Path, PitXml.Fmt("xxx"));

				var jobRequest = new JobRequest
				{
					IsControlIteration = true,
				};

				var job = _monitor.Start(xmlFile.Path, xmlFile.Path, jobRequest);
				Assert.IsNotNull(job);
				Assert.IsTrue(WaitUntil(JobStatus.Stopped));

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
							 "Error, Pit file \"{0}\" failed to validate: ".Fmt(xmlFile.Path) +
							 Environment.NewLine +
							 "Line: 21, Position: 33 - The element 'Monitor' in namespace 'http://peachfuzzer.com/2012/Peach' cannot contain text. " +
							 "List of possible elements expected: 'Param' in namespace 'http://peachfuzzer.com/2012/Peach'." +
							 Environment.NewLine),
					});

					job = db.GetJob(job.Guid);
					Assert.IsNotNull(job);
				}

				Assert.IsFalse(File.Exists(job.DatabasePath));
				Assert.IsFalse(File.Exists(job.DebugLogPath));
				Assert.IsTrue(File.Exists(job.AltDebugLogPath));
				Console.Write(File.ReadAllText(job.AltDebugLogPath));
			}
		}

		[Test]
		public void TestPitParseFailureDuringRun()
		{
			using (var xmlFile = new TempFile())
			{
				File.WriteAllText(xmlFile.Path, PitXml.Fmt("xxx"));

				var jobRequest = new JobRequest();
				var job = _monitor.Start(xmlFile.Path, xmlFile.Path, jobRequest);
				Assert.IsNotNull(job);
				Assert.IsTrue(WaitUntil(JobStatus.Stopped));

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
							 "Error, Pit file \"{0}\" failed to validate: ".Fmt(xmlFile.Path) +
							 Environment.NewLine +
							 "Line: 21, Position: 33 - The element 'Monitor' in namespace 'http://peachfuzzer.com/2012/Peach' cannot contain text. " +
							 "List of possible elements expected: 'Param' in namespace 'http://peachfuzzer.com/2012/Peach'." +
							 Environment.NewLine),
					});

					job = db.GetJob(job.Guid);
					Assert.IsNotNull(job);
				}

				Assert.IsFalse(File.Exists(job.DatabasePath));
				Assert.IsFalse(File.Exists(job.DebugLogPath));
				Assert.IsTrue(File.Exists(job.AltDebugLogPath));
				Console.Write(File.ReadAllText(job.AltDebugLogPath));
			}
		}
	}

	[TestFixture]
	[Peach]
	[Quick]
	class ExternalJobMonitorTests : BaseJobMonitorTests<ExternalJobMonitor>
	{
		[Test]
		public void TestRestart()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			var count = job.IterationCount;

			// kill the worker without setting _pendingKill
			((ExternalJobMonitor)_monitor).Terminate();

			Thread.Sleep(TimeSpan.FromSeconds(5));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.Greater(job.IterationCount, count);

			VerifyDatabase(job);
		}

		[Test]
		public void TestCrash()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmp2.Path, _tmp2.Path, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var count = job.IterationCount;

			Thread.Sleep(TimeSpan.FromSeconds(5));

			// should have crashed at least once by now

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.Greater(job.IterationCount, count);

			Assert.IsTrue(_monitor.Kill());
			Assert.IsTrue(WaitUntil(JobStatus.Stopped));

			var logPath = Path.Combine(Configuration.LogRoot, job.Id, "error.log");
			Assert.IsTrue(File.Exists(logPath));
			var log = File.ReadAllText(logPath);
			Console.Write(log);

			VerifyDatabase(job);
		}
	}
}
