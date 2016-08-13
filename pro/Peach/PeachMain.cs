//using System.Diagnostics;

using System;
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
			try
			{

				//Debugger.Launch();

				return new ConsoleProgram
				{
					CreateWeb = (license, pitLibraryPath, jobMonitor) =>
						new WebServer(license, pitLibraryPath, jobMonitor)
				}.Run(args);

			}
			catch (Exception)
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
				throw;
			}
			finally
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
			}
		}
	}
}
