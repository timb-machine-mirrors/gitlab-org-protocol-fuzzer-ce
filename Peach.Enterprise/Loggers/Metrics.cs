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

CREATE TABLE metrics_states (
 id INTEGER PRIMARY KEY,
 state INTEGER,
 count INTEGER,
 FOREIGN KEY(state) REFERENCES states(id)
);

CREATE TABLE metrics_iterations (
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

CREATE UNIQUE INDEX metrics_index ON metrics_iterations (
 state,
 action,
 parameter,
 element,
 mutator,
 dataset
);";

		static string create_view = @"
CREATE VIEW view_metrics_states AS 
SELECT
	s.name as state,
	mi.count
FROM metrics_states AS mi
JOIN states AS s
on s.id = mi.state;

CREATE VIEW view_metrics_iterations AS 
SELECT
	s.name as state,
	a.name as action,
	p.name as parameter,
	e.name as element,
	m.name as mutator,
	ds.name as dataset,
	mi.count
FROM metrics_iterations AS mi
JOIN states AS s
on s.id = mi.state
JOIN actions AS a
on a.id = mi.action
JOIN parameters AS p
on p.id = mi.state
JOIN elements AS e
on e.id = mi.element
JOIN mutators AS m
on m.id = mi.mutator
JOIN datasets AS ds
on ds.id = mi.dataset;";

		static string select_foreign_key = @"
SELECT id FROM {0}s WHERE name = :name;";

		static string insert_foreign_key = @"
INSERT INTO {0}s (name) VALUES (:name); SELECT last_insert_rowid();";

		static string select_iteration = @"
SELECT id FROM metrics_iterations WHERE
 state = :state AND
 action = :action AND
 parameter = :parameter AND
 element = :element AND
 mutator = :mutator AND
 dataset = :dataset;
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
);";

		static string update_iteration = @"
UPDATE metrics_iterations SET count = count + 1 WHERE id = :id;";

		static string select_state = @"
SELECT id FROM metrics_states WHERE state = :state";

		static string insert_state = @"
INSERT INTO metrics_states ( state, count ) VALUES ( :state, 1 );
";

		static string update_state = @"
UPDATE metrics_states SET count = count + 1 WHERE id = :id;";

		static string[] foreignKeys = { "state", "action", "parameter", "element", "mutator", "dataset" };

		protected SQLiteConnection db;
		Sample sample;
		bool reproducingFault;

		Dictionary<string, KeyTracker> keyTracker = new Dictionary<string, KeyTracker>();

		SQLiteCommand select_iteration_cmd;
		SQLiteCommand insert_iteration_cmd;
		SQLiteCommand update_iteration_cmd;
		SQLiteCommand select_state_cmd;
		SQLiteCommand insert_state_cmd;
		SQLiteCommand update_state_cmd;

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

			try
			{
				SQLiteLog.Initialize();
			}
			catch (MissingMethodException)
			{
				throw new PeachException("Error, could not find native sqlite library.");
			}
		}

		public string Path
		{
			get;
			protected set;
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

			select_iteration_cmd = new SQLiteCommand(db);
			select_iteration_cmd.CommandText = select_iteration;

			insert_iteration_cmd = new SQLiteCommand(db);
			insert_iteration_cmd.CommandText = insert_iteration;

			foreach (var item in foreignKeys)
			{
				keyTracker.Add(item, new KeyTracker(db, item));

				select_iteration_cmd.Parameters.Add(new SQLiteParameter(item));
				insert_iteration_cmd.Parameters.Add(new SQLiteParameter(item));
			}

			update_iteration_cmd = new SQLiteCommand(db);
			update_iteration_cmd.CommandText = update_iteration;
			update_iteration_cmd.Parameters.Add(new SQLiteParameter("id"));

			select_state_cmd = new SQLiteCommand(db);
			select_state_cmd.CommandText = select_state;
			select_state_cmd.Parameters.Add(new SQLiteParameter("state"));

			insert_state_cmd = new SQLiteCommand(db);
			insert_state_cmd.CommandText = insert_state;
			insert_state_cmd.Parameters.Add(new SQLiteParameter("state"));

			update_state_cmd = new SQLiteCommand(db);
			update_state_cmd.CommandText = update_state;
			update_state_cmd.Parameters.Add(new SQLiteParameter("id"));
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			if (update_iteration_cmd != null)
			{
				update_iteration_cmd.Dispose();
				update_iteration_cmd = null;
			}

			if (insert_iteration_cmd != null)
			{
				insert_iteration_cmd.Dispose();
				insert_iteration_cmd = null;
			}

			if (select_iteration_cmd != null)
			{
				select_iteration_cmd.Dispose();
				select_iteration_cmd = null;
			}

			if (update_state_cmd != null)
			{
				update_state_cmd.Dispose();
				update_state_cmd = null;
			}

			if (insert_state_cmd != null)
			{
				insert_state_cmd.Dispose();
				insert_state_cmd = null;
			}

			if (select_state_cmd != null)
			{
				select_state_cmd.Dispose();
				select_state_cmd = null;
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

			var dom = state.parent.parent as Peach.Core.Dom.Dom;

			if (!reproducingFault && !dom.context.controlIteration && !dom.context.controlRecordingIteration)
				OnStateSample(sample.State);
		}

		protected override void Action_Starting(Core.Dom.Action action)
		{
			sample.Action = action.name;
		}

		protected override void MutationStrategy_DataMutating(Core.Dom.ActionData actionData, Core.Dom.DataElement element, Mutator mutator)
		{
			sample.DataSet = actionData.selectedData != null ? actionData.selectedData.name : "";
			sample.Parameter = actionData.name ?? "";
			sample.Element = element.fullName;
			sample.Mutator = mutator.name;

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
	}
}
