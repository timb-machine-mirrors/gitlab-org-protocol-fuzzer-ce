using Peach.Core;
using Peach.Pro.Core.Runtime.Enterprise;

namespace PeachWorker
{
	public class Program
	{
		static int Main(string[] args)
		{
			if (!License.IsValid)
				return -1;

			return new Worker().Run(args);
		}
	}
}
