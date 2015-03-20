using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using System.Linq;
using Peach.Core;

#if MONO
using Mono.Data.Sqlite;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
#else
using System.Data.SQLite;
#endif

namespace Peach.Pro.Core.Storage
{
	class JobContext : IDisposable
	{
		#region SQL
		const string SqlSelectJob = @"
SELECT * FROM [Job] WHERE Id = @Id
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
	HasMetrics,
	StartIteration,
	CurrentIteration
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
	@HasMetrics,
	@StartIteration,
	@CurrentIteration
)";

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
	HasMetrics = @HasMetrics,
	StartIteration = @StartIteration,
	CurrentIteration = @CurrentIteration
WHERE
	Id = @Id
";

		const string SqlInsertMutation = @"
INSERT INTO Mutation (
	Iteration,
	StateId,
	StateRunId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId
) VALUES (
	@Iteration,
	@StateId,
	@StateRunId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId
);
SELECT last_insert_rowid();";

		const string SqlInsertFaultMetric = @"
INSERT INTO FaultMetric (
	Iteration,
	MajorHash,
	MinorHash,
	Timestamp,
	Hour
) VALUES (
	@Iteration,
	@MajorHash,
	@MinorHash,
	@Timestamp,
	@Hour
);
SELECT last_insert_rowid();";

		const string SqlInsertFaultMetricMutation = @"
INSERT INTO FaultMetricMutation (
	FaultMetricId,
	MutationId
) VALUES (
	@FaultMetricId,
	@MutationId
)";

		const string SqlInsertFaultDetail = @"
INSERT INTO FaultDetail (
	Reproducable,
	Iteration,
	TimeStamp,
	BucketName,
	Source,
	Exploitability,
	MajorHash,
	MinorHash,
	Title,
	Description,
	Seed,
	IterationStart,
	IterationStop
) VALUES (
	@Reproducable,
	@Iteration,
	@TimeStamp,
	@BucketName,
	@Source,
	@Exploitability,
	@MajorHash,
	@MinorHash,
	@Title,
	@Description,
	@Seed,
	@IterationStart,
	@IterationStop
);
SELECT last_insert_rowid();";

		const string SqlInsertFaultFile = @"
INSERT INTO FaultFile (
	FaultDetailId,
	Name,
	FullName,
	Size
) VALUES (
	@FaultDetailId,
	@Name,
	@FullName,
	@Size
);
SELECT last_insert_rowid();";

		#endregion

		public SQLiteConnection Connection { get; private set; }

		static readonly IEnumerable<Type> EntityTypes = new[]
		{
			typeof(Job),
			typeof(FaultDetail),
			typeof(FaultFile),

			typeof(State),
			typeof(Action),
			typeof(Parameter),
			typeof(Element),
			typeof(Mutator),
			typeof(Dataset),
			typeof(Mutation),
			typeof(FaultMetric),
			typeof(FaultMetricMutation),
		};

		static readonly string[] Scripts =
		{
			Utilities.LoadStringResource(
				Assembly.GetExecutingAssembly(), 
				"Peach.Pro.Core.Resources.Metrics.sql"
			)
		};

		public JobContext(string dbPath)
		{
			var init = new SqliteInitializer(dbPath);

			var cnn = "Data Source=\"{0}\";Foreign Keys=True".Fmt(dbPath);
			Connection = new SQLiteConnection(cnn);
			Connection.Open();

			init.InitializeDatabase(Connection, EntityTypes, Scripts);
		}

		public void Dispose()
		{
			Connection.Dispose();
		}

		public Job GetJob(string id)
		{
			return Connection.Query<Job>(SqlSelectJob, new { Id = id })
				.SingleOrDefault();
		}

		public void UpsertJob(Job job, bool isNew)
		{
			Connection.Execute(isNew ? SqlInsertJob : SqlUpdateJob, job);
		}

		public void UpdateJob(Job job)
		{
			Connection.Execute(SqlUpdateJob, job);
		}

		public IEnumerable<T> LoadTable<T>()
		{
			var sql = "SELECT * FROM {0}".Fmt(typeof(T).Name);
			return Connection.Query<T>(sql);
		}

		public void InsertMetric(Metric metric)
		{
			var typeName = metric.GetType().Name;
			var sql = "INSERT INTO {0} (Name) VALUES (@Name); ".Fmt(typeName) +
				"SELECT last_insert_rowid();";
			metric.Id = Connection.ExecuteScalar<long>(sql, metric);
		}

		public void IncrementStateCount(long stateId)
		{
			const string sql = "UPDATE State SET Count = Count + 1 WHERE Id = @Id";
			Connection.Execute(sql, new { Id = stateId });
		}

		public void InsertMutation(Mutation entity)
		{
			entity.Id = Connection.ExecuteScalar<long>(SqlInsertMutation, entity);
		}

		public void InsertFaultMetric(FaultMetric fault, List<Mutation> mutations)
		{
			fault.Id = Connection.ExecuteScalar<long>(SqlInsertFaultMetric, fault);

			foreach (var mutation in mutations.Where(mutation => mutation.Id == 0))
			{
				mutation.Id = Connection.ExecuteScalar<long>(SqlInsertMutation, mutation);
			}

			var linking = mutations.Select(x => new
			{
				FaultMetricId = fault.Id,
				MutationId = x.Id,
			});
			Connection.Execute(SqlInsertFaultMetricMutation, linking);
		}

		public void InsertFault(FaultDetail fault)
		{
			fault.Id = Connection.ExecuteScalar<long>(SqlInsertFaultDetail, fault);

			foreach (var file in fault.Files)
			{
				file.FaultDetailId = fault.Id;
			}
			Connection.Execute(SqlInsertFaultFile, fault.Files);
		}

		public IEnumerable<StateMetric> QueryStates()
		{
			return Connection.Query<StateMetric>("SELECT * FROM ViewStates");
		}

		public IEnumerable<IterationMetric> QueryIterations()
		{
			return Connection.Query<IterationMetric>("SELECT * FROM ViewIterations");
		}

		public IEnumerable<BucketMetric> QueryBuckets()
		{
			return Connection.Query<BucketMetric>("SELECT * FROM ViewBuckets");
		}

		public IEnumerable<BucketTimelineMetric> QueryBucketTimeline()
		{
			return Connection.Query<BucketTimelineMetric>("SELECT * FROM ViewBucketTimeline");
		}

		public IEnumerable<MutatorMetric> QueryMutators()
		{
			return Connection.Query<MutatorMetric>("SELECT * FROM ViewMutators");
		}

		public IEnumerable<ElementMetric> QueryElements()
		{
			return Connection.Query<ElementMetric>("SELECT * FROM ViewElements");
		}

		public IEnumerable<DatasetMetric> QueryDatasets()
		{
			return Connection.Query<DatasetMetric>("SELECT * FROM ViewDatasets");
		}
	}
}
