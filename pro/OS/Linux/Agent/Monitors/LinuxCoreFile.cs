﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.OS.Linux.Agent.Monitors
{
	[Monitor("LinuxCoreFile")]
	[Alias("LinuxCrashMonitor")]
	[Description("Detect when a process crashes and collect there resulting core file")]
	[Parameter("Executable", typeof(string), "Target executable used to filter crashes.", "")]
	[Parameter("LogFolder", typeof(string), "Folder with log files. Defaults to /var/peachcrash", "/var/peachcrash")]
	[Parameter("Mono", typeof(string), "Full path and executable for mono runtime. Defaults to /usr/bin/mono.", "/usr/bin/mono")]
	public class LinuxCoreFile : Monitor
	{
		protected string corePattern = "|{0} {1} -p=%p -u=%u -g=%g -s=%s -t=%t -h=%h -e=%e";
		protected string origionalCorePattern = null;
		protected string origionalSuidDumpable = null;
		protected string linuxCrashHandlerExe = "PeachLinuxCrashHandler.exe";
		protected bool logFolderCreated = false;

		protected string data = null;
		protected List<string> startingFiles = new List<string>();

		public string LogFolder { get; private set; }
		public string Executable { get; private set; }
		public string Mono { get; private set; }

		public LinuxCoreFile(string name)
			: base(name)
		{
			// TODO
			// 1) Remove dependency on PeachLinuxCrashHandler.exe
			// 2) Use embedded resource PeachCrashHandler.sh
			// 3) Maybe move the install/uninstall logic into the script
		}

		public override void  StopMonitor()
		{
			// Cleanup
			SessionFinished();
		}

		public override void  SessionStarting()
		{
			// Ensure the crash handler has been installed at the right place
			string handler = Path.DirectorySeparatorChar + linuxCrashHandlerExe;
			if (!File.Exists(handler))
				throw new PeachException("Error, LinuxCoreFile did not find crash handler located at '" + handler + "'.");

            try
            {
                origionalCorePattern = File.ReadAllText("/proc/sys/kernel/core_pattern", System.Text.Encoding.ASCII);
            }
            catch(UnauthorizedAccessException ae)
            {
                throw new PeachException("Error, Peach does not have permissions to access core_pattern: re-run Peach as root or elevated user", ae);
            }
            catch(Exception ex)
            {
                throw new PeachException("Error, accessing core_pattern failed",ex);
            }

		    if (origionalCorePattern.IndexOf(linuxCrashHandlerExe) == -1)
			{
				// Register our crash handler via proc file system

				var corePat = string.Format(corePattern,
					Mono,
					linuxCrashHandlerExe);

				File.WriteAllText(
					"/proc/sys/kernel/core_pattern",
					corePat,
					System.Text.Encoding.ASCII);

				var checkWrite = File.ReadAllText("/proc/sys/kernel/core_pattern", System.Text.Encoding.ASCII);
				if (checkWrite.IndexOf(linuxCrashHandlerExe) == -1)
					throw new PeachException("Error, LinuxCoreFile was unable to update /proc/sys/kernel/core_pattern.");
			}
			else
				origionalCorePattern = null;

			origionalSuidDumpable = File.ReadAllText("/proc/sys/fs/suid_dumpable");
			if (!origionalSuidDumpable.StartsWith("1"))
			{
				// Enable core files for all binaries, regardless of suid or protections
				File.WriteAllText(
					"/proc/sys/fs/suid_dumpable",
					"1",
					System.Text.Encoding.ASCII);

				var checkWrite = File.ReadAllText("/proc/sys/fs/suid_dumpable", System.Text.Encoding.ASCII);
				if (!checkWrite.StartsWith("1"))
					throw new PeachException("Error, LinuxCoreFile was unable to update /proc/sys/fs/suid_dumpable.");
			}
			else
				origionalSuidDumpable = null;

			if (Directory.Exists(LogFolder))
				DeleteLogFolder();

			try
			{
				Directory.CreateDirectory(LogFolder);
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, LinuxCoreFile was unable to create the log directory.  " + ex.Message, ex);
			}

			logFolderCreated = true;

			// Enable core files
			UlimitUnlimited();
		}

		public override void  SessionFinished()
		{
			// only replace core_pattern if we updated it.
			if (origionalCorePattern != null)
			{
				File.WriteAllText("/proc/sys/kernel/core_pattern", origionalCorePattern, System.Text.Encoding.ASCII);
			}

			// only replace suid_dumpable if we updated it.
			if (origionalSuidDumpable != null)
			{
				File.WriteAllText("/proc/sys/fs/suid_dumpable", origionalSuidDumpable, System.Text.Encoding.ASCII);
			}

			// Remove folder
			if (logFolderCreated)
				DeleteLogFolder();

			logFolderCreated = false;
		}

		private void DeleteLogFolder()
		{
			try
			{
				Directory.Delete(LogFolder, true);
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, LinuxCoreFile was unable to clear the log directory.  " + ex.Message, ex);
			}
		}

		public override bool  DetectedFault()
		{
			Thread.Sleep (250);
			
			foreach (var file in Directory.GetFiles(LogFolder))
			{
				if (Executable != null)
				{
					if (file.IndexOf(Executable) != -1)
					{
						return true;
					}
				}
				else
					return true;
			}

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			var title = string.IsNullOrEmpty(Executable)
				? "Crash dump found."
				: "{0} crash dump found.".Fmt(Executable);

			var ret = new MonitorData
			{
				Title = title,
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info
				{
					MajorHash = Hash(Class + Executable),
					MinorHash = Hash("CORE"),
				}
			};

			foreach (var file in Directory.GetFiles(LogFolder))
			{
				if(startingFiles.Contains(file))
					continue;

				try
				{
					if (Executable != null)
					{
						if (file.IndexOf(Executable) != -1)
						{
							var key = Path.GetFileName(file);
							Debug.Assert(key != null);
							ret.Data.Add(key, new MemoryStream(File.ReadAllBytes(file)));
							File.Delete(file);
							break;
						}
					}
					else
					{
						// Support multiple crash files
						var key = Path.GetFileName(file);
						Debug.Assert(key != null);
						ret.Data.Add(key, new MemoryStream(File.ReadAllBytes(file)));
						File.Delete(file);
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					throw new PeachException("Error, LinuxCoreFile was unable to read the crash log.  " + ex.Message, ex);
				}
			}

			return ret;
		}

		#region Ulimit

		private static void UlimitUnlimited()
		{
			rlimit rlim = new rlimit();

			if (0 != getrlimit(rlimit_resource.RLIMIT_CORE, ref rlim))
			{
				int err = Marshal.GetLastWin32Error();
				Win32Exception ex = new Win32Exception(err);
				throw new PeachException("Error, LinuxCrashHandler could not query the core size resource limit.  " + ex.Message, ex);
			}

			rlim.rlim_curr = rlim.rlim_max;

			if (0 != setrlimit(rlimit_resource.RLIMIT_CORE, ref rlim))
			{
				int err = Marshal.GetLastWin32Error();
				Win32Exception ex = new Win32Exception(err);
				throw new PeachException("Error, LinuxCrashHandler could not set the core size resource limit.  " + ex.Message, ex);
			}
		}

		enum rlimit_resource : int
		{
			RLIMIT_CPU = 0,
			RLIMIT_FSIZE = 1,
			RLIMIT_DATA = 2,
			RLIMIT_STACK = 3,
			RLIMIT_CORE = 4,
			RLIMIT_RSS = 5,
			RLIMIT_NPROC = 6,
			RLIMIT_NOFILE = 7,
			RLIMIT_MEMLOCK = 8,
			RLIMIT_AS = 9,
			RLIMIT_LOCKS = 10,
			RLIMIT_SIGPENDING = 11,
			RLIMIT_MSGQUEUE = 12,
			RLIMIT_NICE = 13,
			RLIMIT_RTPRIO = 14,
			RLIMIT_RTTIME = 15,
			RLIMIT_NLIMITS = 16,
		};

		struct rlimit
		{
			public IntPtr rlim_curr;
			public IntPtr rlim_max;
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int getrlimit(rlimit_resource resource, ref rlimit rlim);

		[DllImport("libc", EntryPoint = "getrlimit", SetLastError = true)]
		private static extern int setrlimit(rlimit_resource resource, ref rlimit rlim);

		#endregion
	}
}
