using Peach.Pro.Core.Runtime;
using Peach.Pro.WebApi2;

namespace PeachService
{
	public class ServiceMain
	{
		static int Main(string[] args)
		{
			return new Service
			{
				CreateWeb = (license, pitLibraryPath, jobMonitor) => 
					new WebServer(license, pitLibraryPath, jobMonitor)
			}.Run(args);
		}
	}
}
