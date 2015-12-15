using System.Collections.Generic;
using Newtonsoft.Json;

namespace Peach.Pro.Core.WebServices.Models
{
	public class TestResult
	{
		/// <summary>
		/// The overall status of the test result
		/// </summary>
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
		public TestStatus Status { get; set; }

		/// <summary>
		/// The events that mae up the test reslt
		/// </summary>
		public IEnumerable<TestEvent> Events { get; set; }

		public string Log { get; set; }

		public string LogUrl { get; set; }
	}
}
