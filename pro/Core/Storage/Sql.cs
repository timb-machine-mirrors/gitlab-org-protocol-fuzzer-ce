﻿namespace Peach.Pro.Core.Storage
{
	class Sql
	{
		public const string GetLastRowId = "SELECT last_insert_rowid();";

		public const string InsertJobLog = @"
INSERT INTO [JobLog] (
	JobId,
	Message
) VALUES (
	@JobId,
	@Message
);";

		public const string SelectTestEvents = @"
SELECT * 
FROM [TestEvent] 
WHERE JobId = @JobId;
";

		public const string SelectJobLogs = @"
SELECT * 
FROM [JobLog] 
WHERE JobId = @Id;
";

		public const string SelectJob = @"
SELECT * 
FROM [Job] 
WHERE Id = @Id;
";

		public const string InsertJob = @"
INSERT INTO [Job] (
	Id,
	Status,
	Mode,
	PitFile,
	Result,
	Notes,
	User,
	Seed,
	IterationCount,
	StartDate,
	StopDate,
	HeartBeat,
	Pid,
	Runtime,
	FaultCount,
	RangeStart,
	RangeStop,
	IsControlIteration,
	PitUrl,
	LogPath,
	PeachVersion
) VALUES (
	@Id,
	@Status,
	@Mode,
	@PitFile,
	@Result,
	@Notes,
	@User,
	@Seed,
	@IterationCount,
	@StartDate,
	@StopDate,
	@HeartBeat,
	@Pid,
	@Runtime,
	@FaultCount,
	@RangeStart,
	@RangeStop,
	@IsControlIteration,
	@PitUrl,
	@LogPath,
	@PeachVersion
);";

		public const string UpdateRunningJob = @"
UPDATE [Job]
SET 
	IterationCount = @IterationCount,
	FaultCount = @FaultCount,
	Status = @Status,
	Mode = @Mode,
	Runtime = @Runtime,
	HeartBeat = @HeartBeat
WHERE
	Id = @Id
";

		public const string UpdateJob = @"
UPDATE [Job]
SET 
	Status = @Status,
	Mode = @Mode,
	PitFile = @PitFile,
	Result = @Result,
	Notes = @Notes,
	User = @User,
	Seed = @Seed,
	IterationCount = @IterationCount,
	StartDate = @StartDate,
	StopDate = @StopDate,
	HeartBeat = @HeartBeat,
	Pid = @Pid,
	Runtime = @Runtime,
	FaultCount = @FaultCount,
	RangeStart = @RangeStart,
	RangeStop = @RangeStop,
	LogPath = @LogPath,
	PeachVersion = @PeachVersion
WHERE
	Id = @Id
;";

		public const string DeleteJob = @"
DELETE FROM [TestEvent]
WHERE JobId = @Id;

DELETE FROM [JobLog]
WHERE JobId = @Id;

DELETE FROM [Job]
WHERE Id = @Id;
";

		public const string InsertTestEvent = @"
INSERT INTO TestEvent (
	JobId,
	Status, 
	Short, 
	Description,
	Resolve
) VALUES (
	@JobId,
	@Status, 
	@Short, 
	@Description,
	@Resolve
);" + GetLastRowId;

		public const string	UpdateTestEvent = @"
UPDATE TestEvent
SET
	Status = @Status,
	Short = @Short,
	Description = @Description,
	Resolve = @Resolve
WHERE
	Id = @Id
;";
		
		public const string UpsertMutation = @"
INSERT OR REPLACE INTO Mutation (
	StateId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId,
	IterationCount
) VALUES (
	@StateId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId,
	COALESCE((
		SELECT IterationCount + 1
		FROM Mutation
		WHERE
			StateId = @StateId AND
			ActionId = @ActionId AND
			ParameterId = @ParameterId AND
			ElementId = @ElementId AND
			MutatorId = @MutatorId AND
			DatasetId = @DatasetId
	), 1)
);";

		public const string UpsertState = @"
INSERT OR REPLACE INTO [State] (
	Id, 
	NameId, 
	RunCount,
	Count
) VALUES (
	@Id, 
	@NameId, 
	@RunCount,
	@Count
);";

		public const string InsertFaultMetric = @"
INSERT INTO FaultMetric (
	Iteration,
	MajorHash,
	MinorHash,
	Timestamp,
	Hour,
	StateId,
	ActionId,
	ParameterId,
	ElementId,
	MutatorId,
	DatasetId
) VALUES (
	@Iteration,
	@MajorHash,
	@MinorHash,
	@Timestamp,
	@Hour,
	@StateId,
	@ActionId,
	@ParameterId,
	@ElementId,
	@MutatorId,
	@DatasetId
);";

		public const string InsertFaultDetail = @"
INSERT INTO FaultDetail (
	Reproducible,
	Iteration,
	TimeStamp,
	Source,
	Exploitability,
	MajorHash,
	MinorHash,
	Title,
	Description,
	Seed,
	IterationStart,
	IterationStop,
	Flags,
	FaultPath
) VALUES (
	@Reproducible,
	@Iteration,
	@TimeStamp,
	@Source,
	@Exploitability,
	@MajorHash,
	@MinorHash,
	@Title,
	@Description,
	@Seed,
	@IterationStart,
	@IterationStop,
	@Flags,
	@FaultPath
);" + GetLastRowId;

		public const string InsertFaultFile = @"
INSERT INTO FaultFile (
	FaultDetailId,
	Name,
	FullName,
	Size
) VALUES (
	@FaultDetailId,
	@Name,
	@FullName,
	@Size
);" + GetLastRowId;

		public const string SelectFaultDetailById = @"
SELECT * 
FROM FaultDetail 
WHERE Id = @Id;
";

		public const string SelectFaultFilesByFaultId = @"
SELECT * 
FROM FaultFile 
WHERE FaultDetailId = @Id;
";

		public const string SelectFaultFilesById = @"
SELECT * 
FROM FaultFile 
WHERE Id = @Id;
";

		public const string SelectMutationByIteration = @"
SELECT * 
FROM ViewFaults 
WHERE Iteration = @Iteration;
";

		public const string InsertNames = @"
INSERT INTO NamedItem (
	Id, 
	Name
) VALUES (
	@Id, 
	@Name
);
";

		public const string UpdateStates = @"
UPDATE State 
SET Count = @Count 
WHERE Id = @Id;
";

		public const string JobMigrateV1 = @"
ALTER TABLE FaultDetail 
ADD COLUMN 
	Flags INTEGER NOT NULL DEFAULT 0
;
";

		public const string JobMigrateV2 = @"
DROP TABLE Job;
";
	}
}
