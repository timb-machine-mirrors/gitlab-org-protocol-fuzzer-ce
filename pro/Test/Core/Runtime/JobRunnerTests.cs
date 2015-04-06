using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.Core.Storage;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;

namespace Peach.Pro.Test.Core.Runtime
{
	[TestFixture]
	class JobRunnerTests
	{
		TempFile _tmp;

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
		</Monitor>
	</Agent>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
		<Agent ref='LocalAgent' />
	</Test>
</Peach>
";
		class JobTuple
		{
			public Job Job { get; set; }
			public JobRunner JobRunner { get; set; }
		}

		JobTuple StartJob(JobRequest jobRequest)
		{
			var job = JobRunner.CreateJob(_tmp.Path, jobRequest, Guid.NewGuid());
			return new JobTuple
			{
				Job = job,
				JobRunner = new JobRunner(job, "", _tmp.Path),
			};
		}

		void VerifyDatabase(Job job)
		{
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);
			}

			using (var db = new JobDatabase(job.DatabasePath))
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}
		}

		Job GetJob(Guid guid)
		{
			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(guid);
				Assert.IsNotNull(job);
			}

			if (!File.Exists(job.DatabasePath))
				return job;

			using (var db = new JobDatabase(job.DatabasePath))
			{
				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);
			}

			return job;
		}

		void WaitUntil(Guid guid, params JobStatus[] status)
		{
			// waits up to 10 seconds
			for (var i = 0; i < 100; i++)
			{
				var job = GetJob(guid);
				Assert.IsNotNull(job);
				if (status.Contains(job.Status))
					return;

				Thread.Sleep(100);
			}
			Assert.Fail("Timeout");
		}

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempFile();
			File.WriteAllText(_tmp.Path, PitXml);

			if (File.Exists(NodeDatabase.GetDatabasePath()))
				File.Delete(NodeDatabase.GetDatabasePath());
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}

		[Test]
		public void TestBasic()
		{
			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};
			var tuple = StartJob(jobRequest);
			var task = Task.Factory.StartNew(tuple.JobRunner.Run);

			WaitUntil(tuple.Job.Guid, JobStatus.Running, JobStatus.Stopped);

			task.Wait();

			VerifyDatabase(tuple.Job);
		}

		[Test]
		public void TestStop()
		{
			var jobRequest = new JobRequest();
			var tuple = StartJob(jobRequest);
			var task = Task.Factory.StartNew(tuple.JobRunner.Run);
			WaitUntil(tuple.Job.Guid, JobStatus.Running);
			tuple.JobRunner.Stop();
			WaitUntil(tuple.Job.Guid, JobStatus.Stopped);
			task.Wait();
			VerifyDatabase(tuple.Job);
		}

		[Test]
		public void TestPauseContinue()
		{
			var jobRequest = new JobRequest();
			var tuple = StartJob(jobRequest);
			var task = Task.Factory.StartNew(tuple.JobRunner.Run);
			WaitUntil(tuple.Job.Guid, JobStatus.Running);
			tuple.JobRunner.Pause();
			WaitUntil(tuple.Job.Guid, JobStatus.Paused);
			tuple.JobRunner.Continue();
			WaitUntil(tuple.Job.Guid, JobStatus.Running);
			tuple.JobRunner.Stop();
			WaitUntil(tuple.Job.Guid, JobStatus.Stopped);
			task.Wait();
			VerifyDatabase(tuple.Job);
		}

		[Test]
		[Repeat(2)]
		public void TestPitTester()
		{
			var jobRequest = new JobRequest
			{
				IsControlIteration = true,
			};

			var tuple = StartJob(jobRequest);
			var task = Task.Factory.StartNew(tuple.JobRunner.Run);
			WaitUntil(tuple.Job.Guid, JobStatus.Stopped);
			task.Wait();

			var id = tuple.Job.Guid;

			using (var db = new NodeDatabase())
			{
				DatabaseTests.AssertResult(db.GetTestEventsByJob(id), new[]
				{
					 new TestEvent(1, id, TestStatus.Pass, "Loading pit file", "Loading pit file '{0}'".Fmt(_tmp.Path), null),
					 new TestEvent(2, id, TestStatus.Pass, "Starting fuzzing engine", "Starting fuzzing engine", null),
					 new TestEvent(3, id, TestStatus.Pass, "Connecting to agent", "Connecting to agent 'local://'", null),
					 new TestEvent(4, id, TestStatus.Pass, "Starting monitor", "Starting monitor 'RandoFaulter'", null),
					 new TestEvent(5, id, TestStatus.Pass, "Starting fuzzing session", "Notifying agent 'local://' that the fuzzing session is starting", null),
					 new TestEvent(6, id, TestStatus.Pass, "Running iteration", "Running the initial control record iteration", null),
				});
			}

			var job = GetJob(tuple.Job.Guid);
			Assert.IsTrue(File.Exists(job.DebugLogPath));
			Console.Write(File.ReadAllText(job.DebugLogPath));
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

				var id = Guid.NewGuid();
				var job = JobRunner.CreateJob(xmlFile.Path, jobRequest, id);
				var runner = new JobRunner(job, "", xmlFile.Path);
				var task = Task.Factory.StartNew(runner.Run);
				WaitUntil(job.Guid, JobStatus.Stopped);
				task.Wait();

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(id), new[]
					{
						 new TestEvent(
							 1, 
							 id, 
							 TestStatus.Fail, 
							 "Loading pit file", 
							 "Loading pit file '{0}'".Fmt(xmlFile.Path), 
							 "Error: XML Failed to load: Data at the root level is invalid. Line 31, position 1."),
					});
				}

				job = GetJob(id);
				Assert.IsFalse(File.Exists(job.DebugLogPath));

				Assert.IsTrue(File.Exists(job.AltDebugLogPath));
				Console.Write(File.ReadAllText(job.AltDebugLogPath));
			}
		}
	}
}
