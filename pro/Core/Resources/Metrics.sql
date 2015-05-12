-- States >>>
CREATE VIEW ViewStates AS 
SELECT
	n.Name || '_' || s.RunCount AS [State],
	s.[Count] AS [ExecutionCount]
FROM [State] AS s
JOIN NamedItem n ON s.NameId = n.Id;
-- States <<<

-- Iterations >>>
CREATE VIEW ViewIterations AS 
SELECT
	sn.Name || '_' || s.RunCount AS [State],
	a.Name AS [Action],
	p.Name AS Parameter,
	e.Name AS Element,
	m.Name AS Mutator,
	d.Name AS Dataset,
	x.IterationCount
FROM Mutation  AS x
JOIN [State]   AS s  ON s.Id  = x.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS a  ON a.Id  = x.ActionId
JOIN NamedItem AS p  ON p.Id  = x.ParameterId
JOIN NamedItem AS e  ON e.Id  = x.ElementId
JOIN NamedItem AS m  ON m.Id  = x.MutatorId
JOIN NamedItem AS d  ON d.Id  = x.DatasetId;
-- Iterations <<<

-- Buckets >>>
CREATE VIEW ViewBuckets AS
SELECT
	x.MajorHash || '_' || x.MinorHash AS Bucket,
	m.Name AS Mutator,
	CASE WHEN LENGTH(p.Name) > 0 THEN
		sn.Name || '_' || s.RunCount || '.' || 
		a.Name || '.' || 
		p.Name || '.' || 
		e.Name
	ELSE
		sn.Name || '_' || s.RunCount || '.' || 
		a.Name || '.' || 
		e.Name
	END AS Element,
	y.IterationCount,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
JOIN Mutation AS y ON 
	x.StateId     = y.StateId AND
	x.ActionId    = y.ActionId AND
	x.ParameterId = y.ParameterId AND
	x.ElementId   = y.ElementId AND
	x.MutatorId   = y.MutatorId AND
	x.DatasetId   = y.DatasetId
JOIN [State]   AS s  ON s.Id  = x.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS a  ON a.Id  = x.ActionId
JOIN NamedItem AS p  ON p.Id  = x.ParameterId
JOIN NamedItem AS e  ON e.Id  = x.ElementId
JOIN NamedItem AS m  ON m.Id  = x.MutatorId
JOIN NamedItem AS d  ON d.Id  = x.MutatorId
GROUP BY 
	x.MajorHash,
	x.MinorHash,
	x.MutatorId,
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.ElementId,
	x.DatasetId
;
-- Buckets <<<
	
-- BucketTimeline >>>
CREATE VIEW ViewBucketTimeline AS
SELECT
	x.MajorHash || '_' || x.MinorHash AS Label,
	MIN(x.[Iteration]) AS [Iteration],
	MIN(x.[Timestamp]) AS [Time],
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
GROUP BY
	x.MajorHash,
	x.MinorHash
;
-- BucketTimeline <<<

-- FaultTimeline >>>
CREATE VIEW ViewFaultTimeline AS
SELECT
	x.[Timestamp] AS [Date],
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric x
GROUP BY x.[Hour];
-- FaultTimeline <<<

-- Mutators >>>
CREATE VIEW ViewDistinctElements AS
SELECT DISTINCT
	MutatorId,
	StateId,
	ActionId,
	ParameterId,
	ElementId
FROM Mutation;

CREATE VIEW ViewMutatorsByElement AS
SELECT 
	MutatorId,
	COUNT(*) AS ElementCount
FROM ViewDistinctElements
GROUP BY MutatorId;

CREATE VIEW ViewMutatorsByIteration AS
SELECT 
	vme.MutatorId,
	vme.ElementCount,
	SUM(x.IterationCount) AS IterationCount
FROM ViewMutatorsByElement AS vme
JOIN Mutation AS x ON vme.MutatorId = x.MutatorId
GROUP BY x.MutatorId;

