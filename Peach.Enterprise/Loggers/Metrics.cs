using System;
using System.Collections.Generic;
using Peach.Core;
using System.Data.SQLite;

namespace Peach.Enterprise.Loggers
{
	public class Sample
	{
		public int Id { get; set; }

	}

	/// <summary>
	/// Logs fuzzing metrics to a SQLite database.
	/// </summary>
	[Logger("Metrics", true)]
	[Parameter("Path", typeof(string), "Log folder")]
	public class MetricsLogger : Logger
	{
		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
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

		private SQLiteConnection db;
		private SQLiteCommand add_cmd;
		private Sample sample;
		private bool reproducingFault;

		protected class Sample
		{
			public string State { get; set; }
			public string Action { get; set; }
			public string Parameter { get; set; }
			public string Element { get; set; }
			public string Mutator { get; set; }
			public string DataSet { get; set; }
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

			var cmd = new SQLiteCommand(db);

			cmd.CommandText = create_table;
			cmd.ExecuteNonQuery();

			cmd.CommandText = create_index;
			cmd.ExecuteNonQuery();

			add_cmd = new SQLiteCommand(db);
			add_cmd.Parameters.Add(new SQLiteParameter("state"));
			add_cmd.Parameters.Add(new SQLiteParameter("action"));
			add_cmd.Parameters.Add(new SQLiteParameter("parameter"));
			add_cmd.Parameters.Add(new SQLiteParameter("element"));
			add_cmd.Parameters.Add(new SQLiteParameter("mutator"));
			add_cmd.Parameters.Add(new SQLiteParameter("dataset"));
			add_cmd.Parameters.Add(new SQLiteParameter("id"));
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			db.Close();
			db = null;
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

		protected object GetForeignKey(string table, string name)
		{
			var cmd = new SQLiteCommand(db);
			cmd.Parameters.AddWithValue("name", name);
			cmd.CommandText = "select id from {0} where name = :name;".Fmt(table);
			object id = cmd.ExecuteScalar();
			if (id == null)
			{
				cmd.CommandText = "insert into {0} (name) values (:name); select last_insert_rowid();".Fmt(table);
				id = cmd.ExecuteScalar();
			}
			return id;
		}

		protected virtual void OnSample(Sample s)
		{
			add_cmd.CommandText = "BEGIN;";
			add_cmd.ExecuteNonQuery();

			add_cmd.Parameters["state"].Value = GetForeignKey("states", s.State);
			add_cmd.Parameters["action"].Value =  GetForeignKey("actions", s.Action);
			add_cmd.Parameters["parameter"].Value =  GetForeignKey("parameters", s.Parameter ?? "");
			add_cmd.Parameters["element"].Value =  GetForeignKey("elements", s.Element);
			add_cmd.Parameters["mutator"].Value =  GetForeignKey("mutators", s.Mutator);
			add_cmd.Parameters["dataset"].Value =  GetForeignKey("datasets", s.DataSet ?? "");

			add_cmd.CommandText = "select id from metrics where state = :state and action = :action and parameter = :parameter and element = :element and mutator = :mutator and dataset = :dataset";
			object sample_id = add_cmd.ExecuteScalar();
			if (sample_id == null)
			{
				add_cmd.CommandText = "insert into metrics (state, action, parameter, element, mutator, dataset, count) values (:state, :action, :parameter, :element, :mutator, :dataset, 1); COMMIT;";
				add_cmd.ExecuteNonQuery();
			}
			else
			{
				add_cmd.Parameters["id"].Value = sample_id;
				add_cmd.CommandText = "update metrics set count = count + 1 where id = :id; COMMIT;";
				add_cmd.ExecuteNonQuery();
			}
		}
	}
}
