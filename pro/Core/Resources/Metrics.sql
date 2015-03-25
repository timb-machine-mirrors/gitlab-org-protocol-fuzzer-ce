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
	FOREIGN KEY(state)     REFERENCES states(id),
	FOREIGN KEY(action)    REFERENCES actions(id),
	FOREIGN KEY(parameter) REFERENCES parameters(id),
	FOREIGN KEY(element)   REFERENCES elements(id),
	FOREIGN KEY(mutator)   REFERENCES mutators(id),
	FOREIGN KEY(dataset)   REFERENCES datasets(id)
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
	FOREIGN KEY(state)     REFERENCES states(id),
	FOREIGN KEY(action)    REFERENCES actions(id),
	FOREIGN KEY(parameter) REFERENCES parameters(id),
	FOREIGN KEY(element)   REFERENCES elements(id),
	FOREIGN KEY(mutator)   REFERENCES mutators(id),
	FOREIGN KEY(dataset)   REFERENCES datasets(id),
	FOREIGN KEY(bucket)    REFERENCES buckets(id)
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

CREATE UNIQUE INDEX states_index ON states (name);
CREATE UNIQUE INDEX actions_index ON actions (name);
CREATE UNIQUE INDEX parameters_index ON parameters (name);
CREATE UNIQUE INDEX elements_index ON elements (name);
CREATE UNIQUE INDEX mutators_index ON mutators (name);
CREATE UNIQUE INDEX datasets_index ON datasets (name);
CREATE UNIQUE INDEX buckets_index ON buckets (name);
CREATE UNIQUE INDEX metrics_index ON metrics_iterations (
	state,
	action,
	parameter,
	element,
	mutator,
	dataset
);

CREATE INDEX metrics_mutator_index ON metrics_iterations (mutator);
CREATE INDEX metrics_element_index ON metrics_iterations (element);
CREATE INDEX metrics_dataset_index ON metrics_iterations (dataset);

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

CREATE UNIQUE INDEX faultsbyhour_index ON metrics_faultsbyhour (
	date,
	hour
);

CREATE VIEW view_metrics_states AS 
SELECT
	s.name AS state,
	mi.count AS iterationcount
FROM metrics_states AS mi
JOIN states AS s ON s.id = mi.state;

CREATE VIEW view_metrics_iterations AS 
SELECT
	s.name AS state,
	a.name AS action,
	p.name AS parameter,
	e.name AS element,
	m.name AS mutator,
	d.name AS dataset,
	mi.count AS iterationcount
FROM metrics_iterations AS mi
JOIN states     AS s ON s.id = mi.state
JOIN actions    AS a ON a.id = mi.action
JOIN parameters AS p ON p.id = mi.state
JOIN elements   AS e ON e.id = mi.element
JOIN mutators   AS m ON m.id = mi.mutator
JOIN datasets   AS d ON d.id = mi.dataset;

CREATE VIEW view_metrics_faults AS
SELECT
	s.name AS state,
	a.name AS action,
	p.name AS parameter,
	e.name AS element,
	m.name AS mutator,
	d.name AS dataset,
	b.name AS bucket,
	count(distinct mf.faultnumber) AS faultcount
FROM metrics_faults AS mf
JOIN states     AS s ON s.id = mf.state
JOIN actions    AS a ON a.id = mf.action
JOIN parameters AS p ON p.id = mf.state
JOIN elements   AS e ON e.id = mf.element
JOIN mutators   AS m ON m.id = mf.mutator
JOIN datasets   AS d ON d.id = mf.dataset
JOIN buckets    AS b ON b.id = mf.bucket
GROUP BY
	mf.state,
	mf.action,
	mf.parameter,
	mf.element,
	mf.mutator,
	mf.bucket
ORDER BY
	mf.state,
	mf.action,
	mf.parameter,
	mf.element,
	mf.mutator,
	mf.bucket
;

CREATE VIEW view_buckets AS
SELECT
	b.id,
	b.name AS bucket,
	m.name AS mutator,
	CASE WHEN length(p.name) > 0 THEN
		s.name || '.' || a.name || '.' || p.name || '.' || e.name
	ELSE
		s.name || '.' || a.name || '.' || e.name
	END AS element,
	sum(mi.count) AS iterationcount,
	count(distinct(mf.faultnumber)) AS faultcount
FROM metrics_faults AS mf
JOIN metrics_iterations AS mi ON 
	mi.state = mf.state AND
	mi.action = mf.action AND
	mi.parameter = mf.parameter AND
	mi.element = mf.element AND
	mi.mutator = mf.mutator AND
	mi.dataset = mf.dataset
