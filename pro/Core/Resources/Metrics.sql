-- States >>>
CREATE VIEW ViewStates AS 
SELECT
	s.Name AS [State],
	COUNT(*) AS [ExecutionCount]
FROM [StateInstance] AS si
JOIN [State] AS s ON s.Id = si.StateId
GROUP BY si.StateId;
-- States <<<

-- Iterations >>>
CREATE VIEW ViewIterations AS 
SELECT
	s.Name AS [State],
	a.Name AS [Action],
	p.Name AS Parameter,
	e.Name AS Element,
	m.Name AS Mutator,
	d.Name AS Dataset,
	COUNT(DISTINCT(x.Iteration)) AS IterationCount
FROM Mutation  AS x
JOIN [State]   AS s ON s.Id = x.StateId
JOIN [Action]  AS a ON a.Id = x.ActionId
JOIN Parameter AS p ON p.Id = x.ParameterId
JOIN Element   AS e ON e.Id = x.ElementId
JOIN Mutator   AS m ON m.Id = x.MutatorId
JOIN Dataset   AS d ON d.Id = x.DatasetId
GROUP BY
	x.StateId,
	x.Actionid,
	x.ParameterId,
	x.ElementId,
	x.MutatorId,
	x.DatasetId
;
-- Iterations <<<

-- Buckets >>>
CREATE VIEW ViewMutationsByIteration AS
SELECT
	COUNT(DISTINCT(m.Iteration)) AS IterationCount,
	m.StateId,
	m.ActionId,
	m.ParameterId,
	m.ElementId,
	m.MutatorId,
	m.DatasetId
FROM Mutation AS m
GROUP BY
	m.StateId,
	m.ActionId,
	m.ParameterId,
	m.ElementId,
	m.MutatorId,
	m.DatasetId
;

CREATE VIEW ViewMutationsByFault AS
SELECT
	COUNT(*) AS FaultCount,
	z.StateId,
	z.ActionId,
	z.ParameterId,
	z.ElementId,
	z.MutatorId,
	z.DatasetId
FROM FaultMetric AS x
JOIN FaultMetricMutation AS y ON y.FaultMetricId = x.Id
JOIN Mutation AS z ON y.MutationId = z.Id
GROUP BY
	z.StateId,
	z.ActionId,
	z.ParameterId,
	z.ElementId,
	z.MutatorId,
	z.DatasetId
;

CREATE VIEW ViewBuckets AS
SELECT
	printf('%s_%s', x.MajorHash, x.MinorHash) AS Bucket,
	m.Name AS Mutator,
	CASE WHEN length(p.Name) > 0 THEN
		printf('%s.%s.%s.%s', s.Name, a.Name, p.Name, e.Name)
	ELSE
		printf('%s.%s.%s', s.Name, a.Name, e.Name)
	END AS Element,
	vmi.IterationCount AS IterationCount,
	vmf.FaultCount AS FaultCount
FROM FaultMetric AS x
JOIN FaultMetricMutation AS y ON y.FaultMetricId = x.Id
JOIN Mutation AS z ON y.MutationId = z.Id
JOIN ViewMutationsByIteration AS vmi ON
	z.StateId     = vmi.StateId AND
	z.ActionId    = vmi.ActionId AND
	z.ParameterId = vmi.ParameterId AND
	z.ElementId   = vmi.ElementId AND
	z.MutatorId   = vmi.MutatorId AND
	z.DatasetId   = vmi.DatasetId
JOIN ViewMutationsByFault AS vmf ON
	z.StateId     = vmf.StateId AND
	z.ActionId    = vmf.ActionId AND
	z.ParameterId = vmf.ParameterId AND
	z.ElementId   = vmf.ElementId AND
	z.MutatorId   = vmf.MutatorId AND
	z.DatasetId   = vmf.DatasetId
JOIN [State]     AS s ON s.Id = z.[StateId]
JOIN [Action]    AS a ON a.Id = z.[ActionId]
JOIN [Parameter] AS p ON p.Id = z.[ParameterId]
JOIN [Element]   AS e ON e.Id = z.[ElementId]
JOIN [Mutator]   AS m ON m.Id = z.[MutatorId]
JOIN [Dataset]   AS d ON d.Id = z.[MutatorId]
GROUP BY 
	Bucket,
	Mutator,
	Element
