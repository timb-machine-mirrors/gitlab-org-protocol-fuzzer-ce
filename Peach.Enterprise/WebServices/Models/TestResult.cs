using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class TestResult
	{
		/// <summary>
		/// The overall status of the test result
		/// </summary>
		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public TestStatus Status { get; set; }

		/// <summary>
		/// The events that mae up the test reslt
		/// </summary>
		public List<TestEvent> Events { get; set; }

		public string Log { get; set; }
	}
}
