using System;
using Peach.Core;
using System.Runtime.InteropServices;
using System.ComponentModel;

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

		protected override void Terminate()
		{
			var ret = killpg(_process.Id, SIGTERM);
			if (ret == -1)
				throw new Win32Exception("killpg() failed"); // reads errno internally
		}

		protected override void Kill()
		{
			var ret = killpg(_process.Id, SIGKILL);
			if (ret == -1)
				throw new Win32Exception("killpg() failed"); // reads errno internally
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int killpg(int pgrp, int sig);
	}
}
