using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	[Serializable]
	public class WebRoute
	{
		/// <summary>
		/// A pattern of a path portion of the URI
		/// </summary>
		/// <example>
		/// /p
		/// /p/foo/*
		/// </example>
		/// <remarks>
		/// Does a regex match by replacing '*' with '.*' and '?' with '.'.
		/// </remarks>
		public string Url { get; set; }

		/// <summary>
		/// URL of swagger definition document for this route.
		/// </summary>
		/// <example>
		/// file://myswagger.json
		/// http://example/swagger.json
		/// </example>
		/// <remarks>
		/// If null or empty, no swagger will be used.
		/// </remarks>
		public string Swagger { get; set; }

		/// <summary>
		/// Python script to run on each request.
		/// </summary>
		public string Script { get; set; }

		/// <summary>
		/// Should mutations occur on requests that match this route.
		/// </summary>
		public bool Mutate { get; set; }

		/// <summary>
		/// Rewrite the base url to the value specified.
		/// </summary>
		/// <remarks>
		/// If null or empty, no rewriting will occur.
		/// </remarks>
		/// <example>
		/// http://127.0.0.1:1234/
		/// </example>
		public string BaseUrl { get; set; }

		/// <summary>
		/// Generate a fault if response matches one of the specified codes.
		/// </summary>
		/// <example>
		/// 500,501
		/// </example>
		public List<int> FaultOnStatusCodes { get; set; }

		/// <summary>
		/// List of headers to include/exclude
		/// </summary>
		/// <remarks>
		/// THe order matters for matching precedence.
		/// </remarks>
		public List<WebHeader> Headers { get; set; }
	}
}
