using System;
using System.Collections.Generic;
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
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId
) VALUES (
	@Iteration,
	@StateId,
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
			typeof(StateInstance),
			typeof(Mutation),
			typeof(FaultMetric),
			typeof(FaultMetricMutation),
		};

		public JobContext(string dbPath)
		{
			var init = new SqliteContextInitializer(dbPath);

			var cnn = "Data Source=\"{0}\";Foreign Keys=True".Fmt(dbPath);
			Connection = new SQLiteConnection(cnn);
			Connection.Open();

			init.InitializeDatabase(Connection, EntityTypes);
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
			var sql = "INSERT INTO {0} (Name) VALUES (@Name); ".Fmt(metric.GetType().Name) +
			          "SELECT last_insert_rowid();";
			metric.Id = Connection.ExecuteScalar<long>(sql, metric);
		}

		public void InsertStateInstance(StateInstance si)
		{
			const string sql = "INSERT INTO StateInstance (StateId) VALUES (@StateId); " +
			                   "SELECT last_insert_rowid();";
			si.Id = Connection.ExecuteScalar<long>(sql, si);
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
	}

	//[DbConfigurationType(typeof(SqliteConfiguration))] 
	//class JobContext : DbContext
	//{
	//	static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	//	public DbSet<Job> Jobs { get; set; }
	//	public DbSet<FaultDetail> Faults { get; set; }

	//	public DbSet<State> States { get; set; }
	//	public DbSet<Action> Actions { get; set; }
	//	public DbSet<Parameter> Parameters { get; set; }
	//	public DbSet<Element> Elements { get; set; }
	//	public DbSet<Mutator> Mutators { get; set; }
	//	public DbSet<Dataset> Datasets { get; set; }

	//	public DbSet<StateInstance> StateInstances { get; set; }
	//	public DbSet<Mutation> Mutations { get; set; }
	//	public DbSet<FaultMetric> FaultMetrics { get; set; }

	//	private string _dbPath;

	//	public JobContext(string path)
	//		: base(new SQLiteConnection
	//		{
	//			ConnectionString = new SQLiteConnectionStringBuilder
	//			{
	//				DataSource = path,
	//				ForeignKeys = true,
	//				BinaryGUID = false,
	//			}.ConnectionString
	//		}, true)
	//	{
	//		_dbPath = path;
	//		Database.Log = msg => logger.Trace(msg.TrimEnd());
	//	}

	//	protected override void OnModelCreating(DbModelBuilder modelBuilder)
	//	{
	//		modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
	//		Database.SetInitializer(new SqliteContextInitializer<JobContext>(_dbPath, modelBuilder));
	//	}

	//	class MutationGroup
	//	{
	//		public long State { get; set; }
	//		public long Action { get; set; }
	//		public long Parameter { get; set; }
	//		public long Element { get; set; }
	//		public long Mutator { get; set; }
	//		public long Dataset { get; set; }
	//	}

	//	class IterationGroup : MutationGroup
	//	{
	//		public long Iteration { get; set; }
	//	}

	//	class FaultGroup : MutationGroup
	//	{
	//		public long Fault { get; set; }
	//	}

	//	class DistinctMutations
	//	{
	//		public MutationGroup Group { get; set; }
	//		public long Count { get; set; }
	//	}

	//	class MutationsByIteration
	//	{
	//		public IterationGroup Group { get; set; }
	//		public long Count { get; set; }
	//	}

	//	class MutationsByFault
	//	{
	//		public FaultGroup Group { get; set; }
	//		public long Count { get; set; }
	//	}

	//	private IQueryable<DistinctMutations> GetDistinctMutations()
	//	{
	//		return
	//			from x in Mutations
	//			group x by new MutationGroup
	//			{
	//				State = x.State.Id,
	//				Action = x.Action.Id,
	//				Parameter = x.Parameter.Id,
	//				Element = x.Element.Id,
	//				Mutator = x.Mutator.Id,
	//				Dataset = x.Dataset.Id,
	//			} into g
	//			select new DistinctMutations
	//			{
	//				Group = g.Key,
	//				Count = g.Count()
	//			};
	//	}

	//	private IQueryable<MutationsByIteration> GetMutationsByIteration()
	//	{
	//		return
	//			from x in Mutations
	//			group x by new IterationGroup
	//			{
	//				Iteration = x.Iteration,
	//				State = x.State.Id,
	//				Action = x.Action.Id,
	//				Parameter = x.Parameter.Id,
	//				Element = x.Element.Id,
	//				Mutator = x.Mutator.Id,
	//				Dataset = x.Dataset.Id,
	//			} into g
	//			select new MutationsByIteration
	//			{
	//				Group = g.Key,
	//				Count = g.Count()
	//			};
	//	}

	//	private IQueryable<MutationsByFault> GetMutationsByFault()
	//	{
	//		return
	//			from x in FaultMetrics
	//			from y in x.Mutations
	//			group y by new FaultGroup
	//			{
	//				Fault = x.Id,
	//				Mutator = y.Mutator.Id,
	//				State = y.State.Id,
	//				Action = y.Action.Id,
	//				Parameter = y.Parameter.Id,
	//				Element = y.Element.Id,
	//				Dataset = y.Dataset.Id,
	//			} into g
	//			select new MutationsByFault
	//			{
	//				Group = g.Key,
	//				Count = g.Count()
	//			};
	//	}

	//	// view_metrics_states
	//	public IQueryable<StateMetric> QueryStates()
	//	{
	//		var query =
	//			from x in StateInstances
	//			group x by x.State into g
	//			select new StateMetric
	//			{
	//				State = g.Key.Name,
	//				ExecutionCount = g.Count()
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0}, Count: {1}",
	//				item.State,
	//				item.ExecutionCount);
	//		}

	//		return query;
	//	}

	//	// view_metrics_iterations
	//	public IQueryable<IterationMetric> QueryIterations()
	//	{
	//		var query =
	//			from g in GetMutationsByIteration()
	//			from s in States
	//			from a in Actions
	//			from p in Parameters
	//			from e in Elements
	//			from m in Mutators
	//			from d in Datasets
	//			where
	//				g.Group.State == s.Id &&
	//				g.Group.Action == a.Id &&
	//				g.Group.Parameter == p.Id &&
	//				g.Group.Element == e.Id &&
	//				g.Group.Mutator == m.Id &&
	//				g.Group.Dataset == d.Id
	//			select new IterationMetric
	//			{
	//				State = s.Name,
	//				Action = a.Name,
	//				Parameter = p.Name,
	//				Element = e.Name,
	//				Mutator = m.Name,
	//				Dataset = d.Name,
	//				IterationCount = g.Count,
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0,2}|{1,5}|{2,5}|{3,50}|{4,30}|{5,6}| Count: {6}",
	//				item.State,
	//				item.Action,
	//				item.Parameter,
	//				item.Element,
	//				item.Mutator,
	//				item.Dataset,
	//				item.IterationCount);
	//		}

	//		return query;
	//	}

	//	// view_buckets
	//	public IQueryable<BucketMetric> QueryBuckets()
	//	{
	//		var mutationsByIteration = GetMutationsByIteration();
	//		var mutationsByFault = GetMutationsByFault();

	//		var query =
	//			from x in mutationsByIteration
	//			from y in mutationsByFault
	//			from s in States
	//			from a in Actions
	//			from p in Parameters
	//			from e in Elements
	//			from m in Mutators
	//			from d in Datasets
	//			from f in FaultMetrics
	//			where
	//				x.Group.Iteration == f.Iteration &&
	//				x.Group.State == y.Group.State &&
	//				x.Group.Action == y.Group.Action &&
	//				x.Group.Parameter == y.Group.Parameter &&
	//				x.Group.Element == y.Group.Element &&
	//				x.Group.Mutator == y.Group.Mutator &&
	//				x.Group.Dataset == y.Group.Dataset &&
	//				x.Group.State == s.Id &&
	//				x.Group.Action == a.Id &&
	//				x.Group.Parameter == p.Id &&
	//				x.Group.Element == e.Id &&
	//				x.Group.Mutator == m.Id &&
	//				x.Group.Dataset == d.Id &&
	//				y.Group.Fault == f.Id
	//			orderby y.Count descending
	//			select new BucketMetric
	//			{
	//				Bucket = f.MajorHash + "_" + f.MinorHash,
	//				Mutator = m.Name,
	//				Element = (p.Name != "") ?
	//					s.Name + "." + a.Name + "." + p.Name + "." + e.Name :
	//					s.Name + "." + a.Name + "." + e.Name,
	//				IterationCount = x.Count,
	//				FaultCount = y.Count,
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0}|{1,25}|{2,60}, IC: {3,3}, FC: {4,3}",
	//				item.Bucket,
	//				item.Mutator,
	//				item.Element,
	//				item.IterationCount,
	//				item.FaultCount);
	//		}

	//		return query;
	//	}

	//	// view_buckettimeline
	//	public IQueryable<BucketTimelineMetric> QueryBucketTimeline()
	//	{
	//		var groups =
	//			from x in FaultMetrics
	//			group x by new
	//			{
	//				x.MajorHash,
	//				x.MinorHash,
	//			} into g
	//			select new
	//			{
	//				g.Key,
	//				FaultCount = g.Select(x => x.Iteration).Distinct().Count(),
	//				FirstIteration = g.Min(x => x.Iteration),
	//				FirstTimestamp = g.Min(x => x.Timestamp),
	//			};

	//		var query =
	//			from g in groups
	//			from x in FaultMetrics
	//			where
	//				g.Key.MajorHash == x.MajorHash &&
	//				g.Key.MinorHash == x.MinorHash &&
	//				g.FirstIteration == x.Iteration
	//			select new BucketTimelineMetric
	//			{
	//				Label = x.MajorHash + "_" + x.MinorHash,
	//				Iteration = g.FirstIteration,
	//				Time = g.FirstTimestamp,
	//				FaultCount = g.FaultCount,
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0}|{1}, First: {2}, Count: {3}",
	//				item.Label,
	//				item.Time,
	//				item.Iteration,
	//				item.FaultCount);
	//		}

	//		return query;
	//	}

	//	public IQueryable<MutatorMetric> QueryMutators()
	//	{
	//		// view_distincts
	//		var distinctMutations = GetDistinctMutations();

	//		// view_mutator_elementcount
	//		var mutatorGroups =
	//			from x in distinctMutations
	//			group x by x.Group.Mutator into g
	//			select new
	//			{
	//				Mutator = g.Key,
	//				Count = g.Count()
	//			};

	//		// view_mutators_faults
	//		var faultsByMutator =
	//			from x in FaultMetrics
	//			from y in x.Mutations
	//			group x by y.Mutator.Id into g
	//			select new
	//			{
	//				Mutator = g.Key,
	//				BucketCount = g.Select(x => x.MajorHash).Distinct().Count(),
	//				FaultCount = g.Select(x => x.Iteration).Distinct().Count(),
	//			};

	//		// view_mutators_iterations
	//		var iterationsByMutator =
	//			from x in mutatorGroups
	//			join y in Mutations on
	//				x.Mutator equals y.Mutator.Id into g
	//			select new
	//			{
	//				Mutator = x.Mutator,
	//				ElementCount = x.Count,
	//				IterationCount = g.Select(z => z.Iteration).Distinct().Count(),
	//			};

	//		// view_mutators
	//		var query =
	//			from x in iterationsByMutator
	//			join y in Mutators on
	//				x.Mutator equals y.Id
	//			join z in faultsByMutator on
	//				x.Mutator equals z.Mutator into g
	//			from j in g.DefaultIfEmpty()
	//			select new MutatorMetric
	//			{
	//				Mutator = y.Name,
	//				ElementCount = x.ElementCount,
	//				IterationCount = x.IterationCount,
	//				BucketCount = (j == null) ? 0 : j.BucketCount,
	//				FaultCount = (j == null) ? 0 : j.FaultCount,
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0,30}, EC: {1,3}, IC: {2,3}, BC: {3,3}, FC: {4,3}",
	//				item.Mutator,
	//				item.ElementCount,
	//				item.IterationCount,
	//				item.BucketCount,
	//				item.FaultCount);
	//		}

	//		return query;
	//	}

	//	public IQueryable<ElementMetric> QueryElements()
	//	{
	//		// view_elements_iterations
	//		var iterationsByElement =
	//			from x in Mutations
	//			group x by new
	//			{
	//				State = x.StateId,
	//				Action = x.ActionId,
	//				Parameter = x.ParameterId,
	//				Dataset = x.DatasetId,
	//				Element = x.ElementId,
	//			} into g
	//			select new
	//			{
	//				g.Key.State,
	//				g.Key.Action,
	//				g.Key.Parameter,
	//				g.Key.Dataset,
	//				g.Key.Element,
	//				IterationCount = g.Count(),
	//			};

	//		// view_elements_faults
	//		var faultsByElement =
	//			from x in FaultMetrics
	//			from y in x.Mutations
	//			group x by new
	//			{
	//				State = y.StateId,
	//				Action = y.ActionId,
	//				Parameter = y.ParameterId,
	//				Dataset = y.DatasetId,
	//				Element = y.ElementId,
	//			} into g
	//			select new
	//			{
	//				g.Key.State,
	//				g.Key.Action,
	//				g.Key.Parameter,
	//				g.Key.Dataset,
	//				g.Key.Element,
	//				BucketCount = g.Select(x => x.MajorHash).Distinct().Count(),
	//				FaultCount = g.Select(x => x.Iteration).Distinct().Count(),
	//			};

	//		// view_elements
	//		var query =
	//			from x in iterationsByElement
	//			join y in faultsByElement on new
	//			{
	//				x.State,
	//				x.Action,
	//				x.Parameter,
	//				x.Dataset,
	//				x.Element
	//			}
	//			equals new
	//			{
	//				y.State,
	//				y.Action,
	//				y.Parameter,
	//				y.Dataset,
	//				y.Element
	//			} into g
	//			from j in g.DefaultIfEmpty()
	//			join s in States on x.State equals s.Id
	//			join a in Actions on x.Action equals a.Id
	//			join p in Parameters on x.Parameter equals p.Id
	//			join d in Datasets on x.Dataset equals d.Id
	//			join e in Elements on x.Element equals e.Id
	//			orderby j.FaultCount descending
	//			select new ElementMetric
	//			{
	//				State = s.Name,
	//				Action = a.Name,
	//				Parameter = p.Name,
	//				Dataset = d.Name,
	//				Element = e.Name,
	//				IterationCount = x.IterationCount,
	//				BucketCount = (j == null) ? 0 : j.BucketCount,
	//				FaultCount = (j == null) ? 0 : j.FaultCount,
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0,2}|{1,5}|{2,2}|{3,6}|{4,45}, IC: {5,3}, BC: {6,3}, FC: {7,3}",
	//				item.State,
	//				item.Action,
	//				item.Parameter,
	//				item.Dataset,
	//				item.Element,
	//				item.IterationCount,
	//				item.BucketCount,
	//				item.FaultCount);
	//		}

	//		return query;
	//	}

	//	public IQueryable<DatasetMetric> QueryDatasets()
	//	{
	//		// view_datasets_iterations
	//		var iterationsByDataset =
	//			from x in Mutations
	//			group x by x.DatasetId into g
	//			select new { Dataset = g.Key, IterationCount = g.Count() };

	//		// view_datasets_faults
	//		var faultsByDataset =
	//			from x in FaultMetrics
	//			from y in x.Mutations
	//			group x by y.DatasetId into g
	//			select new
	//			{
	//				Dataset = g.Key,
	//				BucketCount = g.Select(x => x.MajorHash).Distinct().Count(),
	//				FaultCount = g.Select(x => x.Iteration).Distinct().Count(),
	//			};

	//		// view_datasets
	//		var query =
	//			from x in iterationsByDataset
	//			join y in faultsByDataset on x.Dataset equals y.Dataset into g
	//			from j in g.DefaultIfEmpty()
	//			join d in Datasets on x.Dataset equals d.Id
	//			orderby j.BucketCount descending
	//			select new DatasetMetric
	//			{
	//				Dataset = d.Name,
	//				IterationCount = x.IterationCount,
	//				BucketCount = (j == null) ? 0 : j.BucketCount,
	//				FaultCount = (j == null) ? 0 : j.FaultCount,
	//			};

	//		foreach (var item in query)
	//		{
	//			Console.WriteLine("{0,20}, IC: {1,3}, BC: {2,3}, FC: {3,3}",
	//				item.Dataset,
	//				item.IterationCount,
	//				item.BucketCount,
	//				item.FaultCount);
	//		}

	//		return query;
	//	}
	//}
}
