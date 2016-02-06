using System;
using Peach.Pro.Core.WebServices;

namespace Peach.Pro.WebApi2
{
	/// <summary>
	/// The context that is passed to each ApiController instance.
	/// This is where state between requests is maintained.
	/// </summary>
	public class WebContext : IDisposable
	{
		public WebContext(string pitLibraryPath, IJobMonitor jobMonitor)
		{
			PitLibraryPath = pitLibraryPath;
			NodeGuid = Guid.NewGuid().ToString().ToLower();
			JobMonitor = jobMonitor;
		}

		public void Dispose()
		{
			if (JobMonitor == null)
				return;

			JobMonitor.Dispose();
			JobMonitor = null;
		}

		public string PitLibraryPath { get; private set; }

		public string NodeGuid { get; private set; }

		public IJobMonitor JobMonitor { get; private set; }
	}
}
