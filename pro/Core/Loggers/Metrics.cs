using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
#if MONO
using Mono.Data.Sqlite;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
#else
using System.Data.SQLite;
#endif
using Peach.Core;

namespace Peach.Pro.Core.Loggers
{
	/// <summary>
	/// Logs fuzzing metrics to a SQLite database.
	/// </summary>
	[Logger("Metrics", true)]
	[Parameter("Path", typeof(string), "Log folder")]
	public class MetricsLogger : Logger
	{
		private static string fileName = "metrics.sqlite";
		private List<Sample> samples = new List<Sample>();

		private static string schema = Utilities.LoadStringResource(
			Assembly.GetExecutingAssembly(), "Peach.Pro.Core.Resources.Metrics.sql"
		);

		#region foreign key queries
		static string select_foreign_key = @"
SELECT id 
FROM {0}s 
WHERE name = :name;
";

		static string insert_foreign_key = @"
INSERT INTO {0}s (name) VALUES (:name); 
SELECT last_insert_rowid();";
		#endregion

		#region iteration queries

		static string select_iteration = @"
SELECT id FROM metrics_iterations 
WHERE
	state = :state AND
	action = :action AND
	parameter = :parameter AND
	element = :element AND
	mutator = :mutator AND
	dataset = :dataset
";

		static string insert_iteration = @"
INSERT INTO metrics_iterations (
	state,
	action,
	parameter,
	element,
	mutator,
	dataset,
	count
) VALUES (
	:state,
	:action,
	:parameter,
	:element,
	:mutator,
	:dataset,
	1
);
";

		static string update_iteration = @"
UPDATE metrics_iterations 
SET count = count + 1 
WHERE id = :id;
";
		#endregion

		#region bucket queries
		static string select_bucket = @"
SELECT id 
FROM buckets 
WHERE name = :name;
";

		static string insert_bucket = @"
INSERT INTO buckets (
	name,
	timestamp,
	type,
	majorhash,
	minorhash,
	firstiteration,
	faultcount
) VALUES (
	:name,
	:timestamp,
	:type,
	:majorhash,
	:minorhash,
	:firstiteration,
	1
);

SELECT last_insert_rowid();
";
		static string update_bucket = @"
UPDATE buckets 
SET faultcount = faultcount + 1 
WHERE id = :id;
";
		#endregion

		#region fault queries
		static string insert_fault = @"
INSERT INTO metrics_faults (
	state,
	action,
	parameter,
	element,
	mutator,
	dataset,
	bucket,
	faultnumber
) VALUES (
	:state,
	:action,
	:parameter,
	:element,
	:mutator,
	:dataset,
	:bucket,
	:faultnumber
);
";

		static string select_faultsbyhour = @"
SELECT id 
FROM metrics_faultsbyhour 
WHERE date = :date AND hour = :hour;
";

		static string insert_faultsbyhour = @"
INSERT INTO metrics_faultsbyhour (
	date,
	hour,
	faultcount
) VALUES (
	:date,
	:hour,
	1
);
";

		static string update_faultsbyhour = @"
UPDATE metrics_faultsbyhour 
SET faultcount = faultcount + 1 
WHERE id = :id;
";

		#endregion

		#region state queries
		static string select_state = @"
SELECT id 
FROM metrics_states 
WHERE state = :state
";

		static string insert_state = @"
INSERT INTO metrics_states ( 
	state, 
	count 
) VALUES ( 
	:state, 
	1 
);
";

		static string update_state = @"
UPDATE metrics_states SET count = count + 1 WHERE id = :id;";
		#endregion

		static string[] foreignKeys =
		{
			"state", 
			"action", 
			"parameter", 
			"element", 
			"mutator", 
			"dataset", 
			"bucket"
		};

		public SQLiteConnection db;
		readonly Sample sample = new Sample();
		bool reproducingFault;

		Dictionary<string, KeyTracker> keyTracker = new Dictionary<string, KeyTracker>();

