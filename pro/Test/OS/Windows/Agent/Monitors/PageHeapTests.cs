using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.OS.Windows.Agent.Monitors;
using System;
using System.Collections.Generic;
using System.IO;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Category("Peach")]
	class PageHeapTests
	{
		const string Monitor = "PageHeap";

		private bool Check(string exe)
		{
			var dbgPath = WindowsDebuggerHybrid.FindWinDbg(); ;
			var p = SubProcess.Run(Path.Combine(dbgPath, "gflags.exe"), "/p");
			var stdout = p.StdOut.ToString();
			var lines = stdout.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

			bool isFirst = true;
			foreach (var line in lines)
			{
				if (isFirst)
				{
					isFirst = false;
					continue;
				}

				if (line.Contains("{0}: page heap enabled".Fmt(exe)))
					return true;
			}

			return false;
		}

		[SetUp]
		public void SetUp()
		{
			if (Platform.GetOS() == Platform.OS.Windows &&
				Platform.GetArch() == Platform.Architecture.x86)
				Assert.Ignore("Test is not supported on this platform (yet)");
		}

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>());
			var ex = Assert.Catch(() => runner.Run());
			Assert.That(ex, Is.InstanceOf<PeachException>());
			var msg = "Could not start monitor \"PageHeap\".  Monitor 'PageHeap' is missing required parameter 'Executable'.";
			StringAssert.StartsWith(msg, ex.Message);
		}

		[Test]
		public void TestBasic()
		{
			var exe = "foobar";
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>
			{
				{"Executable", exe},
			})
			{
				SessionStarting = m =>
				{
					m.SessionStarting();
					Assert.IsTrue(Check(exe), "PageHeap should be enabled");
				},
				SessionFinished = m =>
				{
					m.SessionFinished();
					Assert.IsFalse(Check(exe), "PageHeap should be disabled");
				},
			};
			runner.Run();
		}
	}
}
