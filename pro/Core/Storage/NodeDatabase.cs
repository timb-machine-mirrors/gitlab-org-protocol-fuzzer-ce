using Peach.Pro.Core.WebServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;

namespace Peach.Pro.Core.Storage
{
	public class NodeDatabase : Database
	{
		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// job status
			typeof(Job),

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
