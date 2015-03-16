using NUnit.Framework;
using Peach.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	class SingleInstanceTests
	{
		[Test]
		public void TestBasic()
		{
			var argsList = new List<string>();
			var path = Path.Combine(Utilities.ExecutionDirectory, "CrashTestDummy.exe");
			if (Platform.GetOS() != Platform.OS.Windows)
			{
				argsList.Add(path);
				path = "mono";
			}
			var args = string.Join(" ", argsList);
			var proc = new Process();
			proc.StartInfo = new ProcessStartInfo(path, args);

			using (var mutex = SingleInstance.CreateInstance("CrashTestDummy"))
			{
				mutex.Lock();

				proc.Start();

				Assert.IsFalse(proc.WaitForExit(5000));
			}

			Assert.IsTrue(proc.WaitForExit(1000));
		}
	}
}
