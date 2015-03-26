using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using Peach.Core;
using System.IO;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	internal class JobDatabase : NodeDatabase
	{
		#region SQL
		const string SqlGetLastRowId = "SELECT last_insert_rowid();";

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

		const string SqlSelectFaultById = @"
SELECT * FROM FaultDetail WHERE Id = @Id;
SELECT * FROM FaultFile WHERE FaultDetailId = @Id;
";


		#endregion

		protected override IEnumerable<Type> Schema
		{
			get { return StaticSchema; }
		}

		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// live job status
			typeof(Job),

			// fault data
			typeof(FaultDetail),
			typeof(FaultFile),

			// metrics
			typeof(NamedItem),
			typeof(State),
			typeof(Mutation),
			typeof(FaultMetric),
		};

		protected override IEnumerable<string> Scripts
		{
			get { return StaticScripts; }
		}

		static readonly string[] StaticScripts =
		{
			Utilities.LoadStringResource(
				Assembly.GetExecutingAssembly(), 
				"Peach.Pro.Core.Resources.Metrics.sql"
			)
		};

		public JobDatabase(Guid guid)
			: base(GetDatabasePath(guid))
		{
		}

		// used by unit tests
		internal JobDatabase(string path)
			: base(path)
		{
		}

		public static string GetStorageDirectory(Guid guid)
		{
			var config = Utilities.GetUserConfig();
			var logRoot = config.AppSettings.Settings.Get("LogPath");
			if (string.IsNullOrEmpty(logRoot))
				logRoot = Utilities.GetAppResourcePath("db");

			var logPath = System.IO.Path.Combine(logRoot, guid.ToString());
			if (!Directory.Exists(logPath))
				Directory.CreateDirectory(logPath);

			return logPath;
		}

		static string GetDatabasePath(Guid guid)
		{
			return System.IO.Path.Combine(GetStorageDirectory(guid), "job.db");
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

		public FaultDetail GetFaultById(long id)
		{
			using (var multi = Connection.QueryMultiple(SqlSelectFaultById, new { Id = id }))
			{
				var fault = multi.Read<FaultDetail>().SingleOrDefault();
				fault.Files = multi.Read<FaultFile>().ToList();
				return fault;
			}
		}

		public FaultFile GetFaultFileById(long id)
		{
			const string sql = "SELECT * FROM FaultFile WHERE Id = @Id";
			return Connection.Query<FaultFile>(sql, new { Id = id })
				.SingleOrDefault();
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
	}
}
