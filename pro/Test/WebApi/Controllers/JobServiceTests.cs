using System;
using System.Diagnostics;
using System.IO;
using Nancy;
using Nancy.Testing;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi;

namespace Peach.Pro.Test.WebApi.Controllers
{
	[TestFixture]
	[Quick]
	class JobServiceTests
	{
		class TestJobMonitor : IJobMonitor
		{
			readonly JobServiceTests _owner;
			readonly int _pid = Utilities.GetCurrentProcessId();

			public TestJobMonitor(JobServiceTests owner)
			{
				_owner = owner;
			}

			public void Dispose()
			{
			}

			public int Pid { get { return _pid; } }

			public bool IsTracking(Job job)
			{
				lock (this)
				{
					return _owner._runningJob != null && _owner._runningJob.Guid == job.Guid;
				}
			}

			public Job GetJob()
			{
				return _owner._runningJob;
			}

			public Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest)
			{
				throw new NotImplementedException();
			}

			public bool Pause()
			{
				throw new NotImplementedException();
			}

			public bool Continue()
			{
				throw new NotImplementedException();
			}

			public bool Stop()
			{
				throw new NotImplementedException();
			}

			public bool Kill()
			{
				throw new NotImplementedException();
			}

			public EventHandler InternalEvent { set { } }
			}

		class TestBootstrapper : Bootstrapper
		{
			public TestBootstrapper(IJobMonitor jobMonitor)
				: base(new WebContext("pits", jobMonitor))
			{
			}

			protected override bool EulaAccepted
			{
				get { return true; }
			}
		}

		static int GetRunningPid()
		{
			int pid = -1;

			foreach (var proc in Process.GetProcesses())
			{
				if (pid == -1)
					pid = proc.Id;

				proc.Dispose();
			}

			return pid;
		}

		Job _runningJob;
		Browser _browser;
		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_runningJob = null;
			_tmpDir = new TempDirectory();
			_browser = new Browser(new TestBootstrapper(new TestJobMonitor(this)));

			Configuration.LogRoot = _tmpDir.Path;
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

