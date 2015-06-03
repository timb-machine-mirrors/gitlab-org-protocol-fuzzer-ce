using System.Diagnostics;
using Peach.Pro.Core.WebServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;

namespace Peach.Pro.Core.Storage
{
	public class JobHelper
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		/// <summary>
		/// refresh job status from NodeDatabase or JobDatabase
		/// 1. Use NodeDatabase if LogPath is not set
		/// 2. Use JobDatabase if LogPath is set
		/// 3. Return null if job does not exist in NodeDatabase or LogPath is invalid
		/// </summary>
		/// <param name="id">Job ID</param>
		/// <returns>Job if job can be found, otherwise null</returns>
		public static Job GetJob(Guid id)
		{
			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(id);
				if (job == null)
				{
					Logger.Trace("Job does not exist in NodeDatabase");
					return null;
				}

				return EnsureUpToDate(db, job);
			}
		}

		public static Job EnsureUpToDate(NodeDatabase db, Job job)
		{
			if (job.DatabasePath == null)
				return job;

			if (!File.Exists(job.DatabasePath))
			{
				Logger.Trace("DatabasePath is invalid, deleting job");
				db.DeleteJob(job.Guid);
				return null;
			}

			using (var jobDb = new JobDatabase(job.DatabasePath))
			{
				job = jobDb.GetJob(job.Guid);
				if (job == null)
				{
					Logger.Trace("Job does not exist in JobDatabase");
					return null;
				}
			}

			return job;
		}

		public static void Fail(
			Guid id,
			Func<NodeDatabase, IEnumerable<TestEvent>> getEvents,
			string message)
		{
			var job = GetJob(id);
			if (job == null)
				return;

			using (var db = new NodeDatabase())
			{
				var events = getEvents(db).ToList();
				foreach (var testEvent in events)
				{
					if (testEvent.Status == TestStatus.Active)
					{
						testEvent.Status = TestStatus.Fail;
						testEvent.Resolve = message;
					}
				}

				job.StopDate = DateTime.Now;
				job.HeartBeat = job.StopDate;
				job.Mode = JobMode.Fuzzing;
				job.Status = JobStatus.Stopped;
				job.Result = message;

				db.UpdateJob(job);
				db.UpdateTestEvents(events);
			}

			if (File.Exists(job.DatabasePath))
			{
				using (var db = new JobDatabase(job.DatabasePath))
				{
					db.UpdateJob(job);
				}
			}
		}
	}

	public class NodeDatabase : Database
	{
		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// job status
			typeof(Job),
			typeof(JobLog),

			// pit tester
			typeof(TestEvent),
		};

		protected override IEnumerable<Type> Schema
		{
			get { return StaticSchema; }
		}

		protected override IEnumerable<string> Scripts
		{
			get { return null; }
		}

		public NodeDatabase()
			: base(GetDatabasePath(), false)
		{
		}

		// used by unit tests
		internal static string GetDatabasePath()
		{
			var logRoot = Configuration.LogRoot;

			if (!Directory.Exists(logRoot))
				Directory.CreateDirectory(logRoot);

			return System.IO.Path.Combine(logRoot, "node.db");
		}

		public IEnumerable<JobLog> GetJobLogs(Guid id)
		{
			return Connection.Query<JobLog>(Sql.SelectJobLogs, new { Id = id.ToString() });
		}

		public void InsertJobLog(JobLog log)
		{
			Connection.Execute(Sql.InsertJobLog, log);
		}

		public Job GetJob(Guid id)
		{
			return Connection.Query<Job>(Sql.SelectJob, new { Id = id.ToString() })
				.SingleOrDefault();
		}

		public void InsertJob(Job job)
		{
			Connection.Execute(Sql.InsertJob, job);
		}

		public void UpdateJob(Job job)
		{
			Connection.Execute(Sql.UpdateJob, job);
		}

		public void DeleteJob(Guid id)
		{
			Connection.Execute(Sql.DeleteJob, new { Id = id.ToString() });
		}

		public void UpdateJobs(IEnumerable<Job> job)
		{
			Connection.Execute(Sql.UpdateJob, job);
		}

		public void DeleteJobs(IEnumerable<Job> jobs)
		{
			var ids = jobs.Select(x => new { Id = x.Id });
			Connection.Execute(Sql.DeleteJob, ids);
		}

		public void InsertTestEvent(TestEvent testEvent)
		{
			testEvent.Id = Connection.ExecuteScalar<long>(Sql.InsertTestEvent, testEvent);
		}

		public void UpdateTestEvents(IEnumerable<TestEvent> testEvent)
		{
			Connection.Execute(Sql.UpdateTestEvent, testEvent);
		}

		public IEnumerable<TestEvent> GetTestEventsByJob(Guid jobId)
		{
			const string sql = "SELECT * FROM [TestEvent] WHERE JobId = @JobId;";
			return Connection.Query<TestEvent>(sql, new { JobId = jobId.ToString() });
		}
	}
}
