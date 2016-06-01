//using System.Diagnostics;
using Peach.Pro.Core.Runtime;
using Peach.Pro.WebApi2;

namespace Peach
{
	/// <summary>
	/// Command line interface for Peach 3.
	/// Mostly backwards compatable with Peach 2.3.
	/// </summary>
	public class PeachMain
	{
		static int Main(string[] args)
		{
			//Debugger.Launch();

			return new ConsoleProgram
			{
				CreateWeb = (license, pitLibraryPath, jobMonitor) => 
					new WebServer(license, pitLibraryPath, jobMonitor)
			}.Run(args);
		}
	}
}
