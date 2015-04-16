using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class ProcessKillerMonitorTests
	{
		[Test]
		public void TestBadProcss()
		{
			// Nothing happens if process doesn't exist

			var runner = new MonitorRunner("ProcessKiller", new Dictionary<string, string>
			{
				{"ProcessNames", "some_invalid_process"},
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestSingleProcess()
		{
			const string args = "127.0.0.1 0";
			var exe = GetTempExeName();

			// On windows, the process name does not include the extension!
			var procName = Path.GetFileNameWithoutExtension(exe);

			Process p = null;

			try
			{
				p = RunProcess(exe, args);

				var runner = new MonitorRunner("ProcessKiller", new Dictionary<string, string>
					{
						{ "ProcessNames", procName },
					})
				{
					IterationFinished = m =>
					{
						Assert.True(ProcessExists(procName), "Process '{0}' should exist before IterationFinished".Fmt(procName));

						m.IterationFinished();

						Assert.False(ProcessExists(procName), "Process '{0}' should not exist after IterationFinished".Fmt(procName));
					},
				};

				var faults = runner.Run();

				// never faults!
				Assert.AreEqual(0, faults.Length);
			}
			finally
			{
				KillProcess(p);

				try
				{
					File.Delete(exe);
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}
			}

			Assert.False(ProcessExists(procName), "Process '{0}' should not exist after test".Fmt(procName));
		}

		[Test]
		public void TestMultiProcess()
		{
			const string args = "127.0.0.1 0";
			var exe1 = GetTempExeName();
			var exe2 = GetTempExeName();

			// On windows, the process name does not include the extension!
			var procName1 = Path.GetFileNameWithoutExtension(exe1);
			var procName2 = Path.GetFileNameWithoutExtension(exe2);

			Process p1 = null;
			Process p2 = null;

			try
			{
				p1 = RunProcess(exe1, args);
				p2 = RunProcess(exe2, args);

				var runner = new MonitorRunner("ProcessKiller", new Dictionary<string, string>
					{
						{ "ProcessNames", "{0},{1},some_invalid_process".Fmt(procName1, procName2) },
					})
				{
					IterationFinished = m =>
					{
						Assert.True(ProcessExists(procName1), "Process 1 '{0}' should exist before IterationFinished".Fmt(procName1));
						Assert.True(ProcessExists(procName2), "Process 2 '{0}' should exist before IterationFinished".Fmt(procName2));

						m.IterationFinished();

						Assert.False(ProcessExists(procName1), "Process '{0}' should not exist after IterationFinished".Fmt(procName1));
						Assert.False(ProcessExists(procName2), "Process '{0}' should not exist after IterationFinished".Fmt(procName2));
					},
				};

				var faults = runner.Run();

				// never faults!
				Assert.AreEqual(0, faults.Length);
			}
			finally
			{
				KillProcess(p1);
				KillProcess(p2);

				try
				{
					File.Delete(exe1);
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}

				try
				{
					File.Delete(exe2);
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}
			}

			Assert.False(ProcessExists(procName1), "Process '{0}' should not exist after test".Fmt(procName1));
			Assert.False(ProcessExists(procName2), "Process '{0}' should not exist after test".Fmt(procName2));
		}

		static string GetTempExeName()
		{
			var exe = Platform.GetOS() == Platform.OS.Windows ? "CrashableServer.exe" : "CrashableServer";
			var tmp = Path.GetTempFileName();
			var dir = Path.GetDirectoryName(tmp);

			if (string.IsNullOrEmpty(dir))
				Assert.Fail("Temp directory should not be null or empty");

			var name = Path.GetFileNameWithoutExtension(tmp);

			var tmpExe = Path.Combine(dir, name + "-" + exe);

			File.Delete(tmp);
			File.Copy(Utilities.GetAppResourcePath(exe), tmpExe);

			return tmpExe;
		}

		static Process RunProcess(string exe, string args)
		{
			var p = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = exe,
					Arguments = args,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};

			p.Start();

			return p;
		}

		static void KillProcess(Process p)
		{
			if (p == null)
				return;

			try
			{
				if (!p.HasExited)
				{
					p.Kill();
					p.WaitForExit(10000);
				}
			}
			// ReSharper disable once EmptyGeneralCatchClause
			catch
			{
			}
			finally
			{
				p.Close();
			}
		}

		static bool ProcessExists(string name)
		{
			var procs = ProcessInfo.Instance.GetProcessesByName(name);
			procs.ForEach(p => p.Close());
			return procs.Length > 0;
		}
	}
}