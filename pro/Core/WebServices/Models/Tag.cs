using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	/// <summary>
	/// An arbitrary tag that is included with various models.
	/// </summary>
	public class Tag
	{
		/// <summary>
		/// The name of this tag.
		/// </summary>
		/// <example>
		/// "Category.Network"
		/// </example>
		public string Name { get; set; }

		/// <summary>
		/// The values of this tag.
		/// </summary>
		/// <example>
		/// { "Category", "Network" }
		/// </example>
		public List<string> Values { get; set; }
	}
}
