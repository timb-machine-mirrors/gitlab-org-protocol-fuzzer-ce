using Newtonsoft.Json;
using System;

namespace Peach.Enterprise.WebServices.Models
{
	public enum TestStatus
	{
		Active,
		Pass,
		Fail
	}

	public class TestEvent
	{
		/// <summary>
		/// Identifier of event
		/// </summary>
		public uint Id { get; set; }

		/// <summary>
		/// Status of event
		/// </summary>
		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public TestStatus Status { get; set; }

		/// <summary>
		/// Short description of event
		/// </summary>
		public string Short { get; set; }

		/// <summary>
		/// Long description of event
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// How to resolve the event if it is an issue
		/// </summary>
		public string Resolve { get; set; }
	}
}
