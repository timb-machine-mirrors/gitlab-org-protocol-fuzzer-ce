using System;
using System.Diagnostics;
using NUnit.Framework;
using Peach.Core;

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
			var p = ProcessInfo.Instance.GetProcessesByName("sshd");

			Assert.NotNull(p);

			foreach (var i in p)
				i.Dispose();

			Assert.AreEqual(1, p.Length);
		}
	}
}
