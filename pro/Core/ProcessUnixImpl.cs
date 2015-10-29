using System;
using Peach.Core;
using System.Runtime.InteropServices;
using System.ComponentModel;
using SysProcess = System.Diagnostics.Process;
using System.Threading;

namespace Peach.Pro.Core
{
	public abstract class ProcessUnixImpl : Process
	{
		const int SIGKILL = 9;
		const int SIGTERM = 15;

		protected ProcessUnixImpl(NLog.Logger logger) : base(logger)
		{
		}

		protected override string FileName(string executable)
		{
			return Utilities.GetAppResourcePath("PeachTrampoline");
		}

		protected override string Arguments(string executable, string arguments)
		{
			return executable + " " + arguments;
		}

		protected override void Terminate(SysProcess process)
		{
			var ret = killpg(process.Id, SIGTERM);
			if (ret == -1)
				throw new Win32Exception("killpg({0}, SIGTERM) failed".Fmt(process.Id)); // reads errno internally
		}

		protected override void Kill(SysProcess process)
		{
			var ret = killpg(process.Id, SIGKILL);
			if (ret == -1)
				throw new Win32Exception("killpg({0}, SIGKILL) failed".Fmt(process.Id)); // reads errno internally
		}

		protected override void WaitForProcessGroup(SysProcess process)
		{
			while (getpgid(process.Id) != process.Id)
				Thread.Sleep(10);
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int killpg(int pgrp, int sig);

		[DllImport("libc", SetLastError = true)]
		private static extern int getpgid(int pid);
	}
}