CREATE VIEW ViewMutatorsByFault AS
SELECT 
	x.MutatorId,
	COUNT(DISTINCT(x.MajorHash)) AS BucketCount,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
GROUP BY x.MutatorId;
	
CREATE VIEW ViewMutators AS
SELECT
	n.Name AS Mutator,
	vmi.ElementCount,
	vmi.IterationCount,
	vmf.BucketCount,
	vmf.FaultCount
FROM ViewMutatorsByIteration AS vmi
LEFT JOIN ViewMutatorsByFault AS vmf ON vmi.MutatorId = vmf.MutatorId
JOIN NamedItem AS n ON vmi.MutatorId = n.Id;
-- Mutators <<<

-- Elements >>>
CREATE VIEW ViewElementsByIteration AS
SELECT
	x.StateId,
	x.Actionid,
	x.ParameterId,
	x.DatasetId,
	x.ElementId,
	SUM(x.IterationCount) AS IterationCount
FROM Mutation AS x
GROUP BY 
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.ElementId
;

CREATE VIEW ViewElementsByFault AS
SELECT
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.ElementId,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount,
	COUNT(DISTINCT(x.MajorHash)) AS BucketCount
FROM FaultMetric AS x
GROUP BY
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	x.ElementId
;

CREATE VIEW ViewElements AS
SELECT 
	sn.Name || '_' || s.RunCount AS [State],
	a.Name as [Action],
	p.Name as [Parameter],
	d.Name as [Dataset],
	e.Name as [Element],
	vei.IterationCount,
	vef.BucketCount,
	vef.FaultCount
FROM ViewElementsByIteration AS vei
LEFT JOIN ViewElementsByFault AS vef ON
	vei.ElementId   = vef.ElementId AND 
	vei.StateId     = vef.StateId AND 
	vei.ActionId    = vef.ActionId AND 
	vei.ParameterId = vef.ParameterId AND 
	vei.DatasetId   = vef.DatasetId
JOIN [State]   AS s  ON s.Id  = vei.StateId
JOIN NamedItem AS sn ON sn.Id = s.NameId
JOIN NamedItem AS e  ON e.Id  = vei.ElementId
JOIN NamedItem AS a  ON a.Id  = vei.ActionId
JOIN NamedItem AS p  ON p.Id  = vei.ParameterId
JOIN NamedItem AS d  ON d.Id  = vei.DatasetId;
-- Elements <<<

-- Datasets >>>
CREATE VIEW ViewDatasetsByIteration AS
SELECT
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	SUM(x.IterationCount) AS IterationCount
FROM Mutation AS x
GROUP BY 
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId
;

CREATE VIEW ViewDatasetsByFault AS
SELECT
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId,
	COUNT(DISTINCT(x.MajorHash)) as BucketCount,
	COUNT(DISTINCT(x.Iteration)) as FaultCount
FROM FaultMetric AS x
GROUP BY 
	x.StateId,
	x.ActionId,
	x.ParameterId,
	x.DatasetId
;

CREATE VIEW ViewDatasets AS
SELECT
	CASE WHEN length(p.name) > 0 THEN
		sn.name || '.' || a.name || '.' || p.name || '/' || d.name
	ELSE
		sn.name || '.' || a.name || '/' || d.name
	END AS Dataset,
	vdi.IterationCount,
	vdf.BucketCount,
	vdf.FaultCount
FROM ViewDatasetsByIteration AS vdi
LEFT JOIN ViewDatasetsByFault as vdf ON 
	vdi.StateId = vdf.StateId AND
	vdi.ActionId = vdf.ActionId AND
	vdi.ParameterId = vdf.ParameterId AND
	vdi.DatasetId = vdf.DatasetId
JOIN [State] AS s ON vdi.StateId = s.Id
JOIN NamedItem AS sn ON s.NameId = sn.Id
JOIN NamedItem AS a ON vdi.ActionId = a.Id
JOIN NamedItem AS p ON vdi.ParameterId = p.Id
JOIN NamedItem AS d ON vdi.DatasetId = d.Id;
