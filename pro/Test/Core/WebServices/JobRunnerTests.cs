using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core.WebServices
{
	[TestFixture]
	class JobRunnerTests
	{
		TempFile _tmp1;
		TempFile _tmp2;
		JobRunner _runner;

		const string PitXml =
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
			_runner = new JobRunner();
			_tmp1 = new TempFile();
			_tmp2 = new TempFile();
			File.WriteAllText(_tmp1.Path, PitXml.Fmt(""));
			const string param = "<Param name='CrashAfter' value='2000'/>";
			File.WriteAllText(_tmp2.Path, PitXml.Fmt(param));
		}

		[TearDown]
		public void TearDown()
		{
			_tmp1.Dispose();
			_tmp2.Dispose();
			_runner.Dispose();
		}

		bool WaitUntil(JobStatus status)
		{
			// waits up to 10 seconds
			for (var i = 0; i < 100; i++)
			{
				var job = _runner.Job;
				Assert.IsNotNull(job);
				if (job.Status == status)
					return true;

				Thread.Sleep(100);
			}
			return false;
		}

		bool WaitUntilStopped()
		{
			// waits up to 10 seconds
			for (var i = 0; i < 100;i++)
			{
				var job = _runner.Job;
				if (job == null)
					return true;

				Thread.Sleep(100);
			}
			return false;
		}

		[Test]
		public void TestBasic()
		{
			var jobRequest = new Job
			{
				RangeStop = 1,
			};

			var ret = _runner.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsTrue(ret);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			var job = _runner.Job;
			Assert.IsNotNull(job);

			using (var db = new NodeDatabase())
			{
				Assert.IsNull(db.GetJob(job.Guid));
			}
		}

		[Test]
		public void TestStop()
		{
			var jobRequest = new Job();

			var ret = _runner.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsTrue(ret);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			var job = _runner.Job;
			Assert.IsNotNull(job);

			Assert.IsTrue(_runner.Stop());
			Assert.IsTrue(WaitUntilStopped());

			using (var db = new NodeDatabase())
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}
		}

		[Test]
		public void TestPauseContinue()
		{
			var jobRequest = new Job();

			var ret = _runner.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsTrue(ret);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			var job = _runner.Job;
			Assert.IsNotNull(job);

			Assert.IsTrue(_runner.Pause());
			Assert.IsTrue(WaitUntil(JobStatus.Paused));

			job = _runner.Job;
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			Assert.IsTrue(_runner.Continue());
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			using (var db = new NodeDatabase())
			{
				Assert.IsNull(db.GetJob(job.Guid));
			}
		}

		[Test]
		public void TestKill()
		{
			var jobRequest = new Job();

			var ret = _runner.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsTrue(ret);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			var job = _runner.Job;
			Assert.IsNotNull(job);

			Assert.IsTrue(_runner.Kill());
			Assert.IsTrue(WaitUntilStopped());

			using (var db = new NodeDatabase())
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}
		}

		[Test]
		public void TestRestart()
		{
			var jobRequest = new Job();

			var ret = _runner.Start(_tmp1.Path, _tmp1.Path, jobRequest);
			Assert.IsTrue(ret);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			var job = _runner.Job;
			Assert.IsNotNull(job);
			var count = job.IterationCount;

			// kill the worker without setting _pendingKill
			_runner.Terminate();

			Thread.Sleep(TimeSpan.FromSeconds(5));

			job = _runner.Job;
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.Greater(job.IterationCount, count);

			using (var db = new NodeDatabase())
			{
				Assert.IsNull(db.GetJob(job.Guid));
			}
		}

		[Test]
		public void TestCrash()
		{
			var jobRequest = new Job();

			var ret = _runner.Start(_tmp2.Path, _tmp2.Path, jobRequest);
			Assert.IsTrue(ret);
			Assert.IsTrue(WaitUntil(JobStatus.Running));

			var job = _runner.Job;
			Assert.IsNotNull(job);

			var count = job.IterationCount;

			Thread.Sleep(TimeSpan.FromSeconds(5));

			// should have crashed at least once by now

			job = _runner.Job;
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.Greater(job.IterationCount, count);
		}

		[Test]
		public void TestPitTester()
		{

		}

		// TODO: Fatal error reporting (ensure no restarts):
		//       Pit parser failure
		//       Control iteration failure
	}
}
