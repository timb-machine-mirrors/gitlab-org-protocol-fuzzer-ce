using System.Collections.Generic;

namespace PeachDownloader
{
	public class AppSession : TypedSession<AppSession>
	{
		public bool IsAuthenticated { get; set; }
		public bool IsEulaAccepted { get; set; }
		public Operations Operations { get; set; }
		public SortedDownloads Downloads { get; set; }
		public List<Activation> Activations { get; set; }
	}
}
