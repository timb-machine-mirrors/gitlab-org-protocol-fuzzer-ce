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
					MigrateV1,
					MigrateV2,
				};
			}
		}

		private void MigrateV1()
		{
			Connection.Execute(Sql.JobMigrateV1);
		}

		private void MigrateV2()
		{
			Connection.Execute(Sql.JobMigrateV2);
		}

		public JobDatabase(string path, bool doMigration = false)
			: base(path, true, doMigration)
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

				BucketDetails = LoadTable<BucketDetail>()
					.OrderByDescending(m => m.FaultCount)
					.ThenBy(m => m.MajorHash)
					.ThenBy(m => m.MinorHash)
					.ThenBy(m => m.Exploitability)
					.ToList(),

				MutatorMetrics = LoadTable<MutatorMetric>()
					.OrderByDescending(m => m.BucketCount)
					.ThenByDescending(m => m.FaultCount)
					.ThenByDescending(m => m.IterationCount)
					.ThenByDescending(m => m.ElementCount)
					.ThenBy(m => m.Mutator)
					.ToList(),

				ElementMetrics = LoadTable<ElementMetric>()
					.OrderByDescending(m => m.BucketCount)
					.ThenByDescending(m => m.FaultCount)
					.ThenByDescending(m => m.IterationCount)
					.ThenBy(m => m.State)
					.ThenBy(m => m.Action)
					.ThenBy(m => m.Element)
					.ToList(),

				StateMetrics = LoadTable<StateMetric>()
					.OrderByDescending(m => m.ExecutionCount)
					.ThenBy(m => m.State)
					.ToList(),

				DatasetMetrics = LoadTable<DatasetMetric>()
					.OrderByDescending(m => m.BucketCount)
					.ThenByDescending(m => m.FaultCount)
					.ThenByDescending(m => m.IterationCount)
					.ThenBy(m => m.Dataset)
					.ToList(),

				BucketMetrics = LoadTable<BucketMetric>()
					.OrderByDescending(m => m.FaultCount)
					.ThenByDescending(m => m.IterationCount)
					.ThenBy(m => m.Mutator)
					.ThenBy(m => m.Bucket)
					.ThenBy(m => m.Element)
					.ToList(),
			};

			foreach (var b in report.BucketDetails)
				b.Mutations = GetFaultMutations(b.Iteration).ToList();

			return report;
		}
	}
}
