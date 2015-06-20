using System;
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

			var temp1 = GetTempExeName();
			var exe = temp1.Item1;
			var procName = temp1.Item2;

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

			var temp1 = GetTempExeName();
			var exe1 = temp1.Item1;
			var procName1 = temp1.Item2;

			var temp2 = GetTempExeName();
			var exe2 = temp2.Item1;
			var procName2 = temp2.Item2;

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

		[Test]
		public void TestKillingProcess()
		{
			var temp = GetTempExeName();
			var exe = temp.Item1;
			var procName = temp.Item2;

			Process p = null;

			try
			{
				p = RunProcess(exe, "127.0.0.1 0");

				p.Kill();
				p.WaitForExit();

				Assert.True(p.HasExited, "Process should have exited");

				// Killing a process that has exited should throw
				// an InvalidOperationException
				Assert.Throws<InvalidOperationException>(() => p.Kill());
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
		}

		static Tuple<string, string> GetTempExeName()
		{
			const string cs = "CrashableServer";
			var suffix = Platform.GetOS() == Platform.OS.Windows ? ".exe" : "";
			var tmp = Path.GetTempFileName();
			var dir = Path.GetDirectoryName(tmp);

			if (string.IsNullOrEmpty(dir))
				Assert.Fail("Temp directory should not be null or empty");

			var procName = cs + "-" + Guid.NewGuid();
			var fileName = Path.Combine(dir, procName + suffix);

			File.Delete(tmp);
			File.Copy(Utilities.GetAppResourcePath(cs + suffix), fileName);

			return new Tuple<string, string>(fileName, procName);
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
				if (p.HasExited)
					return;

				try
				{
					p.Kill();
				}
				catch (InvalidOperationException)
				{
				}

				// Waiting is ok since we created the process
				p.WaitForExit();
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