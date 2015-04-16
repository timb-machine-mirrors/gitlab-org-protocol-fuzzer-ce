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
	Name,
	Result,
	Notes,
	User,
	Seed,
	IterationCount,
	StartDate,
	StopDate,
	Runtime,
	Speed,
	FaultCount,
	RangeStart,
	RangeStop,
	IsControlIteration,
	PitUrl,
	LogPath
) VALUES (
	@Id,
	@Status,
	@Mode,
	@Name,
	@Result,
	@Notes,
	@User,
	@Seed,
	@IterationCount,
	@StartDate,
	@StopDate,
	@Runtime,
	@Speed,
	@FaultCount,
	@RangeStart,
	@RangeStop,
	@IsControlIteration,
	@PitUrl,
	@LogPath
);";

		public const string UpdateJob = @"
UPDATE [Job]
SET 
	Status = @Status,
	Mode = @Mode,
	Name = @Name,
	Result = @Result,
	Notes = @Notes,
	User = @User,
	Seed = @Seed,
	IterationCount = @IterationCount,
	StartDate = @StartDate,
	StopDate = @StopDate,
	Runtime = @Runtime,
	Speed = @Speed,
	FaultCount = @FaultCount,
	RangeStart = @RangeStart,
	RangeStop = @RangeStop,
	LogPath = @LogPath
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
	Reproducable,
	Iteration,
	TimeStamp,
	BucketName,
	Source,
	Exploitability,
	MajorHash,
	MinorHash,
	Title,
	Description,
	Seed,
	IterationStart,
	IterationStop,
	FaultPath
) VALUES (
	@Reproducable,
	@Iteration,
	@TimeStamp,
	@BucketName,
	@Source,
	@Exploitability,
	@MajorHash,
	@MinorHash,
	@Title,
	@Description,
	@Seed,
	@IterationStart,
	@IterationStop,
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
SELECT * FROM FaultDetail WHERE Id = @Id;
";

		public const string SelectFaultFilesById = @"
SELECT * FROM FaultFile WHERE FaultDetailId = @Id;
";
	}
}
