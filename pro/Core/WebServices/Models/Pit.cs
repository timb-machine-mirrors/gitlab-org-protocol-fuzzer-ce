using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class Pit
	{
		/// <summary>
		/// The URL of this pit
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		public string PitUrl { get; set; }

		/// <summary>
		/// The name of this pit
		/// </summary>
		/// <example>
		/// "PNG Graphics Format"
		/// </example>
		public string Name { get; set; }

		/// <summary>
		/// The description of this pit
		/// </summary>
		/// <example>
		/// 
		/// </example>
		public string Description { get; set; }

		public bool Locked { get; set; }

		public List<Tag> Tags { get; set; }

		public List<PitVersion> Versions { get; set; }

		public List<PeachVersion> Peaches { get; set; }

		public string User { get; set; }

		public DateTime Timestamp { get; set; }

		#region Details
		public List<KeyValuePair<string, string>> PeachConfig { get; set; }

		public List<Parameter> Config { get; set; }
	
		public List<Agent> Agents { get; set; }
		
		public List<string> Calls { get; set; }
		#endregion
	}
}
