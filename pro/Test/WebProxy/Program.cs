using System.Threading;
using Peach.Pro.Test.WebProxy.TestTarget;

namespace Peach.Pro.Test.WebProxy
{
	public class Program
	{
		public static void Main(string[] args)
		{
			using (TestTargetServer.StartServer())
			{
				Thread.Sleep(2000);
			}
		}
	}
}
