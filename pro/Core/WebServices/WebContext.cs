using System;

namespace Peach.Pro.Core.WebServices
{
	/// <summary>
	/// The context that is passed to each WebService instance.
	/// This is where state between requests is maintained.
	/// </summary>
	public class WebContext
	{
		public WebContext(string pitLibraryPath, IJobMonitor jobMonitor)
		{
			PitLibraryPath = pitLibraryPath;
			NodeGuid = Guid.NewGuid().ToString().ToLower();
			JobMonitor = jobMonitor;
		}

		public string PitLibraryPath { get; private set; }

		public string NodeGuid { get; private set; }

		public IJobMonitor JobMonitor { get; private set; }
	}
}
