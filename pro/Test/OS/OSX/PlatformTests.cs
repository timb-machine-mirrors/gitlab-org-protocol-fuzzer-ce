using System;
using System.Diagnostics;
using NUnit.Framework;
using Peach.Core;
using System.Linq;

namespace Peach.Pro.Test.OS.OSX
{
	[TestFixture]
	[Category("Peach")]
	public class PlatformTests
	{
		[Test]
		public void TestCpuUsage()
		{
			using (var p = Process.GetProcessById(1))
			{
				var pi = ProcessInfo.Instance.Snapshot(p);
				Assert.NotNull(pi);
				Assert.AreEqual(1, pi.Id);
				Assert.AreEqual("launchd", pi.ProcessName);
				Assert.Greater(pi.PrivilegedProcessorTicks, 0);
				Assert.Greater(pi.UserProcessorTicks, 0);
			}

			using (var p = new Process())
			{
				var si = new ProcessStartInfo { FileName = "/bin/ls" };
				p.StartInfo = si;
				p.Start();
				p.WaitForExit();
				Assert.True(p.HasExited);
				p.Close();

				Assert.Throws<ArgumentException>(() => ProcessInfo.Instance.Snapshot(p));
			}
		}

		[Test]
		public void GetProcByName()
		{
			string procName;
			int procId;

			using (var self = Process.GetCurrentProcess())
			{
				// Use process snapshot so we are sure to get the correct name on osx
				procName = ProcessInfo.Instance.Snapshot(self).ProcessName;
				procId = self.Id;
			}

			var p = ProcessInfo.Instance.GetProcessesByName(procName);

			try
			{
				Assert.NotNull(p);
				Assert.Greater(p.Length, 0);

				var match = p.Where(i => i.Id == procId);

				Assert.AreEqual(1, match.Count());
			}
			finally
			{
				foreach (var i in p)
					i.Dispose();
			}
		}
	}
}
