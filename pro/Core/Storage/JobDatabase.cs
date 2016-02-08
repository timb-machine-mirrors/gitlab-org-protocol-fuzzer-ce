using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using Peach.Core;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	public class JobDatabase : Database
	{
		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// fault data
			typeof(FaultDetail),
			typeof(FaultFile),

			// metrics
			typeof(NamedItem),
			typeof(State),
			typeof(Mutation),
			typeof(FaultMetric),
		};

		static readonly string[] StaticScripts =
		{
			Utilities.LoadStringResource(
				Assembly.GetExecutingAssembly(), 
				"Peach.Pro.Core.Resources.Metrics.sql"
			)
		};

		protected override IEnumerable<Type> Schema
		{
			get { return StaticSchema; }
		}

		protected override IEnumerable<string> Scripts
		{
			get { return StaticScripts; }
		}

		protected override IList<MigrationHandler> Migrations
		{
			get
			{
				return new MigrationHandler[]
				{
					() => { Connection.Execute(Sql.JobMigrateV1); },
					() => { Connection.Execute(Sql.JobMigrateV2); },
					() => { Connection.Execute(Sql.JobMigrateV3); },
				};
			}
		}

		public JobDatabase(string path)
			: base(path, true)
		{
		}

		public void InsertNames(IEnumerable<NamedItem> items)
		{
			Connection.Execute(Sql.InsertNames, items);
		}

		public void UpdateStates(IEnumerable<State> states)
		{
			Connection.Execute(Sql.UpdateStates, states);
		}

		public void UpsertStates(IEnumerable<State> states)
		{
			Connection.Execute(Sql.UpsertState, states);
		}

		public void UpsertMutations(IEnumerable<Mutation> mutations)
		{
			Connection.Execute(Sql.UpsertMutation, mutations);
		}

		public void InsertFaultMetrics(IEnumerable<FaultMetric> faults)
		{
			Connection.Execute(Sql.InsertFaultMetric, faults);
		}

		public void InsertFault(FaultDetail fault)
		{
			fault.Id = Connection.ExecuteScalar<long>(Sql.InsertFaultDetail, fault);

			foreach (var file in fault.Files)
			{
				file.FaultDetailId = fault.Id;
			}
			Connection.Execute(Sql.InsertFaultFile, fault.Files);
		}

		public FaultDetail GetFaultById(long id, bool loadFiles = true)
		{
			if (loadFiles)
			{
				const string sql = Sql.SelectFaultDetailById + Sql.SelectFaultFilesByFaultId;
				using (var multi = Connection.QueryMultiple(sql, new { Id = id }))
				{
					var fault = multi.Read<FaultDetail>().SingleOrDefault();
					if (fault == null)
						return null;
					fault.Files = multi.Read<FaultFile>().ToList();
					return fault;
				}
			}
			return Connection.Query<FaultDetail>(Sql.SelectFaultDetailById, new { Id = id })
				.SingleOrDefault();
		}

		public FaultFile GetFaultFileById(long id)
		{
			return Connection.Query<FaultFile>(Sql.SelectFaultFilesById, new { Id = id })
				.SingleOrDefault();
		}

		public IEnumerable<FaultMutation> GetFaultMutations(long iteration)
		{
			return Connection.Query<FaultMutation>(
				Sql.SelectMutationByIteration,
				new { Iteration = iteration }
			);
		}

		public Report GetReport(Job job)
		{
			var report = new Report
			{
				Job = job,
				BucketCount = Connection.ExecuteScalar<int>(Sql.SelectBucketCount),
				BucketDetails = LoadTable<BucketDetail>()
					.Select(m =>
					{
						m.Mutations = GetFaultMutations(m.Iteration);
						return m;
					}),
				MutatorMetrics = LoadTable<MutatorMetric>(),
				ElementMetrics = LoadTable<ElementMetric>(),
				StateMetrics = LoadTable<StateMetric>(),
				DatasetMetrics = LoadTable<DatasetMetric>(),
				BucketMetrics = LoadTable<BucketMetric>(),
			};

			return report;
		}
	}
}
