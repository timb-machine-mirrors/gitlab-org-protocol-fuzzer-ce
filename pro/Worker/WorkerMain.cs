//using System.Diagnostics;
using Peach.Pro.Core.Runtime;

namespace PeachWorker
{
	public class WorkerMain
	{
		static int Main(string[] args)
		{
			//Debugger.Launch();

			return new Worker().Run(args);
		}
	}
}
