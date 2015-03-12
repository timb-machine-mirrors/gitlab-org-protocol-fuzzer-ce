using System;

namespace Peach.Pro.Core.WebServices
{
	/// <summary>
	/// The context that is passed to each WebService instance.
	/// This is where state between requests is maintained.
	/// </summary>
	public class WebContext
	{
		public WebContext()
			: this(String.Empty)
		{
		}

		public WebContext(string pitLibraryPath)
		{
			PitLibraryPath = pitLibraryPath;
			NodeGuid = Guid.NewGuid().ToString().ToLower();
		}

		public string PitLibraryPath { get; private set; }

		public string NodeGuid { get; private set; }
	}
}
