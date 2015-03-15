using Peach.Pro.Core.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	class JobContext : DbContext
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public DbSet<Job> Jobs { get; set; }
		public DbSet<FaultDetail> Faults { get; set; }
	
		public DbSet<State> States { get; set; }
		public DbSet<Action> Actions { get; set; }
		public DbSet<Parameter> Parameters { get; set; }
		public DbSet<Element> Elements { get; set; }
		public DbSet<Mutator> Mutators { get; set; }
		public DbSet<Dataset> Datasets { get; set; }
		public DbSet<Bucket> Buckets { get; set; }

		public DbSet<StateInstance> StateInstances { get; set; }
		public DbSet<Sample> Samples { get; set; }
		public DbSet<FaultMetric> FaultMetrics { get; set; }

		//public DbSet<FaultMetricSample> FaultSamples { get; set; }

		public JobContext(string path)
			: base(new SQLiteConnection
			{
				ConnectionString = new SQLiteConnectionStringBuilder
				{
					DataSource = path,
					ForeignKeys = true,
					BinaryGUID = false,
				}.ConnectionString
			}, true)
		{
			Database.Log = msg => logger.Trace(msg.TrimEnd());
			Database.SetInitializer(new SqliteContextInitializer<JobContext>(path));
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
			//modelBuilder.Entity<FaultMetric>()
			//	.HasMany<Sample>(x => x.Samples)
			//	.WithMany(x => x.Faults)
			//	.Map(x =>
			//	{
			//		x.MapLeftKey("FaultMetricId");
			//		x.MapRightKey("SampleId");
			//		x.ToTable("FaultMetricSample");
			//	});
		}
	}
}
