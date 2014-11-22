using System;
using System.Collections.Generic;
using System.Linq;
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

		#region create tables query
		static string create_table = @"
PRAGMA automatic_index = false;

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
);

CREATE TABLE metrics_faults (
 id INTEGER PRIMARY KEY,
 state INTEGER,
 action INTEGER,
 parameter INTEGER,
 element INTEGER,
 mutator INTEGER,
 dataset INTEGER,
 bucket INTEGER,
 faultnumber INTEGER,
 FOREIGN KEY(state) REFERENCES states(id),
 FOREIGN KEY(action) REFERENCES actions(id),
 FOREIGN KEY(parameter) REFERENCES parameters(id),
 FOREIGN KEY(element) REFERENCES elements(id),
 FOREIGN KEY(mutator) REFERENCES mutators(id),
 FOREIGN KEY(dataset) REFERENCES datasets(id),
 FOREIGN KEY(bucket) REFERENCES buckets(id)
);

CREATE TABLE metrics_faultsbyhour (
	id INTEGER PRIMARY KEY,
	date DATE,
	hour INTEGER,
	faultcount INTEGER
);

CREATE TABLE buckets (
 id INTEGER PRIMARY KEY,
 type TEXT,
 majorhash TEXT,
 minorhash TEXT,
 name TEXT NOT NULL,
 timestamp DATE,
 firstiteration INTEGER,
 faultcount INTEGER
);
";
		#endregion

		#region create indices query
		static string create_index = @"
CREATE UNIQUE INDEX states_index on states ( name );

CREATE UNIQUE INDEX actions_index on actions ( name );

CREATE UNIQUE INDEX parameters_index on parameters ( name );

CREATE UNIQUE INDEX elements_index on elements ( name );

CREATE UNIQUE INDEX mutators_index on mutators ( name );

CREATE UNIQUE INDEX datasets_index on datasets ( name );

CREATE UNIQUE INDEX buckets_index on buckets ( name );

CREATE UNIQUE INDEX metrics_index ON metrics_iterations (
 state,
 action,
 parameter,
 element,
 mutator,
 dataset
);

CREATE INDEX metrics_mutator_index on metrics_iterations (mutator);
CREATE INDEX metrics_element_index on metrics_iterations (element);
CREATE INDEX metrics_dataset_index on metrics_iterations (dataset);


CREATE UNIQUE INDEX faults_index ON metrics_faults(
 state,
 action,
 parameter,
 element,
 mutator,
 dataset,
 bucket,
 faultnumber
);


CREATE INDEX faults_mutator_index ON metrics_faults (mutator);
CREATE INDEX faults_element_index ON metrics_faults (element);
CREATE INDEX faults_dataset_index on metrics_faults (dataset);

CREATE UNIQUE INDEX faultsbyhour_index on metrics_faultsbyhour (
	date,
	hour
);

";
		#endregion

		#region create views query
		static string create_view = @"
CREATE VIEW view_metrics_states AS 
SELECT
	s.name as state,
	mi.count as iterationcount
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
	mi.count as iterationcount
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
on ds.id = mi.dataset;

CREATE VIEW view_metrics_faults AS
SELECT
	s.name as state,
	a.name as action,
	p.name as parameter,
	e.name as element,
	m.name as mutator,
	ds.name as dataset,
  b.name as bucket,
	count(distinct mf.faultnumber) as faultcount
FROM metrics_faults AS mf
JOIN states AS s
on s.id = mf.state
JOIN actions AS a
on a.id = mf.action
JOIN parameters AS p
on p.id = mf.state
JOIN elements AS e
on e.id = mf.element
JOIN mutators AS m
on m.id = mf.mutator
JOIN datasets AS ds
on ds.id = mf.dataset
JOIN buckets AS b
ON b.id = mf.bucket
group by
	mf.state
	,mf.action
	,mf.parameter
	,mf.element
	,mf.mutator
	,mf.bucket
order by
	mf.state
	,mf.action
	,mf.parameter
	,mf.element
	,mf.mutator
	,mf.bucket;

CREATE VIEW view_buckets AS
SELECT
	b.id
	,b.name as bucket
	,m.name as mutator
	,case when length(p.name) > 0 then
		s.name || '.' || a.name || '.' || p.name || '.' || e.name
	else
		s.name || '.' || a.name || '.' || e.name
	end as element
	,sum(mi.count) as iterationcount
	,count(distinct(mf.faultnumber)) as faultcount
FROM metrics_faults AS mf
JOIN metrics_iterations AS mi ON 
	mi.state = mf.state AND
	mi.action = mf.action AND
	mi.parameter = mf.parameter AND
	mi.element = mf.element AND
	mi.mutator = mf.mutator AND
	mi.dataset = mf.dataset
JOIN buckets AS b
	on b.id = mf.bucket
JOIN states AS s
	on s.id = mf.state
JOIN actions AS a
	on a.id = mf.action
JOIN parameters AS p
	on p.id = mf.parameter
JOIN elements AS e
	on e.id = mf.element
JOIN mutators AS m
	on m.id = mf.mutator
