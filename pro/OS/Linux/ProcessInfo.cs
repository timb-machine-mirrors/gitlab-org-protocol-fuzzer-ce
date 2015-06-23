using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using NLog;
using Peach.Core;

namespace Peach.Pro.OS.Linux
{
	[PlatformImpl(Platform.OS.Linux)]
	public class ProcessInfoImpl : IProcessInfo
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private const string StatPath = "/proc/{0}/stat";

		private enum Fields
		{
			State = 0,
			UserTime = 11,
			KernelTime = 12,
			Max = 13,
		}

		private static Tuple<string, string[]> ReadProc(int pid, bool stats)
		{
			var path = string.Format(StatPath, pid);
			string stat;

			try
			{
				stat = File.ReadAllText(path);
			}
			catch (Exception ex)
			{
				Logger.Info("Failed to read \"{0}\".  {1}", path, ex.Message);
				return new Tuple<string,string[]>(null, null);
			}

			var start = stat.IndexOf('(');
			var end = stat.LastIndexOf(')');

			if (stat.Length < 2 || start < 0 || end < start)
				return new Tuple<string, string[]>(null, null);

			var before = stat.Substring(0, start);
			var middle = stat.Substring(start + 1, end - start - 1);
			var after = stat.Substring(end + 1);

			var strPid = before.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (strPid.Length != 1 || strPid[0] != pid.ToString(CultureInfo.InvariantCulture))
				return new Tuple<string, string[]>(null, null);

			if (string.IsNullOrEmpty(middle))
				return new Tuple<string, string[]>(null, null);

			if (!stats)
				return new Tuple<string, string[]>(middle, null);

			var parts = after.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < (int)Fields.Max)
				return null;

			return new Tuple<string, string[]>(middle, parts);
		}

		public ProcessInfo Snapshot(Process p)
		{
			var tuple = ReadProc(p.Id, true);
			if (tuple.Item1 == null)
				throw new ArgumentException();

			var parts = tuple.Item2;

			var pi = new ProcessInfo
			{
				Id = p.Id
			};

			try
			{
				pi.ProcessName = p.ProcessName;
			}
			catch (InvalidOperationException)
			{
				pi.ProcessName = tuple.Item1;
			}

			pi.Responding = parts[(int)Fields.State] != "Z";

			pi.UserProcessorTicks = ulong.Parse(parts[(int)Fields.UserTime]);
			pi.PrivilegedProcessorTicks = ulong.Parse(parts[(int)Fields.KernelTime]);
			pi.TotalProcessorTicks = pi.UserProcessorTicks + pi.PrivilegedProcessorTicks;

			pi.PrivateMemorySize64 = p.PrivateMemorySize64;         // /proc/[pid]/status VmData
			pi.VirtualMemorySize64 = p.VirtualMemorySize64;         // /proc/[pid]/status VmSize
			pi.PeakVirtualMemorySize64 = p.PeakVirtualMemorySize64; // /proc/[pid]/status VmPeak
			pi.WorkingSet64 = p.WorkingSet64;                       // /proc/[pid]/status VmRSS
			pi.PeakWorkingSet64 = p.PeakWorkingSet64;               // /proc/[pid]/status VmHWM

			return pi;
		}

		public Process[] GetProcessesByName(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			var ret = new List<Process>();

			foreach (var p in Process.GetProcesses())
			{
				string procName;

				try
				{
					procName = p.ProcessName;
				}
				catch (InvalidOperationException)
				{
					procName = ReadProc(p.Id, false).Item1;
				}

				if (name == procName)
					ret.Add(p);
				else
					p.Dispose();
			}

			return ret.ToArray();
		}

		public void Kill(Process p)
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

			while (!p.HasExited)
				Thread.Sleep(10);
		}

		public bool Kill(Process p, int milliseconds)
		{
			if (p.HasExited)
				return true;

			try
			{
				p.Kill();
			}
			catch (InvalidOperationException)
			{
			}

			// Process.WaitForExit doesn't work on processes
			// that were not started from within mono.
			// waitpid returns ECHILD

			var sw = Stopwatch.StartNew();

			if (milliseconds <= 0)
				return p.HasExited;

			while (true)
			{
				if (p.HasExited)
					return true;

				var remain = milliseconds - sw.ElapsedMilliseconds;
				if (remain <= 0)
					return false;

				Thread.Sleep(Math.Min((int)remain, 10));
			}
		}
	}

}
