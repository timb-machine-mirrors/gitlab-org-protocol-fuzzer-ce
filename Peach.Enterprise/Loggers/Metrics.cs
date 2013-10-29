using System;
using System.Collections.Generic;
using Peach.Core;
using System.Data.SQLite;

namespace Peach.Enterprise.Loggers
{
	/// <summary>
	/// Logs fuzzing metrics to a SQLite database.
	/// </summary>
	[Logger("Metrics", true)]
	[Parameter("Path", typeof(string), "Log folder")]
	public class MetricsLogger : Logger
	{
		private static string fileName = "metrics.sqlite";

		static string create_table = @"
CREATE TABLE states (
 id INTEGER PRIMARY KEY,
 name TEXT NOT NULL
);

CREATE TABLE actions (
 id INTEGER PRIMARY KEY,
 name TEXT NOT NULL
);

CREATE TABLE parameters (
 id INTEGER PRIMARY KEY,
 name TEXT NOT NULL
);

CREATE TABLE elements (
 id INTEGER PRIMARY KEY,
 name TEXT NOT NULL
);

CREATE TABLE mutators (
 id INTEGER PRIMARY KEY,
 name TEXT NOT NULL
);

CREATE TABLE datasets (
 id INTEGER PRIMARY KEY,
 name TEXT NOT NULL
);

CREATE TABLE metrics (
 id INTEGER PRIMARY KEY,
 state INTEGER,
 action INTEGER,
 parameter INTEGER,
 element INTEGER,
 mutator INTEGER,
 dataset INTEGER,
 count INTEGER,
 FOREIGN KEY(state) REFERENCES states(id),
 FOREIGN KEY(action) REFERENCES actions(id),
 FOREIGN KEY(parameter) REFERENCES parameters(id),
 FOREIGN KEY(element) REFERENCES elements(id),
 FOREIGN KEY(mutator) REFERENCES mutators(id),
 FOREIGN KEY(dataset) REFERENCES datasets(id)
);";

		static string create_index = @"
CREATE UNIQUE INDEX states_index on states ( name );

CREATE UNIQUE INDEX actions_index on actions ( name );

CREATE UNIQUE INDEX parameters_index on parameters ( name );

CREATE UNIQUE INDEX elements_index on elements ( name );

CREATE UNIQUE INDEX mutators_index on mutators ( name );

CREATE UNIQUE INDEX datasets_index on datasets ( name );

CREATE UNIQUE INDEX metrics_index ON metrics (
 state,
 action,
 parameter,
 element,
 mutator,
 dataset
);";

		static string create_view = @"
CREATE VIEW all_metrics AS SELECT * FROM metrics;
";

		static string select_foreign_key = @"
SELECT id FROM {0}s WHERE name = :name;";

		static string insert_foreign_key = @"
INSERT INTO {0}s (name) VALUES (:name); SELECT last_insert_rowid();";

		static string select_metric = @"
SELECT id FROM metrics WHERE
 state = :state AND
 action = :action AND
 parameter = :parameter AND
 element = :element AND
 mutator = :mutator AND
 dataset = :dataset;
";

		static string insert_metric = @"
INSERT INTO metrics (
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
);";

		static string update_metric = @"
UPDATE metrics SET count = count + 1 WHERE id = :id;";

		static string[] foreignKeys = { "state", "action", "parameter", "element", "mutator", "dataset" };

		SQLiteConnection db;
		Sample sample;
		bool reproducingFault;

		Dictionary<string, KeyTracker> keyTracker = new Dictionary<string, KeyTracker>();

		SQLiteCommand select_metric_cmd;
		SQLiteCommand insert_metric_cmd;
		SQLiteCommand update_metric_cmd;

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
				select_cmd = new SQLiteCommand(db);
				select_cmd.CommandText = select_foreign_key.Fmt(table);
				select_cmd.Parameters.Add("name", System.Data.DbType.String);

				insert_cmd = new SQLiteCommand(db);
				insert_cmd.CommandText = insert_foreign_key.Fmt(table);
				insert_cmd.Parameters.Add("name", System.Data.DbType.String);
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

