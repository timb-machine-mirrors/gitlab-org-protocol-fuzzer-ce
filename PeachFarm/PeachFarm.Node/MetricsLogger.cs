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
	[Logger("MetricsRabbit", true)]
	[Parameter("Path", typeof(string), "Log folder")]
	[Parameter("JobID", typeof(string), "JobID")]
	[Parameter("Target", typeof(string), "Target")]
	[Parameter("RabbitHost", typeof(string), "RabbitHost")]
	[Parameter("RabbitPort", typeof(int), " RabbitPort")]
	[Parameter("RabbitUser", typeof(string), "RabbitUser")]
	[Parameter("RabbitPassword", typeof(string), "RabbitPassword")]
	[Parameter("RabbitUseSSL", typeof(bool), "RabbitUseSSL")]
	class MetricsRabbitLogger : Peach.Enterprise.Loggers.MetricsLogger
	{
		//int samplecount = 0;
		const int samplecountmax = 1000;

		string filepath;
		string rabbithost;
		int rabbitport;
		string rabbituser;
		string rabbitpassword;
		bool rabbitusessl;
		string jobid;
		string target;

		public MetricsRabbitLogger(Dictionary<string, Peach.Core.Variant> args)
			:base(args)
		{
			filepath = (string)args["Path"];
			rabbithost = (string)args["RabbitHost"];
			rabbitport = (int)args["RabbitPort"];
			rabbituser = (string)args["RabbitUser"];
			rabbitpassword = (string)args["RabbitPassword"];
			rabbitusessl = (bool)args["RabbitUseSSL"];
			jobid = (string)args["JobID"];
			target = (string)args["Target"];
		}

		//TODO override methods and call SendRows

		private void SendRows()
		{
			DataTable dt = SelectAllFrom(filepath, "metrics");
			
			JobProgressNotification notification = new JobProgressNotification();

			foreach(DataRow row in dt.Rows)
			{
				IterationMetric i = new IterationMetric();
				i.Action = (string)row["action"];
				i.DataElement = (string)row["element"];
				i.DataSet = (string)row["dataset"];
				i.IterationCount = (uint)row["count"];
				i.JobID = jobid;
				i.Mutator = (string)row["mutator"];
				i.State = (string)row["state"];
				i.Target = target;
				notification.IterationMetrics.Add(i);
			}

			RabbitMqHelper rabbit = new RabbitMqHelper(rabbithost, rabbitport, rabbituser, rabbitpassword, rabbitusessl);
			rabbit.PublishToQueue(QueueNames.QUEUE_REPORTGENERATOR, notification.Serialize(), Actions.NotifyJobProgress);
			rabbit.CloseConnection();
			rabbit = null;
		}

		private static DataTable SelectAllFrom(string file, string table, bool truncate = false)
		{
			var dt = new DataTable();
			var sqliteconnstr = String.Format("Data Source={0}", file);
			SQLiteCommand sqlitecmd;

			using (var sqliteconn = new SQLiteConnection(sqliteconnstr))
			{
				sqliteconn.Open();
				using (sqlitecmd = sqliteconn.CreateCommand())
				{
					sqlitecmd.CommandText = String.Format("select * from {0}", table);
					using (var reader = sqlitecmd.ExecuteReader())
					{
						dt.Load(reader);
					}
				}

				if (truncate)
				{
					using (sqlitecmd = sqliteconn.CreateCommand())
					{
						sqlitecmd.CommandText = String.Format("truncate table {0}", table);
						sqlitecmd.ExecuteNonQuery();
					}
				}
			}
			return dt;
		}
	}
}
