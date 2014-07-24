using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;	

namespace Peach.Core.Test
{
	public static class Helpers
	{
		public static Process StartAgent()
		{
			var startEvent = new ManualResetEvent(false);
			var process = new Process();

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				process.StartInfo.FileName = "Peach.exe";
				process.StartInfo.Arguments = "-a tcp";
			}
			else
			{
				List<string> paths = new List<string>();
				paths.Add(Environment.CurrentDirectory);
				paths.AddRange(process.StartInfo.EnvironmentVariables["PATH"].Split(Path.PathSeparator));
				string peach = "Peach.exe";
				foreach (var dir in paths)
				{
					var candidate = Path.Combine(dir, peach);
					if (File.Exists(candidate))
					{
						peach = candidate;
						break;
					}
				}

				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = "--debug {0} -a tcp".Fmt(peach);
			}

			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.OutputDataReceived += (sender, e) =>
			{
				if (e.Data == null)
					return;

				if (e.Data.Contains("Press ENTER to quit agent"))
					startEvent.Set();
			};

			try
			{
				process.Start();
				if (Platform.GetOS() == Platform.OS.Windows)
					process.BeginOutputReadLine();

				startEvent.WaitOne(5000);

				return process;
			}
			catch
			{
				process = null;
				throw;
			}
			finally
			{
				startEvent.Dispose();
				startEvent = null;
			}
		}

		public static void StopAgent(Process process)
		{
			if (!process.HasExited)
			{
				process.Kill();
				process.WaitForExit();
			}

			process.Close();
			process = null;
		}
	}
}