GROUP BY 
	mf.bucket, 
	mf.mutator, 
	mf.state, 
	mf.action, 
	mf.parameter, 
	mf.element,
	mf.dataset
ORDER BY count(distinct(mf.faultnumber)) DESC;

CREATE VIEW view_buckettimeline AS
SELECT
	b.id,
	b.name as bucket,
	b.timestamp,
	b.type,
	b.majorhash,
	b.minorhash,
	b.firstiteration,
	b.faultcount 
FROM buckets AS b
WHERE b.type = 'minorHash';

CREATE VIEW view_distincts AS
	SELECT DISTINCT
		mutator
		,state
		,action
		,parameter
		,element
	FROM metrics_iterations;

CREATE VIEW view_mutator_elementcount AS
SELECT 
	mutator
	, count(*) as count
FROM view_distincts
GROUP BY mutator;

CREATE VIEW view_buckets_major AS
SELECT id
FROM buckets
WHERE type = 'majorHash';

CREATE VIEW view_buckets_minor AS
SELECT id
FROM buckets
WHERE type = 'minorHash';

CREATE VIEW view_mutators_faults AS
SELECT 
	mf.mutator, 
	count(distinct mf.bucket) as bucketcount, 
	count(distinct mf.faultnumber) as faultcount
FROM metrics_faults AS mf
WHERE mf.bucket in (SELECT id FROM view_buckets_major)
GROUP BY mf.mutator;

CREATE VIEW view_mutators_iterations AS
SELECT 
    mi.mutator, 
	ec.count as elementcount,
	sum(mi.count) as iterationcount
FROM view_mutator_elementcount AS ec
JOIN metrics_iterations AS mi ON
	ec.mutator = mi.mutator
GROUP BY mi.mutator;

CREATE VIEW view_mutators AS
SELECT
      m.name as mutator,
      mi.elementcount,
      mi.iterationcount,
      mf.bucketcount,
      mf.faultcount
FROM view_mutators_iterations AS mi
LEFT JOIN view_mutators_faults AS mf
ON mf.mutator = mi.mutator
JOIN mutators AS m
ON m.id = mi.mutator;

CREATE VIEW view_elements_iterations AS
SELECT
      state
      ,action
      ,parameter
      ,dataset 
      ,element
      ,sum(count) as iterationcount
FROM metrics_iterations
GROUP BY state,action,parameter,dataset,element;

CREATE VIEW view_elements_faults AS
SELECT
      state
      ,action
      ,parameter
      ,dataset
      ,element
      ,count(distinct(bucket)) as bucketcount
      ,count(distinct(faultnumber)) as faultcount
FROM metrics_faults
WHERE bucket in (SELECT id FROM view_buckets_major)
GROUP BY state,action,parameter,dataset,element;

CREATE VIEW view_elements AS
SELECT 
	s.name as state
	,a.name as action
	,p.name as parameter
	,d.name as dataset
	,e.name as element 
	,ei.iterationcount
	,ef.bucketcount
	,ef.faultcount
FROM view_elements_iterations AS ei
LEFT JOIN view_elements_faults AS ef ON
	ef.element = ei.element
	AND ef.state = ei.state
	AND ef.action = ei.action
	AND ef.parameter = ei.parameter
	AND ef.dataset = ei.dataset
JOIN elements AS e ON	e.id = ei.element
JOIN states AS s ON s.id = ei.state
JOIN actions AS a ON a.id = ei.action
JOIN parameters AS p ON p.id = ei.parameter
JOIN datasets AS d ON d.id = ei.dataset
WHERE ef.faultcount > 0
ORDER BY ef.faultcount DESC;

CREATE VIEW view_datasets_iterations AS
SELECT
	dataset
	,sum(count) as iterationcount
FROM metrics_iterations
GROUP BY dataset;

CREATE VIEW view_datasets_faults AS
SELECT
	dataset
	,count(distinct(bucket)) as bucketcount
	,count(distinct(faultnumber)) as faultcount
FROM metrics_faults
WHERE bucket in (SELECT id FROM view_buckets_major)
GROUP BY dataset;

CREATE VIEW view_datasets AS
SELECT
	d.name as dataset
	,di.iterationcount
	,df.bucketcount
	,df.faultcount
FROM view_datasets_iterations AS di
LEFT JOIN view_datasets_faults as df
ON df.dataset = di.dataset
JOIN datasets AS d ON d.id = di.dataset
WHERE df.faultcount > 0
ORDER BY df.bucketcount DESC
LIMIT 20;


";
		/*
			,case when length(p.name) > 0 then
				s.name || '.' || a.name || '.' || p.name || '.' || e.name
			else
				s.name || '.' || a.name || '.' || e.name
			end as element
		*/
		#endregion

		#region foreign key queries
		static string select_foreign_key = @"
SELECT id FROM {0}s WHERE name = :name;";

		static string insert_foreign_key = @"
INSERT INTO {0}s (name) VALUES (:name); SELECT last_insert_rowid();";
		#endregion

		#region iteration queries

		static string select_iteration = @"