		SQLiteCommand select_iteration_cmd;
		SQLiteCommand insert_iteration_cmd;
		SQLiteCommand update_iteration_cmd;
		SQLiteCommand select_state_cmd;
		SQLiteCommand insert_state_cmd;
		SQLiteCommand update_state_cmd;
		SQLiteCommand insert_fault_cmd;
		SQLiteCommand select_bucket_cmd;
		SQLiteCommand insert_bucket_cmd;
		SQLiteCommand update_bucket_cmd;
		SQLiteCommand select_faultsbyhour_cmd;
		SQLiteCommand insert_faultsbyhour_cmd;
		SQLiteCommand update_faultsbyhour_cmd;

		[Serializable]
		protected class Sample
		{
			public string State { get; set; }
			public string Action { get; set; }
			public string Parameter { get; set; }
			public string Element { get; set; }
			public string Mutator { get; set; }
			public string DataSet { get; set; }
		}

		class KeyTracker : IDisposable
		{
			public KeyTracker(SQLiteConnection db, string table)
			{
				select_cmd = new SQLiteCommand(select_foreign_key.Fmt(table), db);
				select_cmd.Parameters.Add("name", DbType.String);

				insert_cmd = new SQLiteCommand(insert_foreign_key.Fmt(table), db);
				insert_cmd.Parameters.Add("name", DbType.String);
			}

			public void Dispose()
			{
				select_cmd.Dispose();
				insert_cmd.Dispose();
			}

			public object Get(string name)
			{
				select_cmd.Parameters[0].Value = name;
				object id = select_cmd.ExecuteScalar();
				if (id == null)
				{
					insert_cmd.Parameters[0].Value = name;
					id = insert_cmd.ExecuteScalar();
				}
				return id;
			}

			SQLiteCommand select_cmd;
			SQLiteCommand insert_cmd;
		}

		/// <summary>
		/// Delegate for database connection event.
		/// </summary>
		/// <param name="connection">Database connection</param>
		public delegate void ConnectionOpenedEvent(SQLiteConnection connection);

		public event ConnectionOpenedEvent ConnectionOpened;

		public MetricsLogger(Dictionary<string, Variant> args)
		{
			ParameterParser.Parse(this, args);
		}

