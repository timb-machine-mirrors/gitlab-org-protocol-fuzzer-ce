using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	public enum JobStatus
	{
		Stopped,
		StartPending,
		StopPending,
		Running,
		ContinuePending,
		PausePending,
		Paused,
	}

	public enum JobMode
	{
		Starting,
		Recording,
		Fuzzing,
		Searching,
		Reproducing,
	}

	public class JobLinks
	{
		/// <summary>
		/// The URL of this job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}"
		/// </example>
		public string JobUrl { get; set; }

		/// <summary>
		/// URLs used to control a running job.
		/// </summary>
		public JobCommands Commands { get; set; }

		/// <summary>
		/// URLs to associated metrics
		/// </summary>
		public JobMetrics Metrics { get; set; }

		/// <summary>
		/// The URL for getting test results
		/// </summary>
		public string TestUrl { get; set; }

		/// <summary>
		/// The URL of faults from job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/faults"
		/// </example>
		public string FaultsUrl { get; set; }

		/// <summary>
		/// The URL of the target this job is fuzzing
		/// </summary>
		/// <example>
		/// "/p/targets/{id}"
		/// </example>
		public string TargetUrl { get; set; }

		/// <summary>
		/// The URL of the target configuration for this job
		/// </summary>
		/// <example>
		/// "/p/targets/{target_id}/config/{config_id}"
		/// </example>
		public string TargetConfigUrl { get; set; }

		/// <summary>
		/// The URL that returns a list of nodes used by this job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/nodes"
		/// </example>
		public string NodesUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of peach for this job
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/peaches/{id}"
		/// </example>
		public string PeachUrl { get; set; }

		/// <summary>
		/// The URL of the version of final report for this job
		/// </summary>
		/// <example>
		/// "/p/files/{id}"
		/// </example>
		public string ReportUrl { get; set; }

		/// <summary>
		/// The URL of the version of the package containing all job inputs
		/// </summary>
		/// <example>
		/// "/p/files/{id}"
		/// </example>
		public string PackageFileUrl { get; set; }
	}

	public class JobCommands
	{
		/// <summary>
		/// The URL used to stop this job.
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/stop"
		/// </example>
		public string StopUrl { get; set; }

		/// <summary>
		/// The URL used to continue this job.
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/continue"
		/// </example>
		public string ContinueUrl { get; set; }

		/// <summary>
		/// The URL used to pause this job.
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/pause"
		/// </example>
		public string PauseUrl { get; set; }

		/// <summary>
		/// The URL used to kill this job.
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/kill"
		/// </example>
		public string KillUrl { get; set; }
	}

	public class JobMetrics
	{
		public string BucketTimeline { get; set; }
		public string FaultTimeline { get; set; }
		public string Mutators { get; set; }
		public string Elements { get; set; }
		public string Dataset { get; set; }
		public string States { get; set; }
		public string Buckets { get; set; }
		public string Iterations { get; set; }
	}

	public class JobRequest
	{
		/// <summary>
		/// The URL of the specific version of the pit for this job
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		public string PitUrl { get; set; }

		/// <summary>
		/// The random seed being used by the fuzzing job
		/// </summary>
		public long? Seed { get; set; }

		/// <summary>
		/// Optional starting iteration number
		/// </summary>
		public long RangeStart { get; set; }

		/// <summary>
		/// Optional ending iteration number
		/// </summary>
		public long? RangeStop { get; set; }

		/// <summary>
		/// Determines whether the job is a test run or an actual fuzzing session.
		/// </summary>
		public bool IsTest { get; set; }
	}

	[Table]
	public class Job : JobRequest
	{
		[Key]
		public string Id { get; set; }

		[NotMapped]
		[JsonIgnore]
		public Guid Guid
		{
			get { return new Guid(Id); }
			set { Id = value.ToString(); }
		}

		[NotMapped]
		public JobLinks Links { get; set; }

		/// <summary>
		/// The status of this job record
		/// </summary>
		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public JobStatus Status { get; set; }

		/// <summary>
		/// The mode that this job is operating under
		/// </summary>
		[JsonConverter(typeof (CamelCaseStringEnumConverter))]
		public JobMode Mode { get; set; }

		/// <summary>
		/// Display name for the job
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The result of the job.
		/// Only set when the Status is Stopped.
		/// Otherwise is null and omitted from the JSON.
		/// </summary>
		/// <example>
		/// "Job ran to completion."
		/// "User initiated stop."
		/// "Some random error occured."
		/// </example>
		public string Result { get; set; }

		/// <summary>
		/// Fuzzing notes associated with the job
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// User that started the fuzzing job
		/// </summary>
		public string User { get; set; }

		/// <summary>
		/// How many iterations of fuzzing have been completed
		/// </summary>
		public long IterationCount { get; set; }

		/// <summary>
		/// The date the job was started
		/// </summary>
		public DateTime StartDate
		{
			get { return _startDate; }
			set { _startDate = value.MakeUtc(); }
		}
		private DateTime _startDate;

		/// <summary>
		/// The date the job ended
		/// </summary>
		public DateTime? StopDate
		{
			get { return _stopDate; }
			set
			{
				if (value.HasValue)
					_stopDate = value.Value.MakeUtc();
				else
					_stopDate = null;
			}
		}
		private DateTime? _stopDate;

		/// <summary>
		/// The number of seconds the job has been running for
		/// </summary>
		public long Runtime { get; set; }

		/// <summary>
		/// The average speed of the job in iterations per hour
		/// </summary>
		public long Speed { get; set; }

		/// <summary>
		/// How many faults have been detected
		/// </summary>
		public long FaultCount { get; set; }

		/// <summary>
		/// List of tags associated with this job
		/// </summary>
		[NotMapped]
		public ICollection<Tag> Tags { get; set; }

		/// <summary>
		/// ACL for this job
		/// </summary>
		[NotMapped]
		public ICollection<Group> Groups { get; set; }

		/// <summary>
		/// Indicates if metrics are being collected for the job
		/// </summary>
		[Obsolete]
		public bool HasMetrics { get; set; }
	}
}
