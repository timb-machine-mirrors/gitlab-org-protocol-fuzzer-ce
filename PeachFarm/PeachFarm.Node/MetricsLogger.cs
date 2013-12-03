using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common.Messages;
using PeachFarm.Common;
using System.Data;
using System.Data.SQLite;
using Peach.Core;

namespace PeachFarm.Loggers
{
	[Peach.Core.Logger("MetricsRabbit", true)]
	[Parameter("Path", typeof(string), "Log folder")]
	[Peach.Core.Parameter("JobID", typeof(string), "JobID")]
	[Peach.Core.Parameter("RabbitHost", typeof(string), "RabbitHost")]
	[Peach.Core.Parameter("RabbitPort", typeof(int), " RabbitPort")]
	[Peach.Core.Parameter("RabbitUser", typeof(string), "RabbitUser")]
	[Peach.Core.Parameter("RabbitPassword", typeof(string), "RabbitPassword")]
	[Peach.Core.Parameter("RabbitUseSSL", typeof(bool), "RabbitUseSSL")]
	class MetricsRabbitLogger : Peach.Enterprise.Loggers.MetricsLogger
	{
		int samplecount = 0;
		const int samplecountmax = 10;

		public string RabbitHost { get; private set; }
		public int RabbitPort { get; private set; }
		public string RabbitUser { get; private set; }
		public string RabbitPassword { get; private set; }
		public bool RabbitUseSSL { get; private set; }
		public string JobID { get; private set; }


		public MetricsRabbitLogger(Dictionary<string, Peach.Core.Variant> args)
			:base(args)
		{
			//JobID = (string)args["JobID"];
			//RabbitHost = (string)args["RabbitHost"];
			//RabbitPort = (int)args["RabbitPort"];
			//RabbitUser = (string)args["RabbitUser"];
			//RabbitPassword = (string)args["RabbitPassword"];
			//RabbitUseSSL = (bool)args["RabbitUseSSL"];
		}

		//TODO override methods and call SendRows
		protected override void Engine_TestFinished(RunContext context)
		{
			if (db != null)
				SendRows();
			base.Engine_TestFinished(context);
		}

		private void SendRows()
		{
			JobProgressNotification notification = new JobProgressNotification(JobID);
			notification.JobID = JobID;

			using (DataTable dt = SelectAllFrom("view_metrics_iterations"))
			{
				Truncate("metrics_iterations");

				foreach (DataRow row in dt.Rows)
				{
					notification.IterationMetrics.Add(new IterationMetric()
					{
						Action = (string)row["action"],
						DataElement = (string)row["element"],
						DataSet = (string)row["dataset"],
						IterationCount = Convert.ToUInt32(row["count"]),
						Mutator = (string)row["mutator"],
						State = (string)row["state"],
						Parameter = (string)row["parameter"]
					});
				}
			}

			using (DataTable dt = SelectAllFrom("view_metrics_states"))
			{
				Truncate("metrics_states");

				foreach (DataRow row in dt.Rows)
				{
					notification.StateMetrics.Add(new StateMetric()
					{
						ExecutionCount = Convert.ToUInt32(row["count"]),
						State = (string)row["state"]
					});
				}
			}
			

			if ((notification.IterationMetrics.Count > 0) || (notification.StateMetrics.Count > 0))
			{
				RabbitMqHelper rabbit = new RabbitMqHelper(RabbitHost, RabbitPort, RabbitUser, RabbitPassword, RabbitUseSSL);
				rabbit.PublishToQueue(QueueNames.QUEUE_REPORTGENERATOR, notification.Serialize(), Actions.NotifyJobProgress);
				rabbit.CloseConnection();
				rabbit = null;
			}
		}

		protected override void OnIterationSample(Sample s)
		{
			base.OnIterationSample(s);
			samplecount++;
			if (samplecount == samplecountmax)
			{
				SendRows();
				samplecount = 0;
			}
		}

		private DataTable SelectAllFrom(string table)
		{
			var dt = new DataTable();


			using (SQLiteCommand sqlitecmd = db.CreateCommand())
			{
				sqlitecmd.CommandText = String.Format("select * from {0}", table);
				using (var reader = sqlitecmd.ExecuteReader())
				{
					dt.Load(reader);
				}
			}

			return dt;
		}

		private void Truncate(string table)
		{
			using (SQLiteCommand sqlitecmd = db.CreateCommand())
			{
				sqlitecmd.CommandText = String.Format("delete from {0}", table);
				sqlitecmd.ExecuteNonQuery();
			}
		}
	}
}
