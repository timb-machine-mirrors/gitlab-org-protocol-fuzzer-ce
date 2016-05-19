using Peach.Pro.Core.Runtime;

namespace PeachWorker
{
	public class WorkerMain
	{
		static int Main(string[] args)
		{
			return new Worker().Run(args);
		}
	}
}
