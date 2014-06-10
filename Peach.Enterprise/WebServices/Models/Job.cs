using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class Job
	{
		/// <summary>
		/// The URL of this job
		/// </summary>
		/// <example>
		/// "/p/jobs/{id}"
		/// </example>
		public string JobUrl { get; set; }

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
		/// The URL of the specific version of the pit for this job
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		public string PitUrl { get; set; }

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

		/// <summary>
		/// Display name for the job
		/// </summary>
		public string Name { get; set; }

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
		public uint Seed { get; set; }

		/// <summary>
		/// How many iterations of fuzzing have been completed
		/// </summary>
		public uint IterationCount { get; set; }

		/// <summary>
		/// The date the job was started
		/// </summary>
		public DateTime StartDate { get; set; }

		/// <summary>
		/// The date the job ended
		/// </summary>
		public DateTime StopDate { get; set; }

		/// <summary>
		/// The number of seconds the job has been running for
		/// </summary>
		public uint Runtime { get; set; }

		/// <summary>
		/// The average speed of the job in iterations per hour
		/// </summary>
		public uint Speed { get; set; }

		/// <summary>
		/// How many faults have been detected
		/// </summary>
		public uint FaultCount { get; set; }

		/// <summary>
		/// List of tags associated with this job
		/// </summary>
		public List<Tag> Tags { get; set; }

		/// <summary>
		/// ACL for this job
		/// </summary>
		public List<Group> Groups { get; set; }
	}
}
