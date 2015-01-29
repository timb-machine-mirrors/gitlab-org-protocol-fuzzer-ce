using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.OS.OSX.Agent.Monitors;

namespace Peach.Pro.Test.OS.OSX.Agent.Monitors
{
	[TestFixture] [Category("Peach")]
	public class CrashReporterTest
	{
		static string CrashingProcess
		{
			get { return Utilities.GetAppResourcePath("CrashingProgram"); }
		}

		[Test]
		public void NoProcessNoFault()
		{
			// ProcessName argument not provided to the monitor
			// When no crashing program is run, the monitor should not detect a fault

			var args = new Dictionary<string, string>();
			const string peach = "";
			const bool shouldFault = false;

			RunProcess(peach, null, shouldFault, args);
		}

		[Test]
		public void NoProcessFault()
		{
			// ProcessName argument not provided to the monitor
			// When crashing program is run, the monitor should detect a fault

			var args = new Dictionary<string, string>();
			const string peach = "qwertyuiopasdfghjklzxcvbnm";
			const bool shouldFault = true;

			var fault = RunProcess(peach, CrashingProcess, shouldFault, args);

			Assert.NotNull(fault);
			Assert.Greater(fault.collectedData.Count, 0);
			foreach (var item in fault.collectedData)
			{
				Assert.NotNull(item.Key);
				Assert.Greater(item.Value.Length, 0);
			}
		}

		[Test]
		public void ProcessFault()
		{
			// Correct ProcessName argument is provided to the monitor
			// When crashing program is run, the monitor should detect a fault

			var args = new Dictionary<string, string>
			{
				{ "ProcessName", "CrashingProgram" },
			};
			const string peach = "qwertyuiopasdfghjklzxcvbnm";
			const bool shouldFault = true;

			var fault = RunProcess(peach, CrashingProcess, shouldFault, args);

			Assert.NotNull(fault);
			Assert.Greater(fault.collectedData.Count, 0);
			foreach (var item in fault.collectedData)
			{
				Assert.NotNull(item.Key);
				Assert.Greater(item.Value.Length, 0);
			}
		}

		[Test]
		public void WrongProcessFault()
		{
			// Incorrect ProcessName argument is provided to the monitor
			// When crashing program is run, the monitor should not detect a fault

			var args = new Dictionary<string, string>
			{
				{ "ProcessName", "WrongCrashingProgram" },
			};
			const string peach = "qwertyuiopasdfghjklzxcvbnm";
			const bool shouldFault = false;

			RunProcess(peach, CrashingProcess, shouldFault, args);
		}

		private static Fault RunProcess(string peach, string process, bool shouldFault, Dictionary<string, string> args)
		{
			var reporter = new CrashReporter(null);
			reporter.StartMonitor(args);
			reporter.SessionStarting();
			reporter.IterationStarting(null);
			if (process != null)
			{
				using (var p = new System.Diagnostics.Process())
				{
					p.StartInfo = new System.Diagnostics.ProcessStartInfo();
					p.StartInfo.EnvironmentVariables["PEACH"] = peach;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.FileName = process;
					p.Start();
				}
			}
			Thread.Sleep(2000);
			reporter.IterationFinished();
			Assert.AreEqual(shouldFault, reporter.DetectedFault());
			var fault = reporter.GetMonitorData();
			reporter.StopMonitor();
			return fault;
		}
	}
}