		public string Path
		{
			get;
			protected set;
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			var fileLogger = context.test.loggers.OfType<FileLogger>().FirstOrDefault();
			if (fileLogger != null)
				fileLogger.FaultSaved += FaultSaved;

			var dir = GetLogPath(context, Path);
			System.IO.Directory.CreateDirectory(dir);
			var path = System.IO.Path.Combine(dir, fileName);

			var parts = new[] 
			{
				"Data Source=\"{0}\"".Fmt(path),
				"Foreign Keys=True",
				"PRAGMA journal_mode=WAL",
			};
			var connectionString = string.Join(";", parts);

			db = new SQLiteConnection(connectionString);
			db.Open();

			using (var trans = db.BeginTransaction())
			{
				using (var cmd = new SQLiteCommand(schema, db))
				{
					cmd.ExecuteNonQuery();
				}

				trans.Commit();
			}

			select_iteration_cmd = new SQLiteCommand(select_iteration, db);
			insert_iteration_cmd = new SQLiteCommand(insert_iteration, db);

			foreach (var item in foreignKeys)
			{
				keyTracker.Add(item, new KeyTracker(db, item));

				select_iteration_cmd.Parameters.Add(new SQLiteParameter(item));
				insert_iteration_cmd.Parameters.Add(new SQLiteParameter(item));
			}

			update_iteration_cmd = new SQLiteCommand(update_iteration, db);
			update_iteration_cmd.Parameters.Add(new SQLiteParameter("id"));

			select_state_cmd = new SQLiteCommand(select_state, db);
			select_state_cmd.Parameters.Add(new SQLiteParameter("state"));

			insert_state_cmd = new SQLiteCommand(insert_state, db);
			insert_state_cmd.Parameters.Add(new SQLiteParameter("state"));

			update_state_cmd = new SQLiteCommand(update_state, db);
			update_state_cmd.Parameters.Add(new SQLiteParameter("id"));

			select_bucket_cmd = new SQLiteCommand(select_bucket, db);
			select_bucket_cmd.Parameters.Add(new SQLiteParameter("name"));

			insert_bucket_cmd = new SQLiteCommand(insert_bucket, db);
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("name"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("timestamp"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("type"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("majorhash"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("minorhash"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("firstiteration"));

			update_bucket_cmd = new SQLiteCommand(update_bucket, db);
			update_bucket_cmd.Parameters.Add(new SQLiteParameter("id"));

			insert_fault_cmd = new SQLiteCommand(insert_fault, db);
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("state"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("action"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("parameter"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("element"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("mutator"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("dataset"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("bucket"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("faultnumber"));

			select_faultsbyhour_cmd = new SQLiteCommand(select_faultsbyhour, db);
			select_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("date"));
			select_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("hour"));

			insert_faultsbyhour_cmd = new SQLiteCommand(insert_faultsbyhour, db);
			insert_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("date"));
			insert_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("hour"));

			update_faultsbyhour_cmd = new SQLiteCommand(update_faultsbyhour, db);
			update_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("id"));

			if (ConnectionOpened != null)
				ConnectionOpened(db);
		}

		private void CleanupCommand(IDisposable cmd)
		{
			if (cmd != null)
				cmd.Dispose();
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			var fileLogger = context.test.loggers.OfType<FileLogger>().FirstOrDefault();
			if (fileLogger != null)
				fileLogger.FaultSaved -= FaultSaved;

			CleanupCommand(update_iteration_cmd);
			CleanupCommand(insert_iteration_cmd);
			CleanupCommand(select_iteration_cmd);
	
			CleanupCommand(update_state_cmd);
			CleanupCommand(insert_state_cmd);
			CleanupCommand(select_state_cmd);

			CleanupCommand(update_bucket_cmd);
			CleanupCommand(insert_bucket_cmd);
			CleanupCommand(select_bucket_cmd);

			CleanupCommand(insert_fault_cmd);
			CleanupCommand(select_faultsbyhour_cmd);
			CleanupCommand(insert_faultsbyhour_cmd);
			CleanupCommand(update_faultsbyhour_cmd);

			foreach (var kv in keyTracker)
				kv.Value.Dispose();

			keyTracker.Clear();

			if (db != null)
			{
				db.Close();
				db = null;
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			reproducingFault = context.reproducingFault;
			samples.Clear();
		}

		protected override void StateStarting(RunContext context, Peach.Core.Dom.State state)
		{
			sample.State = state.Name;

			var dom = state.parent.parent;

			if (!reproducingFault && !dom.context.controlIteration && !dom.context.controlRecordingIteration)
				OnStateSample(sample.State);
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			sample.Action = action.Name;
		}

		protected override void DataMutating(RunContext context, Peach.Core.Dom.ActionData actionData, Peach.Core.Dom.DataElement element, Mutator mutator)
		{
			sample.DataSet = actionData.selectedData != null ? actionData.selectedData.Name : "";
			sample.Parameter = actionData.Name ?? "";
			sample.Element = element.fullName;
			sample.Mutator = mutator.Name;

			samples.Add(ObjectCopier.Clone(sample));

			if (!reproducingFault)
				OnIterationSample(sample);
		}

		protected virtual void OnStateSample(string state)
		{
			using (var trans = db.BeginTransaction())
			{
				object id;

				id = keyTracker["state"].Get(state);
				select_state_cmd.Parameters["state"].Value = id;
				insert_state_cmd.Parameters["state"].Value = id;

				id = select_state_cmd.ExecuteScalar();
				if (id == null)
				{
					insert_state_cmd.ExecuteNonQuery();
				}
				else
				{
					update_state_cmd.Parameters["id"].Value = id;
					update_state_cmd.ExecuteNonQuery();
				}

				trans.Commit();
			}
		}

		protected virtual void OnIterationSample(Sample s)
		{
			using (var trans = db.BeginTransaction())
			{
				object id;

				id = keyTracker["state"].Get(s.State);
				select_iteration_cmd.Parameters["state"].Value = id;
				insert_iteration_cmd.Parameters["state"].Value = id;

				id = keyTracker["action"].Get(s.Action);
				select_iteration_cmd.Parameters["action"].Value = id;
				insert_iteration_cmd.Parameters["action"].Value = id;

				id = keyTracker["parameter"].Get(s.Parameter ?? "");
				select_iteration_cmd.Parameters["parameter"].Value = id;
				insert_iteration_cmd.Parameters["parameter"].Value = id;

				id = keyTracker["element"].Get(s.Element);
				select_iteration_cmd.Parameters["element"].Value = id;
				insert_iteration_cmd.Parameters["element"].Value = id;

				id = keyTracker["mutator"].Get(s.Mutator);
				select_iteration_cmd.Parameters["mutator"].Value = id;
				insert_iteration_cmd.Parameters["mutator"].Value = id;

				id = keyTracker["dataset"].Get(s.DataSet ?? "");
				select_iteration_cmd.Parameters["dataset"].Value = id;
				insert_iteration_cmd.Parameters["dataset"].Value = id;

				id = select_iteration_cmd.ExecuteScalar();
				if (id == null)
				{
					insert_iteration_cmd.ExecuteNonQuery();
				}
				else
				{
					update_iteration_cmd.Parameters["id"].Value = id;
					update_iteration_cmd.ExecuteNonQuery();
				}

				trans.Commit();
			}
		}

		private uint faultCount = 0;

		private void FaultSaved(FileLogger.Category category, Fault fault, string rootPath)
		{
			if (category == FileLogger.Category.Reproducing)
				return;

			var bucketdelimiter = @"_";
			using (var trans = db.BeginTransaction())
			{
				var now = DateTime.Now;

				var date = now.Date;
				var hour = now.Hour;

				select_faultsbyhour_cmd.Parameters["date"].Value = date;
				select_faultsbyhour_cmd.Parameters["hour"].Value = hour;
				var faultbyhourid = select_faultsbyhour_cmd.ExecuteScalar();
				if (faultbyhourid == null)
				{
					insert_faultsbyhour_cmd.Parameters["date"].Value = date;
					insert_faultsbyhour_cmd.Parameters["hour"].Value = hour;
					insert_faultsbyhour_cmd.ExecuteNonQuery();
				}
				else
				{
					update_faultsbyhour_cmd.Parameters["id"].Value = faultbyhourid;
					update_faultsbyhour_cmd.ExecuteNonQuery();
				}

				var buckets = new List<string>(2);
				fault.majorHash = fault.majorHash ?? "UNKNOWN";
				fault.minorHash = fault.minorHash ?? "UNKNOWN";
				buckets.Add(fault.majorHash);
				buckets.Add(fault.majorHash + bucketdelimiter + fault.minorHash);

				object bucketid;

				faultCount++;

				foreach (var bucket in buckets)
				{
					select_bucket_cmd.Parameters["name"].Value = bucket;
					bucketid = select_bucket_cmd.ExecuteScalar();

					if (bucketid == null)
					{
						insert_bucket_cmd.Parameters["name"].Value = bucket;
						insert_bucket_cmd.Parameters["timestamp"].Value = now;
						insert_bucket_cmd.Parameters["type"].Value = bucket.Contains(bucketdelimiter) ? "minorHash" : "majorHash";
						insert_bucket_cmd.Parameters["majorhash"].Value = fault.majorHash;
						insert_bucket_cmd.Parameters["minorhash"].Value = bucket.Contains(bucketdelimiter) ? fault.minorHash : null;
						insert_bucket_cmd.Parameters["firstiteration"].Value = fault.iteration;
						bucketid = insert_bucket_cmd.ExecuteScalar();
					}
					else
					{
						update_bucket_cmd.Parameters["id"].Value = bucketid;
						update_bucket_cmd.ExecuteNonQuery();
					}

					foreach (var s in samples)
					{
						insert_fault_cmd.Parameters["state"].Value = keyTracker["state"].Get(s.State);
						insert_fault_cmd.Parameters["action"].Value = keyTracker["action"].Get(s.Action);
						insert_fault_cmd.Parameters["parameter"].Value = keyTracker["parameter"].Get(s.Parameter);
						insert_fault_cmd.Parameters["element"].Value = keyTracker["element"].Get(s.Element);
						insert_fault_cmd.Parameters["mutator"].Value = keyTracker["mutator"].Get(s.Mutator);
						insert_fault_cmd.Parameters["dataset"].Value = keyTracker["dataset"].Get(s.DataSet);
						insert_fault_cmd.Parameters["bucket"].Value = bucketid;
						insert_fault_cmd.Parameters["faultnumber"].Value = faultCount;
						insert_fault_cmd.ExecuteNonQuery();
					}
				}

				trans.Commit();
			}
		}
	}
}
