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
CREATE TABLE metrics (
 id INTEGER PRIMARY KEY,
 state TEXT NOT NULL,
 action TEXT NOT NULL,
 parameter TEXT NOT NULL,
 element TEXT NOT NULL,
 mutator TEXT NOT NULL,
 dataset TEXT NOT NULL,
 count INTEGER
);";

		static string create_index = @"
CREATE UNIQUE INDEX metrics_index ON metrics (
 state,
 action,
 parameter,
 element,
 mutator,
 dataset
);";

/*
BEGIN;
 INSERT OR IGNORE INTO metrics
  (state, action, parameter, element, mutator, dataset, count)
  VALUES
  ("state1", "action1", "parameter1", "element1", "mutator1", "", 0);
 UPDATE metrics SET count = count + 1 WHERE
  state = "state1" AND
  action = "action1" AND
  parameter = "parameter1" AND
  element = "element1" AND
  mutator = "mutator1" AND
  dataset = "";
COMMIT;
SELECT * FROM metrics;
*/

		static string add_sample = @"
BEGIN;
 INSERT OR IGNORE INTO metrics
  (state, action, parameter, element, mutator, dataset, count)
  VALUES
  (:state, :action, :parameter, :element, :mutator, :dataset, 0);
 UPDATE metrics SET count = count + 1 WHERE
  state = :state AND
  action = :action AND
  parameter = :parameter AND
  element = :element AND
  mutator = :mutator AND
  dataset = :dataset;
COMMIT;";


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

			db = new SQLiteConnection("data source=\"" + path + "\"");
			db.Open();

			var cmd = new SQLiteCommand(db);

			cmd.CommandText = create_table;
			cmd.ExecuteNonQuery();

			cmd.CommandText = create_index;
			cmd.ExecuteNonQuery();

			add_cmd = new SQLiteCommand(db);
			add_cmd.CommandText = add_sample;
			add_cmd.Parameters.Add("state", System.Data.DbType.String);
			add_cmd.Parameters.Add("action", System.Data.DbType.String);
			add_cmd.Parameters.Add("parameter", System.Data.DbType.String);
			add_cmd.Parameters.Add("element", System.Data.DbType.String);
			add_cmd.Parameters.Add("mutator", System.Data.DbType.String);
			add_cmd.Parameters.Add("dataset", System.Data.DbType.String);
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
			sample.Element = elementName;
			sample.Mutator = mutatorName;

			if (!reproducingFault)
				OnSample(sample);
		}

		protected virtual void OnSample(Sample s)
		{
			add_cmd.Parameters["state"].Value = s.State;
			add_cmd.Parameters["action"].Value = s.Action;
			add_cmd.Parameters["parameter"].Value = s.Parameter ?? "";
			add_cmd.Parameters["element"].Value = s.Element;
			add_cmd.Parameters["mutator"].Value = s.Mutator;
			add_cmd.Parameters["dataset"].Value = s.DataSet ?? "";

			add_cmd.ExecuteNonQuery();
		}
	}
}
