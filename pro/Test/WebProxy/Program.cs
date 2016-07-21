using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
