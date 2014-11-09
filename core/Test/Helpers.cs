using NLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Peach.Core.Test
{
	public static class Helpers
	{
		public static Dictionary<T, int> Total<T>(this IEnumerable<T> seq)
		{
			var dict = new Dictionary<T, int>();

			foreach (var item in seq)
			{
				int val;
				dict.TryGetValue(item, out val);
				dict[item] = val + 1;
			}

			return dict;
		}

		public static Process StartAgent()
		{
			var startEvent = new ManualResetEvent(false);
			var process = new Process();

			NLog.Logger logger = LogManager.GetCurrentClassLogger();
			var peach = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Peach.exe");
			logger.Error("Peach.exe: {0}", peach);

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				process.StartInfo.FileName = peach;
				process.StartInfo.Arguments = "-a tcp";
			}
			else
			{
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = "--debug {0} -a tcp".Fmt(peach);
			}

			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			var output = new List<string>();
			process.OutputDataReceived += (sender, e) =>
			{
				if (e.Data == null)
					return;

				output.Add(e.Data);
				if (e.Data.Contains("Press ENTER to quit agent"))
					startEvent.Set();
			};

			try
			{
				process.Start();
				process.BeginOutputReadLine();

				if (!startEvent.WaitOne(5000))
				{
					Assert.Fail(string.Join("\n", output.ToArray()));
				}

				process.CancelOutputRead();

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
