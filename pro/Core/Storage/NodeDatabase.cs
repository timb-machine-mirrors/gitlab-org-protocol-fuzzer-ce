using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;

namespace Peach.Pro.Core.Storage
{
	class NodeDatabase : Database
	{
		#region SQL
		const string SqlSelectJob = @"
SELECT * 
FROM [Job] 
WHERE Id = @Id;
";

		const string SqlInsertJob = @"
INSERT INTO [Job] (
	Id,
	Status,
	Mode,
	Name,
	Result,
	Notes,
	User,
	Seed,
	IterationCount,
	StartDate,
	StopDate,
	Runtime,
	Speed,
	FaultCount,
	RangeStart,
	RangeStop,
	HasMetrics
) VALUES (
	@Id,
	@Status,
	@Mode,
	@Name,
	@Result,
	@Notes,
	@User,
	@Seed,
	@IterationCount,
	@StartDate,
	@StopDate,
	@Runtime,
	@Speed,
	@FaultCount,
	@RangeStart,
	@RangeStop,
	@HasMetrics
);";

		const string SqlUpdateJob = @"
UPDATE [Job]
SET 
	Status = @Status,
	Mode = @Mode,
	Name = @Name,
	Result = @Result,
	Notes = @Notes,
	User = @User,
	Seed = @Seed,
	IterationCount = @IterationCount,
	StartDate = @StartDate,
	StopDate = @StopDate,
	Runtime = @Runtime,
	Speed = @Speed,
	FaultCount = @FaultCount,
	RangeStart = @RangeStart,
	RangeStop = @RangeStop,
	HasMetrics = @HasMetrics
WHERE
	Id = @Id
;";

		const string SqlDeleteJob = @"
DELETE FROM [Job]
WHERE Id = @Id;
";
		#endregion

		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// job status
			typeof(Job),
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
			: base(GetDatabasePath())
		{
		}

		// used by unit tests
		internal NodeDatabase(string path)
			: base(path)
		{
		}

		static string GetDatabasePath()
		{
			var config = Utilities.GetUserConfig();
			var logRoot = config.AppSettings.Settings.Get("LogPath");
			if (string.IsNullOrEmpty(logRoot))
				logRoot = Utilities.GetAppResourcePath("db");
	
			if (!Directory.Exists(logRoot))
				Directory.CreateDirectory(logRoot);

			return System.IO.Path.Combine(logRoot, "node.db");
		}

		public Job GetJob(Guid id)
		{
			return Connection.Query<Job>(SqlSelectJob, new { Id = id.ToString() })
				.SingleOrDefault();
		}

		public void InsertJob(Job job)
		{
			Connection.Execute(SqlInsertJob, job);
		}

		public void UpdateJob(Job job)
		{
			Connection.Execute(SqlUpdateJob, job);
		}

		public void DeleteJob(Guid id)
		{
			Connection.Execute(SqlDeleteJob, new { Id = id.ToString() });
		}
	}
}
