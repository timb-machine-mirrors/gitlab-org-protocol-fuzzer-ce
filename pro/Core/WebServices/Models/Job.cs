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
		Fuzzing,
		Searching,
		Reproducing,
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

	[Table]
	public class Job
	{
		[Key]
		public string Id { get; set; }

		/// <summary>
		/// The URL of this job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}"
		/// </example>
		[NotMapped]
		public string JobUrl { get; set; }

		/// <summary>
		/// URLs used to control a running job.
		/// </summary>
		[NotMapped]
		public JobCommands Commands { get; set; }

		/// <summary>
		/// The URL of faults from job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/faults"
		/// </example>
		[NotMapped]
		public string FaultsUrl { get; set; }

		/// <summary>
		/// The URL of the target this job is fuzzing
		/// </summary>
		/// <example>
		/// "/p/targets/{id}"
		/// </example>
		[NotMapped]
		public string TargetUrl { get; set; }

		/// <summary>
		/// The URL of the target configuration for this job
		/// </summary>
		/// <example>
		/// "/p/targets/{target_id}/config/{config_id}"
		/// </example>
		[NotMapped]
		public string TargetConfigUrl { get; set; }

		/// <summary>
		/// The URL that returns a list of nodes used by this job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}/nodes"
		/// </example>
		[NotMapped]
		public string NodesUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of the pit for this job
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		[NotMapped]
		public string PitUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of peach for this job
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/peaches/{id}"
		/// </example>
		[NotMapped]
		public string PeachUrl { get; set; }

		/// <summary>
		/// The URL of the version of final report for this job
		/// </summary>
		/// <example>
		/// "/p/files/{id}"
		/// </example>
		[NotMapped]
		public string ReportUrl { get; set; }

		/// <summary>
		/// The URL of the version of the package containing all job inputs
		/// </summary>
		/// <example>
		/// "/p/files/{id}"
		/// </example>
		[NotMapped]
		public string PackageFileUrl { get; set; }

		/// <summary>
		/// URLs to associated metrics
		/// </summary>
		[NotMapped]
		public JobMetrics Metrics { get; set; }

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
		/// The random seed being used by the fuzzing job
		/// </summary>
		public long? Seed { get; set; }

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

		///// <summary>
		///// List of tags associated with this job
		///// </summary>
		//public ICollection<Tag> Tags { get; set; }

		///// <summary>
		///// ACL for this job
		///// </summary>
		//public ICollection<Group> Groups { get; set; }

		/// <summary>
		/// Optional starting iteration number
		/// </summary>
		public long RangeStart { get; set; }

		/// <summary>
		/// Optional ending iteration number
		/// </summary>
		public long RangeStop { get; set; }

		/// <summary>
		/// Indicates if metrics are being collected for the job
		/// </summary>
		public bool HasMetrics { get; set; }

		public long StartIteration { get; set; }
		public long CurrentIteration { get; set; }
	}
}
