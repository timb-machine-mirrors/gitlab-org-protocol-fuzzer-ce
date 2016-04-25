using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;

namespace PeachWorker
{
	public class Program
	{
		static int Main(string[] args)
		{
			if (!License.Instance.IsValid)
				return -1;

			return new Worker().Run(args);
		}
	}
}