		[Test]
		public void NoJobs()
		{
			var result = _browser.Get("/p/jobs", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();

			Assert.NotNull(jobs);
			Assert.AreEqual(0, jobs.Length);
		}

		[Test]
		public void TwoStopped()
		{
			// When jobs are running, their status should ne pulled from the job database

			var j1 = new Job(new JobRequest(), "pit1.xml");
			Assert.AreEqual(j1.Status, JobStatus.StartPending);
			j1.IterationCount = 100;
			j1.Status = JobStatus.Stopped;

			var j2 = new Job(new JobRequest(), "pit2.xml");
			Assert.AreEqual(j2.Status, JobStatus.StartPending);
			j1.IterationCount = 100;
			j2.Status = JobStatus.Stopped;


			using (var db = new NodeDatabase())
			{
				db.UpdateJob(j1);
				db.UpdateJob(j2);
			}

			var result = _browser.Get("/p/jobs", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();

			Assert.NotNull(jobs);
			Assert.AreEqual(2, jobs.Length);

			Assert.AreEqual(jobs[0].Id, j1.Id);
			Assert.AreEqual(JobStatus.Stopped, jobs[0].Status);
			Assert.AreEqual(jobs[1].Id, j2.Id);
			Assert.AreEqual(JobStatus.Stopped, jobs[1].Status);
		}

		[Test]
		public void TwoRunning()
		{
			// When jobs are running, their status should ne pulled from the job database
			// since it will be more up to date than the node database

			var j1 = new Job(new JobRequest(), "pit1.xml");

			var dir1 = Path.Combine(Configuration.LogRoot, "pit1");
			Directory.CreateDirectory(dir1);
			j1.LogPath = dir1;
			j1.Status = JobStatus.Running;
			j1.Pid = GetRunningPid();

			_runningJob = new Job(new JobRequest(), "pit2.xml");

			var dir2 = Path.Combine(Configuration.LogRoot, "pit2");
			Directory.CreateDirectory(dir2);
			_runningJob.LogPath = dir2;
			_runningJob.Status = JobStatus.Running;

			using (var db = new NodeDatabase())
			{
				db.UpdateJob(j1);
				db.UpdateJob(_runningJob);
			}

			j1.IterationCount = 100;
			j1.FaultCount = 5;

			_runningJob.IterationCount = 10;
			_runningJob.FaultCount = 3;

			using (var db = new JobDatabase(j1.DatabasePath))
				db.InsertJob(j1);

			using (var db = new JobDatabase(_runningJob.DatabasePath))
				db.InsertJob(_runningJob);

			var result = _browser.Get("/p/jobs", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();

			Assert.NotNull(jobs);
			Assert.AreEqual(2, jobs.Length);

			Assert.NotNull(jobs);
			Assert.AreEqual(2, jobs.Length);

			Assert.AreEqual(j1.Id, jobs[0].Id);
			Assert.AreEqual(JobStatus.Running, jobs[0].Status);
			Assert.AreEqual(100, jobs[0].IterationCount);
			Assert.AreEqual(5, jobs[0].FaultCount);

			Assert.AreEqual(_runningJob.Id, jobs[1].Id);
			Assert.AreEqual(JobStatus.Running, jobs[1].Status);
			Assert.AreEqual(10, jobs[1].IterationCount);
			Assert.AreEqual(3, jobs[1].FaultCount);
		}

		[Test]
		public void StartPending()
		{
			// Ensure we can get /p/jobs when a job is in the StartPending state
			// One job is ours and in start pending, the other job is not
			// ours and also in start pending.

			var j1 = new Job(new JobRequest(), "pit1.xml");
			j1.Pid = GetRunningPid(); // Make the pid not be us

			Assert.AreEqual(j1.Status, JobStatus.StartPending);

			_runningJob = new Job(new JobRequest(), "pit2.xml");

			var j2 = new Job(new JobRequest(), "pit2.xml");
			j2.Pid = -1; // Make it be an invalid pid

			using (var db = new NodeDatabase())
			{
				db.UpdateJob(j1);
				db.UpdateJob(j2);
			}

			var result = _browser.Get("/p/jobs", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();

			Assert.NotNull(jobs);
			Assert.AreEqual(3, jobs.Length);
			Assert.AreEqual(j1.Id, jobs[0].Id);
			Assert.AreEqual(JobStatus.StartPending, jobs[0].Status);
			Assert.AreEqual(_runningJob.Id, jobs[1].Id);
			Assert.AreEqual(JobStatus.StartPending, jobs[1].Status);
			Assert.AreEqual(j2.Id, jobs[2].Id);
			Assert.AreEqual(JobStatus.Stopped, jobs[2].Status);
		}

		[Test]
		public void PruneDeleted()
		{
			// If the job db is deleted, the entry from the node db should also be deleted
			// on the next get

			var dir1 = Path.Combine(Configuration.LogRoot, "pit1");
			Directory.CreateDirectory(dir1);

			// Stopped job w/ database
			var j1 = new Job(new JobRequest(), "pit1.xml")
			{
				LogPath = dir1,
				Status = JobStatus.Stopped
			};

			// Stopped job w/o database
			var j2 = new Job(new JobRequest(), "pit2.xml")
			{
				LogPath = Path.Combine(Configuration.LogRoot, "pit2"),
				Status = JobStatus.Stopped
			};

			// Stopped job w/ null database
			var j3 = new Job(new JobRequest(), "pit3.xml")
			{
				Status = JobStatus.Stopped
			};

			using (var db = new JobDatabase(j1.DatabasePath))
				db.InsertJob(j1);

			using (var db = new NodeDatabase())
			{
				db.UpdateJob(j1);
				db.UpdateJob(j2);
				db.UpdateJob(j3);
			}

			var result = _browser.Get("/p/jobs", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();

			Assert.NotNull(jobs);
			Assert.AreEqual(2, jobs.Length);
			Assert.AreEqual(j1.Id, jobs[0].Id);
			Assert.AreEqual(j3.Id, jobs[1].Id);
		}

		[Test]
		public void CompleteStale()
		{
			// Stale jobs should be completed as long as the heartbeat
			// has surpassed some threshold and the pid meets the proper
			// criteria.

			// Pid of job is us and job is not being monitored, it should be immediatley stopped
			// Pid of job is not us, the pid doesn't exist, it should be stopped immediatley
			// Pid of job is not us and the pid exists, fallback to heartbeat

			var now = DateTime.Now;

			int pid = -1;

			foreach (var proc in Process.GetProcesses())
			{
				if (pid == -1)
					pid = proc.Id;

				proc.Dispose();
			}

			// Our pid and not being monitored and recent herarbeat
			var j1 = new Job(new JobRequest(), "pit1.xml")
			{
				StartDate = now - TimeSpan.FromHours(1),
				HeartBeat = now,
				Status = JobStatus.Running
			};

			// Not us but existing pid and 30sec late
			var j2 = new Job(new JobRequest(), "pit2.xml")
			{
				StartDate = now - TimeSpan.FromHours(1),
				HeartBeat = now - TimeSpan.FromSeconds(30),
				Pid = pid,
				Status = JobStatus.Running
			};

			// Not us but existing pid and 60sec late
			var j3 = new Job(new JobRequest(), "pit3.xml")
			{
				StartDate = now - TimeSpan.FromHours(1),
				HeartBeat = now - TimeSpan.FromSeconds(60),
				Pid = pid,
				Status = JobStatus.Running
			};

			// Pid doesn't exist and recent hearbeat
			var j4 = new Job(new JobRequest(), "pit4.xml")
			{
				StartDate = now - TimeSpan.FromHours(1),
				HeartBeat = now,
				Pid = -1,
				Status = JobStatus.Running
			};

			// Running job with late heartbeat
			_runningJob = new Job(new JobRequest(), "pit5.xml")
			{
				StartDate = now - TimeSpan.FromHours(1),
				HeartBeat = now - TimeSpan.FromHours(1),
				Status = JobStatus.Running
			};

			var jobs = new[] { j1, j2, j3, j4, _runningJob };

			foreach (var j in jobs)
			{
				j.LogPath = Path.Combine(Configuration.LogRoot, j.PitFile);
				Directory.CreateDirectory(j.LogPath);

				using (var db = new JobDatabase(j.DatabasePath))
				{
					db.InsertJob(j);
				}
			}

			using (var db = new NodeDatabase())
				db.UpdateJobs(jobs);

			var result = _browser.Get("/p/jobs", with => with.HttpRequest());

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			jobs = result.DeserializeJson<Job[]>();

			Assert.NotNull(jobs);
			Assert.AreEqual(5, jobs.Length);

			Assert.AreEqual(j1.Id, jobs[0].Id);
			Assert.AreEqual(JobStatus.Stopped, jobs[0].Status);

			Assert.AreEqual(j2.Id, jobs[1].Id);
			Assert.AreEqual(JobStatus.Running, jobs[1].Status);

			Assert.AreEqual(j3.Id, jobs[2].Id);
			Assert.AreEqual(JobStatus.Running, jobs[2].Status);

			Assert.AreEqual(j4.Id, jobs[3].Id);
			Assert.AreEqual(JobStatus.Stopped, jobs[3].Status);

			Assert.AreEqual(_runningJob.Id, jobs[4].Id);
			Assert.AreEqual(JobStatus.Running, jobs[4].Status);
		}

		[Test]
		[TestCase("true")]
		[TestCase("false")]
		public void GetRunning(string query)
		{
			var j1 = new Job(new JobRequest(), "pit1.xml");
			Assert.AreEqual(j1.Status, JobStatus.StartPending);
			j1.IterationCount = 100;
			j1.Status = JobStatus.Stopped;

			_runningJob = new Job(new JobRequest(), "pit2.xml");

			var dir2 = Path.Combine(Configuration.LogRoot, "pit2");
			Directory.CreateDirectory(dir2);
			_runningJob.LogPath = dir2;
			_runningJob.Status = JobStatus.Running;

			using (var db = new NodeDatabase())
			{
				db.UpdateJob(j1);
				db.UpdateJob(_runningJob);
			}

			using (var db = new JobDatabase(_runningJob.DatabasePath))
				db.InsertJob(_runningJob);

			var result = _browser.Get("/p/jobs", with =>
			{
				with.Query("running", query);
				with.HttpRequest();
			});

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();
			Assert.NotNull(jobs);

			Assert.AreEqual(1, jobs.Length);
		}

		[Test]
		[TestCase("true")]
		[TestCase("false")]
		public void GetDryRun(string query)
		{
			var j1 = new Job(new JobRequest { IsControlIteration = true }, "pit1.xml")
			{
				Status = JobStatus.Stopped
			};

			var j2 = new Job(new JobRequest(), "pit1.xml")
			{
				IterationCount = 100,
				Status = JobStatus.Stopped
			};

			using (var db = new NodeDatabase())
			{
				db.UpdateJob(j1);
				db.UpdateJob(j2);
			}

			var result = _browser.Get("/p/jobs", with =>
			{
				with.Query("dryrun", query);
				with.HttpRequest();
			});

			Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

			var jobs = result.DeserializeJson<Job[]>();
			Assert.NotNull(jobs);

			Assert.AreEqual(1, jobs.Length);
		}
	}
}