;
-- Buckets <<<
	
-- BucketTimeline >>>
CREATE VIEW ViewBucketTimeline AS
SELECT
	printf('%s_%s', x.MajorHash, x.MinorHash) AS Label,
	MIN(x.[Iteration]) AS [Iteration],
	MIN(x.[Timestamp]) AS [Time],
	COUNT(DISTINCT(x.MinorHash)) AS FaultCount
FROM FaultMetric AS x
GROUP BY
	x.MajorHash,
	x.MinorHash
;
-- BucketTimeline <<<

-- Mutators >>>
CREATE VIEW ViewMutatorsByIteration AS
SELECT 
	x.MutatorId,
	COUNT(DISTINCT(x.MutatorId)) AS ElementCount,
	COUNT(DISTINCT(x.Iteration)) AS IterationCount
FROM Mutation AS x
GROUP BY x.MutatorId;

CREATE VIEW ViewMutatorsByFault AS
SELECT 
	z.MutatorId,
	COUNT(DISTINCT(x.MajorHash)) AS BucketCount,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount
FROM FaultMetric AS x
JOIN FaultMetricMutation AS y ON y.FaultMetricId = x.Id
JOIN Mutation AS z ON y.MutationId = z.Id
GROUP BY z.MutatorId;
	
CREATE VIEW ViewMutators AS
SELECT
	m.Name AS Mutator,
	vmi.ElementCount,
	vmi.IterationCount,
	vmf.BucketCount,
	vmf.FaultCount
FROM ViewMutatorsByIteration AS vmi
LEFT JOIN ViewMutatorsByFault AS vmf ON vmi.MutatorId = vmf.MutatorId
JOIN Mutator AS m ON vmi.MutatorId = m.Id;
-- Mutators <<<

-- Elements >>>
CREATE VIEW ViewElementsByIteration AS
SELECT
	x.StateId,
	x.Actionid,
	x.ParameterId,
	x.DatasetId,
	x.ElementId,
	COUNT(DISTINCT(x.Iteration)) AS IterationCount
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
	z.StateId,
	z.ActionId,
	z.ParameterId,
	z.DatasetId,
	z.ElementId,
	COUNT(DISTINCT(x.Iteration)) AS FaultCount,
	COUNT(DISTINCT(x.MajorHash)) AS BucketCount
FROM FaultMetric AS x
JOIN FaultMetricMutation AS y ON y.FaultMetricId = x.Id
JOIN Mutation AS z ON y.MutationId = z.Id
GROUP BY
	z.StateId,
	z.ActionId,
	z.ParameterId,
	z.DatasetId,
	z.ElementId
;

CREATE VIEW ViewElements AS
SELECT 
	s.Name as [State],
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
JOIN [Element]   AS e ON e.Id = vei.ElementId
JOIN [State]     AS s ON s.Id = vei.StateId
JOIN [Action]    AS a ON a.Id = vei.ActionId
JOIN [Parameter] AS p ON p.Id = vei.ParameterId
JOIN [Dataset]   AS d ON d.Id = vei.DatasetId;
-- Elements <<<

-- Datasets >>>
CREATE VIEW ViewDatasetsByIteration AS
SELECT
	x.DatasetId,
	COUNT(DISTINCT(x.Iteration)) AS IterationCount
FROM Mutation AS x
GROUP BY x.DatasetId;

CREATE VIEW ViewDatasetsByFault AS
SELECT
	z.DatasetId,
	COUNT(DISTINCT(x.MajorHash)) as BucketCount,
	COUNT(DISTINCT(x.Iteration)) as FaultCount
FROM FaultMetric AS x
JOIN FaultMetricMutation AS y ON y.FaultMetricId = x.Id
JOIN Mutation AS z ON y.MutationId = z.Id
GROUP BY z.DatasetId;

CREATE VIEW ViewDatasets AS
SELECT
	d.Name as Dataset,
	vdi.IterationCount,
	vdf.BucketCount,
	vdf.FaultCount
FROM ViewDatasetsByIteration AS vdi
LEFT JOIN ViewDatasetsByFault as vdf ON 
	vdi.DatasetId = vdf.DatasetId
JOIN Dataset AS d ON 
	vdi.DatasetId = d.Id
;
-- Datasets <<<
