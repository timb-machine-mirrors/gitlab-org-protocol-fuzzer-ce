using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Peach.Pro.Core.OS;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
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
			var proc = new SysProcess
			{
				StartInfo = new ProcessStartInfo(path, args)
			};

			using (var mutex = Pal.SingleInstance("CrashTestDummy"))
			{
				mutex.Lock();

				proc.Start();

				Assert.IsFalse(proc.WaitForExit(5000));
			}

			Assert.IsTrue(proc.WaitForExit(1000));
		}
	}
}
