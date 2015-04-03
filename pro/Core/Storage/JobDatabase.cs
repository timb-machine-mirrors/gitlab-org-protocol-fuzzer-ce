using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using Peach.Core;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	internal class JobDatabase : Database
	{
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

		public JobDatabase(string path)
			: base(path, true)
		{
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
				const string sql = Sql.SelectFaultDetailById + Sql.SelectFaultFilesById;
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
			const string sql = "SELECT * FROM FaultFile WHERE Id = @Id";
			return Connection.Query<FaultFile>(sql, new { Id = id })
				.SingleOrDefault();
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
	}
}