SELECT id FROM metrics_iterations WHERE
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
);";

		static string update_iteration = @"
UPDATE metrics_iterations SET count = count + 1 WHERE id = :id;";
		#endregion

		#region bucket queries
		static string select_bucket = @"
SELECT id FROM buckets WHERE name = :name;
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
UPDATE buckets SET faultcount = faultcount + 1 WHERE id = :id;
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
SELECT id FROM metrics_faultsbyhour WHERE date = :date AND hour = :hour;
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
UPDATE metrics_faultsbyhour SET faultcount = faultcount + 1 WHERE id = :id;
";

		#endregion

		#region state queries
		static string select_state = @"
SELECT id FROM metrics_states WHERE state = :state";

		static string insert_state = @"
INSERT INTO metrics_states ( state, count ) VALUES ( :state, 1 );
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
			var connectionString = "Data Source=\"" + path + "\";Foreign Keys=True";

			db = new SQLiteConnection(connectionString);
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

			select_bucket_cmd = new SQLiteCommand(db);
			select_bucket_cmd.CommandText = select_bucket;
			select_bucket_cmd.Parameters.Add(new SQLiteParameter("name"));

			insert_bucket_cmd = new SQLiteCommand(db);
			insert_bucket_cmd.CommandText = insert_bucket;
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("name"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("timestamp"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("type"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("majorhash"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("minorhash"));
			insert_bucket_cmd.Parameters.Add(new SQLiteParameter("firstiteration"));

			update_bucket_cmd = new SQLiteCommand(db);
			update_bucket_cmd.CommandText = update_bucket;
			update_bucket_cmd.Parameters.Add(new SQLiteParameter("id"));

			insert_fault_cmd = new SQLiteCommand(db);
			insert_fault_cmd.CommandText = insert_fault;
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("state"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("action"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("parameter"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("element"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("mutator"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("dataset"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("bucket"));
			insert_fault_cmd.Parameters.Add(new SQLiteParameter("faultnumber"));

			select_faultsbyhour_cmd = new SQLiteCommand(db);
			select_faultsbyhour_cmd.CommandText = select_faultsbyhour;
			select_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("date"));
			select_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("hour"));

			insert_faultsbyhour_cmd = new SQLiteCommand(db);
			insert_faultsbyhour_cmd.CommandText = insert_faultsbyhour;
			insert_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("date"));
			insert_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("hour"));

			update_faultsbyhour_cmd = new SQLiteCommand(db);
			update_faultsbyhour_cmd.CommandText = update_faultsbyhour;
			update_faultsbyhour_cmd.Parameters.Add(new SQLiteParameter("id"));

			if (ConnectionOpened != null)
				ConnectionOpened(db);
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			var fileLogger = context.test.loggers.OfType<FileLogger>().FirstOrDefault();
			if (fileLogger != null)
				fileLogger.FaultSaved -= FaultSaved;

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

			if (select_bucket_cmd != null)
			{
				select_bucket_cmd.Dispose();
				select_bucket_cmd = null;
			}

			if (insert_bucket_cmd != null)
			{
				insert_bucket_cmd.Dispose();
				insert_bucket_cmd = null;
			}

			if (update_bucket_cmd != null)
			{
				update_bucket_cmd.Dispose();
				update_bucket_cmd = null;
			}

			if (insert_fault_cmd != null)
			{
				insert_fault_cmd.Dispose();
				insert_fault_cmd = null;
			}

			if (select_faultsbyhour_cmd != null)
			{
				select_faultsbyhour_cmd.Dispose();
				select_faultsbyhour_cmd = null;
			}

			if (insert_faultsbyhour_cmd != null)
			{
				insert_faultsbyhour_cmd.Dispose();
				insert_faultsbyhour_cmd = null;
			}

			if (update_faultsbyhour_cmd != null)
			{
				update_faultsbyhour_cmd.Dispose();
				update_faultsbyhour_cmd = null;
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

			//TODO: Clear Sample list
			samples.Clear();
		}

		protected override void StateStarting(RunContext context, Peach.Core.Dom.State state)
		{
			sample.State = state.name;

			var dom = state.parent.parent;

			if (!reproducingFault && !dom.context.controlIteration && !dom.context.controlRecordingIteration)
				OnStateSample(sample.State);
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			sample.Action = action.name;
		}

		protected override void DataMutating(RunContext context, Peach.Core.Dom.ActionData actionData, Peach.Core.Dom.DataElement element, Mutator mutator)
		{
			sample.DataSet = actionData.selectedData != null ? actionData.selectedData.name : "";
			sample.Parameter = actionData.name ?? "";
			sample.Element = element.fullName;
			sample.Mutator = mutator.name;

			//TODO: save copy of sample in a list, use ObjectCopier
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

					//TODO: Add row to buckets or increment fault count, one row for major, one row for major:minor
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

					//TODO: Add row to metrics_faults for each Sample in Sample list using keys from both metrics_buckets entries, add row or increment
					//* // I really dunno about this...
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
					//*/

				}

				trans.Commit();
			}
		}
	}
}
