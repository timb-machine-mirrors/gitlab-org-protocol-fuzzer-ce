using Peach.Core;
using System;
using System.Threading;

namespace Peach.CrashTestDummy
{
	public class Program
	{
		static int Main(string[] args)
		{
			Console.WriteLine("Opening mutex...");
			using (var mutex = SingleInstance.CreateInstance("CrashTestDummy"))
			{
				for (int i = 0; i < 20; i++)
				{
					Console.WriteLine("Waiting for mutex...");
					if (mutex.TryLock())
					{
						Console.WriteLine("Mutex acquired");
						break;
					}
					Thread.Sleep(1000);
				}
			}
			Console.WriteLine("Mutex released");
			return 0;
		}
	}
}
