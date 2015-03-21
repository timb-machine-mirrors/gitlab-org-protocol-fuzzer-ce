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
		const string SqlGetLastRowId = "SELECT last_insert_rowid();";
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
	HasMetrics = @HasMetrics,
	StartIteration = @StartIteration,
	CurrentIteration = @CurrentIteration
WHERE
	Id = @Id
;";

		const string SqlUpsertMutation = @"
INSERT OR REPLACE INTO Mutation (
	StateId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId,
	IterationCount
) VALUES (
	@StateId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId,
	COALESCE((
		SELECT IterationCount + 1
		FROM Mutation
		WHERE
			StateId = @StateId AND
			ActionId = @ActionId AND
			ParameterId = @ParameterId AND
			ElementId = @ElementId AND
			MutatorId = @MutatorId AND
			DatasetId = @DatasetId
	), 1)
);";

		const string SqlUpsertState = @"
INSERT OR REPLACE INTO [State] (
	Id, 
	NameId, 
	RunCount,
	Count
) VALUES (
	@Id, 
	@NameId, 
	@RunCount,
	@Count
);";

		const string SqlInsertFaultMetric = @"
INSERT INTO FaultMetric (
	Iteration,
	MajorHash,
	MinorHash,
	Timestamp,
	Hour,
	StateId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId
) VALUES (
	@Iteration,
	@MajorHash,
	@MinorHash,
	@Timestamp,
	@Hour,
	@StateId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId
);";

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
);" + SqlGetLastRowId;

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
);" + SqlGetLastRowId;


		#endregion

		public SQLiteConnection Connection { get; private set; }

		static readonly IEnumerable<Type> EntityTypes = new[]
		{
			// job status
			typeof(Job),
			typeof(FaultDetail),
			typeof(FaultFile),

			// metrics
			typeof(NamedItem),
			typeof(State),
			typeof(Mutation),
			typeof(FaultMetric),
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

		public void InsertNames(IEnumerable<NamedItem> items)
		{
			const string sql = "INSERT INTO NamedItem (Id, Name) VALUES (@Id, @Name)";
			Connection.Execute(sql, items);
		}

		public void UpdateStates(IEnumerable<State> states)
		{
			const string sql = "UPDATE State SET Count = @Count WHERE Id = @Id";
			Connection.Execute(sql, states);
		}

		public void UpsertStates(IEnumerable<State> states)
		{
			Connection.Execute(SqlUpsertState, states);
		}

		public void UpsertMutations(IEnumerable<Mutation> mutations)
		{
			Connection.Execute(SqlUpsertMutation, mutations);
		}

		public void InsertFaultMetrics(IEnumerable<FaultMetric> faults)
		{
			Connection.Execute(SqlInsertFaultMetric, faults);
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

		public IEnumerable<FaultTimelineMetric> QueryFaultTimeline()
		{
			return Connection.Query<FaultTimelineMetric>("SELECT * FROM ViewFaultTimeline");
		}

		public static void Dump<T>(IEnumerable<T> data)
		{
			var type = typeof(T);

			var columns = type.GetProperties()
				.Where(pi => !pi.HasAttribute<NotMappedAttribute>())
				.ToList();

			var maxWidth = new int[columns.Count];
			var header = new string[columns.Count];
			var rows = new List<string[]> { header };

			for (var i = 0; i < columns.Count; i++)
			{
				var pi = columns[i];
				header[i] = pi.Name;
				maxWidth[i] = pi.Name.Length;
			}

			foreach (var item in data)
			{
				var row = new string[columns.Count];
				var values = columns.Select(pi => pi.GetValue(item, null).ToString())
					.ToArray();
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];
					row[i] = value;
					maxWidth[i] = Math.Max(maxWidth[i], value.Length);
				}
				rows.Add(row);
			}

			var fmts = maxWidth
				.Select((t, i) => "{0},{1}".Fmt(i, t))
				.Select(fmt => "{" + fmt + "}")
				.ToList();
			var finalFmt = string.Join("|", fmts);
			foreach (object[] row in rows)
			{
				Console.WriteLine(finalFmt, row);
			}
		}
	}
}