JOIN buckets    AS b ON b.id = mf.bucket
JOIN states     AS s ON s.id = mf.state
JOIN actions    AS a ON a.id = mf.action
JOIN parameters AS p ON p.id = mf.parameter
JOIN elements   AS e ON e.id = mf.element
JOIN mutators   AS m ON m.id = mf.mutator
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
	b.name AS bucket,
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
	mutator,
	state,
	action,
	parameter,
	element
FROM metrics_iterations;

CREATE VIEW view_mutator_elementcount AS
SELECT 
	mutator,
	count(*) AS count
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
	count(distinct mf.bucket) AS bucketcount, 
	count(distinct mf.faultnumber) AS faultcount
FROM metrics_faults AS mf
WHERE mf.bucket IN (SELECT id FROM view_buckets_major)
GROUP BY mf.mutator;

CREATE VIEW view_mutators_iterations AS
SELECT 
    mi.mutator, 
	ec.count AS elementcount,
	sum(mi.count) AS iterationcount
FROM view_mutator_elementcount AS ec
JOIN metrics_iterations AS mi ON ec.mutator = mi.mutator
GROUP BY mi.mutator;

CREATE VIEW view_mutators AS
SELECT
	m.name AS mutator,
	mi.elementcount,
	mi.iterationcount,
	mf.bucketcount,
	mf.faultcount
FROM view_mutators_iterations AS mi
LEFT JOIN view_mutators_faults AS mf ON mf.mutator = mi.mutator
JOIN mutators AS m ON m.id = mi.mutator;

CREATE VIEW view_elements_iterations AS
SELECT
	state,
	action,
	parameter,
	dataset,
	element,
	sum(count) AS iterationcount
FROM metrics_iterations
GROUP BY 
	state,
	action,
	parameter,
	dataset,
	element
;

CREATE VIEW view_elements_faults AS
SELECT
	state,
	action,
	parameter,
	dataset,
	element,
	count(distinct(bucket)) as bucketcount,
	count(distinct(faultnumber)) as faultcount
FROM metrics_faults
WHERE bucket IN (SELECT id FROM view_buckets_major)
GROUP BY 
	state,
	action,
	parameter,
	dataset,
	element
;

CREATE VIEW view_elements AS
SELECT 
	s.name as state,
	a.name as action,
	p.name as parameter,
	d.name as dataset,
	e.name as element,
	ei.iterationcount,
	ef.bucketcount,
	ef.faultcount
FROM view_elements_iterations AS ei
LEFT JOIN view_elements_faults AS ef ON
	ef.element = ei.element AND 
	ef.state = ei.state AND 
	ef.action = ei.action AND 
	ef.parameter = ei.parameter AND 
	ef.dataset = ei.dataset
JOIN elements   AS e ON e.id = ei.element
JOIN states     AS s ON s.id = ei.state
JOIN actions    AS a ON a.id = ei.action
JOIN parameters AS p ON p.id = ei.parameter
JOIN datasets   AS d ON d.id = ei.dataset
ORDER BY ef.faultcount DESC;

CREATE VIEW view_datasets_iterations AS
SELECT
	[state],
	[action],
	parameter,
	dataset,
	sum(count) AS iterationcount
FROM metrics_iterations
GROUP BY 
	[state],
	[action],
	parameter,
	dataset
;

CREATE VIEW view_datasets_faults AS
SELECT
	[state],
	[action],
	parameter,
	dataset,
	count(distinct(bucket)) as bucketcount,
	count(distinct(faultnumber)) as faultcount
FROM metrics_faults
WHERE bucket IN (SELECT id FROM view_buckets_major)
GROUP BY 
	[state],
	[action],
	parameter,
	dataset
;

CREATE VIEW view_datasets AS
SELECT
	CASE WHEN length(p.name) > 0 THEN
		s.name || '.' || a.name || '.' || p.name || '/' || d.name
	ELSE
		s.name || '.' || a.name || '/' || d.name
	END AS dataset,
	di.iterationcount,
	df.bucketcount,
	df.faultcount
FROM view_datasets_iterations AS di
LEFT JOIN view_datasets_faults as df ON 
	df.[state] = di.[state] AND
	df.[action] = di.[action] AND
	df.parameter = di.parameter AND
	df.dataset = di.dataset
JOIN states     AS s ON s.id = di.[state]
JOIN actions    AS a ON a.id = di.[action]
JOIN parameters AS p ON p.id = di.parameter
JOIN datasets   AS d ON d.id = di.dataset
ORDER BY df.bucketcount DESC
LIMIT 20;