		public MetricsLogger(Dictionary<string, Variant> args)
		{
			ParameterParser.Parse(this, args);

			sample = new Sample();
		}

		public string Path
		{
			get;
			private set;
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			var dir = GetLogPath(context, Path);
			System.IO.Directory.CreateDirectory(dir);
			var path = System.IO.Path.Combine(dir, fileName);

			db = new SQLiteConnection("Data Source=\"" + path + "\";Foreign Keys=True");
			db.Open();

			using (var trans = db.BeginTransaction())
			{
				using (var cmd = new SQLiteCommand(db))
				{
					cmd.CommandText = create_table;
					cmd.ExecuteNonQuery();

					cmd.CommandText = create_index;
					cmd.ExecuteNonQuery();

					cmd.CommandText = create_view;
					cmd.ExecuteNonQuery();
				}

				trans.Commit();
			}

			select_metric_cmd = new SQLiteCommand(db);
			select_metric_cmd.CommandText = select_metric;

			insert_metric_cmd = new SQLiteCommand(db);
			insert_metric_cmd.CommandText = insert_metric;

			foreach (var item in foreignKeys)
			{
				keyTracker.Add(item, new KeyTracker(db, item));

				select_metric_cmd.Parameters.Add(new SQLiteParameter(item));
				insert_metric_cmd.Parameters.Add(new SQLiteParameter(item));
			}

			update_metric_cmd = new SQLiteCommand(db);
			update_metric_cmd.CommandText = update_metric;
			update_metric_cmd.Parameters.Add(new SQLiteParameter("id"));
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			if (update_metric_cmd != null)
			{
				update_metric_cmd.Dispose();
				update_metric_cmd = null;
			}

			if (insert_metric_cmd != null)
			{
				insert_metric_cmd.Dispose();
				insert_metric_cmd = null;
			}

			if (select_metric_cmd != null)
			{
				select_metric_cmd.Dispose();
				select_metric_cmd = null;
			}

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
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
		}

		protected override void State_Starting(Core.Dom.State state)
		{
			sample.State = state.name;
		}

		protected override void Action_Starting(Core.Dom.Action action)
		{
			sample.Action = action.name;
			//sample.DataSet = action.dataSet.Selected.name;
		}

		protected override void MutationStrategy_Mutating(string elementName, string mutatorName)
		{
			//sample.Parameter = "";
			sample.Element = elementName;
			sample.Mutator = mutatorName;

			if (!reproducingFault)
				OnSample(sample);
		}

		protected virtual void OnSample(Sample s)
		{
			using (var trans = db.BeginTransaction())
			{
				object id;

				id = keyTracker["state"].Get(s.State);
				select_metric_cmd.Parameters["state"].Value = id;
				insert_metric_cmd.Parameters["state"].Value = id;

				id = keyTracker["action"].Get(s.Action);
				select_metric_cmd.Parameters["action"].Value = id;
				insert_metric_cmd.Parameters["action"].Value = id;

				id = keyTracker["parameter"].Get(s.Parameter ?? "");
				select_metric_cmd.Parameters["parameter"].Value = id;
				insert_metric_cmd.Parameters["parameter"].Value = id;

				id = keyTracker["element"].Get(s.Element);
				select_metric_cmd.Parameters["element"].Value = id;
				insert_metric_cmd.Parameters["element"].Value = id;

				id = keyTracker["mutator"].Get(s.Mutator);
				select_metric_cmd.Parameters["mutator"].Value = id;
				insert_metric_cmd.Parameters["mutator"].Value = id;

				id = keyTracker["dataset"].Get(s.DataSet ?? "");
				select_metric_cmd.Parameters["dataset"].Value = id;
				insert_metric_cmd.Parameters["dataset"].Value = id;

				id = select_metric_cmd.ExecuteScalar();
				if (id == null)
				{
					insert_metric_cmd.ExecuteNonQuery();
				}
				else
				{
					update_metric_cmd.Parameters["id"].Value = id;
					update_metric_cmd.ExecuteNonQuery();
				}

				trans.Commit();
			}
		}
	}
}
