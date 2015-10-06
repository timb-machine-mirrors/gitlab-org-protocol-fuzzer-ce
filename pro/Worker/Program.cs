using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;

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
