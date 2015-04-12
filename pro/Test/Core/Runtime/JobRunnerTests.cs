using System;
using System.Linq;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.Core.Storage;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Runtime
{
	[TestFixture]
	[Peach]
	[Quick]
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

			if (File.Exists(NodeDatabase.GetDatabasePath()))
				File.Delete(NodeDatabase.GetDatabasePath());
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}

		class SafeRunner : IDisposable
		{
			Job _job;
			public JobRunner JobRunner { get; private set; }
			Thread _thread;
			Exception _caught;

			public SafeRunner(string xmlFile, JobRequest jobRequest)
			{
				_job = JobRunner.CreateJob(xmlFile, jobRequest, Guid.NewGuid());
				JobRunner = new JobRunner(_job, "", xmlFile);
				_thread = new Thread(() =>
				{
					try
					{
						JobRunner.Run();
					}
					catch (Exception ex)
					{
						_caught = ex;
					}
				});
				_thread.Start();
			}

			public Guid Id { get { return _job.Guid; } }

			public void WaitUntil(params JobStatus[] status)
			{
				// waits up to 20 seconds
				for (var i = 0; i < 40; i++)
				{
					var job = GetJob();
					Assert.IsNotNull(job);
					if (status.Contains(job.Status))
						return;

					Thread.Sleep(500);
				}
				Assert.Fail("Timeout");
			}

			public void Join()
			{
				Assert.IsTrue(_thread.Join(TimeSpan.FromSeconds(10)));
			}

			public Job GetJob()
			{
				Job job;
				using (var db = new NodeDatabase())
				{
					job = db.GetJob(Id);
					Assert.IsNotNull(job);
				}

				if (!File.Exists(job.DatabasePath))
					return job;

				using (var db = new JobDatabase(job.DatabasePath))
				{
					job = db.GetJob(Id);
					Assert.IsNotNull(job);
				}

				return job;
			}

			public void VerifyDatabase()
			{
				Job job;
				using (var db = new NodeDatabase())
				{
					job = db.GetJob(Id);
					Assert.IsNotNull(job);
				}

				using (var db = new JobDatabase(job.DatabasePath))
				{
					Assert.IsNotNull(db.GetJob(Id));
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
				runner.WaitUntil(JobStatus.Stopped);
				runner.Join();
				runner.VerifyDatabase();
			}
		}

		[Test]
		public void TestStop()
		{
			var jobRequest = new JobRequest();
			using (var runner = new SafeRunner(_tmp.Path, jobRequest))
			{
				runner.WaitUntil(JobStatus.Running);
				runner.JobRunner.Stop();
				runner.WaitUntil(JobStatus.Stopped);
				runner.Join();
				runner.VerifyDatabase();
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
				runner.WaitUntil(JobStatus.Stopped);
				runner.Join();
				runner.VerifyDatabase();
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
				runner.WaitUntil(JobStatus.Stopped);
				runner.Join();

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
					runner.WaitUntil(JobStatus.Stopped);
					runner.Join();

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
							 "Error: XML Failed to load: Data at the root level is invalid. Line 25, position 1."),
					});
					}

					var job = runner.GetJob();
					Assert.IsFalse(File.Exists(job.DebugLogPath));

					Assert.IsTrue(File.Exists(job.AltDebugLogPath));
					Console.Write(File.ReadAllText(job.AltDebugLogPath));
				}
			}
		}
	}
}
