using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Peach.Core;
using Logger = NLog.Logger;
using Process = Peach.Core.Process;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Windows
{
	#region Job Object

	public class JobObject : IDisposable
	{
		#region P/Invokes

		// ReSharper disable MemberCanBePrivate.Local
		// ReSharper disable FieldCanBeMadeReadOnly.Local
		// ReSharper disable UnusedMember.Local

		const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

		enum JOBOBJECTINFOCLASS
		{
			AssociateCompletionPortInformation = 7,
			BasicLimitInformation = 2,
			BasicUIRestrictions = 4,
			EndOfJobTimeInformation = 6,
			ExtendedLimitInformation = 9,
			SecurityLimitInformation = 5,
			GroupInformation = 11
		}

		[StructLayout(LayoutKind.Sequential)]
		struct JOBOBJECT_BASIC_LIMIT_INFORMATION
		{
			public long PerProcessUserTimeLimit;
			public long PerJobUserTimeLimit;
			public uint LimitFlags;
			public IntPtr MinimumWorkingSetSize;
			public IntPtr MaximumWorkingSetSize;
			public uint ActiveProcessLimit;
			public IntPtr Affinity;
			public uint PriorityClass;
			public uint SchedulingClass;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IO_COUNTERS
		{
			public ulong ReadOperationCount;
			public ulong WriteOperationCount;
			public ulong OtherOperationCount;
			public ulong ReadTransferCount;
			public ulong WriteTransferCount;
			public ulong OtherTransferCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
			public IO_COUNTERS IoInfo;
			public IntPtr ProcessMemoryLimit;
			public IntPtr JobMemoryLimit;
			public IntPtr PeakProcessMemoryUsed;
			public IntPtr PeakJobMemoryUsed;
		}

		// ReSharper restore UnusedMember.Local
		// ReSharper restore FieldCanBeMadeReadOnly.Local
		// ReSharper restore MemberCanBePrivate.Local

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS infoType, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo, int cbJobObjectInfoLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		#endregion

		IntPtr _hJob;

		public JobObject()
		{
			_hJob = CreateJobObject(IntPtr.Zero, null);
			if (_hJob == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
			{
				BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
				{
					LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
				}
			};

			var ret = SetInformationJobObject(_hJob, JOBOBJECTINFOCLASS.ExtendedLimitInformation, ref info, Marshal.SizeOf(info));
			if (!ret)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public void Dispose()
		{
			if (_hJob != IntPtr.Zero)
			{
				CloseHandle(_hJob);
				_hJob = IntPtr.Zero;
			}
		}

		public void AssignProcess(SysProcess p)
		{
			// Will return ACCESS_DENIED on Vista/Win7 if PCA gets in the way:
			// http://stackoverflow.com/questions/3342941/kill-child-process-when-parent-process-is-killed
			var ret = AssignProcessToJobObject(_hJob, p.Handle);
			if (!ret)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}

	#endregion

	[PlatformImpl(Platform.OS.Windows)]
	public class ProcessImpl : Process
	{
		#region Remote IPC Server

		public class ProcessService : MarshalByRefObject
		{
			public class Result : MarshalByRefObject
			{
				readonly SysProcess _p;

				public Result(SysProcess p) { _p = p; }

				public int Id { get { return _p.Id; } }
				public bool HasExited { get { return _p.HasExited; } }
				public int ExitCode { get { return _p.ExitCode; } }
			}

			public Result CreateProcess(string executable,
				string arguments,
				string workingDirectory,
				Dictionary<string, string> environment)
			{
				// NOTE: We must not set CreateNoWindow to true otherwise
				// the inferrior will not properly inherit its stdout/stderr
				// http://stackoverflow.com/questions/19659206/how-do-i-correctly-launch-a-process-and-forward-stdin-stdout-stderr

				// Peach must pass us the working directory so that we don't end up
				// with the working directory changing to the location fo the trampoline
				if (string.IsNullOrEmpty(workingDirectory))
					throw new ArgumentException("The workingDirectory must be specified.");

				var si = new ProcessStartInfo
				{
					FileName = executable,
					Arguments = arguments,
					UseShellExecute = false,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = workingDirectory
				};

				if (environment != null)
					environment.ForEach(x => si.EnvironmentVariables[x.Key] = x.Value);

				var p = new SysProcess { StartInfo = si };

				p.Start();

				return new Result(p);

				// NOTE: We don't want to close the process handle so that if the process exits
				// before the Peach process tries to attach we will succeed and be able
				// to view information like the exit code.
			}
		}

		#endregion

		#region ProcessHelper Implementation

		[PlatformImpl(Platform.OS.Windows)]
		public class ProcessHelper : IProcessHelper
		{
			public ProcessRunResult Run(Logger logger, string executable, string arguments, Dictionary<string, string> environment, string workingDirectory, int timeout)
			{
				using (var p = new ProcessImpl(logger))
				{
					return p.Run(executable, arguments, environment, workingDirectory, timeout);
				}
			}

			public Process Start(Logger logger, string executable, string arguments, Dictionary<string, string> environment, string logDir)
			{
				var ret = new ProcessImpl(logger);

				try
				{
					ret.Start(executable, arguments, environment, logDir);
					return ret;
				}
				catch
				{
					ret.Dispose();
					throw;
				}
			}

			public Process GetCurrentProcess(Logger logger)
			{
				return new ProcessImpl(logger, SysProcess.GetCurrentProcess());
			}

			public Process GetProcessById(Logger logger, int id)
			{
				return new ProcessImpl(logger, SysProcess.GetProcessById(id));
			}

			public Process[] GetProcessesByName(Logger logger, string name)
			{
				return SysProcess.GetProcessesByName(name)
					.Select(p => new ProcessImpl(logger, p))
					.OfType<Process>()
					.ToArray();
			}
		}

		#endregion

		#region Helpers

		static ProcessInfo TakeSnapshot(SysProcess p)
		{
			return new ProcessInfo
			{
				Id = p.Id,
				ProcessName = p.ProcessName,
				Responding = p.Responding,
				TotalProcessorTicks = (ulong)(p.TotalProcessorTime.TotalMilliseconds * TimeSpan.TicksPerMillisecond),
				UserProcessorTicks = (ulong)(p.UserProcessorTime.TotalMilliseconds * TimeSpan.TicksPerMillisecond),
				PrivilegedProcessorTicks = (ulong)(p.PrivilegedProcessorTime.TotalMilliseconds * TimeSpan.TicksPerMillisecond),
				PeakVirtualMemorySize64 = p.PeakVirtualMemorySize64,
				PeakWorkingSet64 = p.PeakWorkingSet64,
				PrivateMemorySize64 = p.PrivateMemorySize64,
				VirtualMemorySize64 = p.VirtualMemorySize64,
				WorkingSet64 = p.WorkingSet64
			};
		}

		#endregion

		#region Base Implementation

		public ProcessImpl(Logger logger)
			: base(logger)
		{
		}

		private ProcessImpl(Logger logger, SysProcess process)
			: base(logger)
		{
			_process = new AttachedProcess(process);
		}

		protected override IProcess CreateProcess(
			string executable,
			string arguments,
			string workingDirectory,
			Dictionary<string, string> environment)
		{
			var svc = typeof(ProcessService);
			var guid = Guid.NewGuid().ToString();

			// Ensure our cwd is set since the inferrior will be invoked by the trampoline process
			var cwd = string.IsNullOrEmpty(workingDirectory) ? Environment.CurrentDirectory : workingDirectory;

			var p = new CreatedProcess();

			try
			{
				p.JobObject = new JobObject();

				using (var readyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + guid))
				{
					p.Trampoline = new SysProcess
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = Utilities.GetAppResourcePath("PeachTrampoline.exe"),
							Arguments = "--ipc {0} \"{1}\"".Fmt(guid, svc.AssemblyQualifiedName),
							UseShellExecute = false,
							RedirectStandardInput = true,
							RedirectStandardOutput = true,
							RedirectStandardError = true,
							WindowStyle = ProcessWindowStyle.Hidden,
							CreateNoWindow = true
						}
					};

					p.Trampoline.Start();

					try
					{
						p.JobObject.AssignProcess(p.Trampoline);
					}
					catch
					{
						// If we fail to add the trampoline to the job object
						// we need to manually kill it before returning
						p.Trampoline.Kill();
						throw;
					}

					var procEvt = new ManualResetEvent(false)
					{
						SafeWaitHandle = new SafeWaitHandle(p.Trampoline.Handle, false)
					};

					// Wait for either ready event or process exit
					var idx = WaitHandle.WaitAny(new WaitHandle[] { readyEvt, procEvt });

					if (idx == 2)
						throw new SoftException("Trampoline process prematurley exited!");
				}

				// Trampoline is now spawned and ready to launch the inferrior

				try
				{
					var remote = (ProcessService)Activator.GetObject(svc, "ipc://{0}/{1}".Fmt(guid, svc.Name));

					_logger.Debug("Start(): \"{0} {1}\"", executable, arguments);

					p.Proxy = remote.CreateProcess(executable, arguments, cwd, environment);
				}
				catch (RemotingException ex)
				{
					throw new SoftException("Failed to initialize remote process service.", ex);
				}

				p.Id = p.Proxy.Id;

				try
				{
					p.Inferrior = SysProcess.GetProcessById(p.Proxy.Id);
				}
				catch (ArgumentException)
				{
					// Process already exited, so kill the trapoline
					// but don't close anything so our stdout/stderr
					// readers will be able to run

					Debug.Assert(p.Proxy.HasExited);
					p.ExitCode = p.Proxy.ExitCode;

					p.Proxy = null;

					p.Trampoline.Kill();
				}

				return p;
			}
			catch (Exception)
			{
				p.Dispose();
				throw;
			}
		}

		protected override IProcess AttachProcess(int pid)
		{
			return new AttachedProcess(SysProcess.GetProcessById(pid));
		}

		#endregion

		#region Created Process Implementation

		class CreatedProcess : IProcess
		{
			public ProcessService.Result Proxy { get; set; }
			public JobObject JobObject { get; set; }
			public SysProcess Trampoline { get; set; }
			public SysProcess Inferrior { private get; set; }
			public int ExitCode { get; set; }

			public void Dispose()
			{
				if (JobObject != null)
				{
					JobObject.Dispose();
					JobObject = null;
				}

				if (Trampoline != null)
				{
					Trampoline.Close();
					Trampoline = null;
				}

				if (Inferrior != null)
				{
					Inferrior.Close();
					Inferrior = null;
				}
			}

			public int Id
			{
				get;
				set;
			}

			public bool HasExited
			{
				get { return Inferrior == null || Inferrior.HasExited; }
			}


			public StreamWriter StandardInput
			{
				get { return Trampoline.StandardInput; }
			}

			public StreamReader StandardOutput
			{
				get { return Trampoline.StandardOutput; }
			}

			public StreamReader StandardError
			{
				get { return Trampoline.StandardError; }
			}

			public ProcessInfo Snapshot()
			{
				return Inferrior == null ? null : TakeSnapshot(Inferrior);
			}

			public void Terminate()
			{
				// Attempt graceful shutdown
				var ret = Inferrior.CloseMainWindow();

				// If no wndproc, fall back on kill
				if (!ret)
					Kill();
			}

			public void Kill()
			{
				// If we try and close the inferior as a non admin we will get an access denied error
				// because it was not spawned by us. The only way to kill the inferrior is
				// to close the job object which was created by us.
				if (JobObject != null)
				{
					JobObject.Dispose();
					JobObject = null;

					if (Proxy != null)
					{
						Proxy = null;
						ExitCode = -1;
					}
				}
			}

			public bool WaitForExit(int timeout)
			{
				if (Inferrior == null)
					return true;

				if (!Inferrior.WaitForExit(timeout))
					return false;

				// When the inferrior dies we need to kill the trampoline
				// in order for stdout/stderr to return EOF.
				// Before we do that, we need to get the exit code from the proxy

				if (Proxy != null)
				{
					try
					{
						ExitCode = Proxy.ExitCode;
					}
					catch
					{
						ExitCode = -1;
					}
					finally
					{
						Proxy = null;
					}
				}

				Inferrior.Close();
				Inferrior = null;

				Kill();
				return true;
			}
		}

		#endregion

		#region Attached Process Implementation

		public class AttachedProcess : IProcess
		{
			readonly SysProcess _process;

			public AttachedProcess(SysProcess process)
			{
				_process = process;
			}

			public void Dispose()
			{
				_process.Close();
			}

			public int Id
			{
				get { return _process.Id; }
			}

			public int ExitCode
			{
				get { return _process.ExitCode; }
			}

			public bool HasExited
			{
				get { return _process.HasExited; }
			}

			public StreamWriter StandardInput
			{
				get { return _process.StandardInput; }
			}

			public StreamReader StandardOutput
			{
				get { return _process.StandardOutput; }
			}

			public StreamReader StandardError
			{
				get { return _process.StandardError; }
			}

			public ProcessInfo Snapshot()
			{
				return TakeSnapshot(_process);
			}

			public void Terminate()
			{
				var ret = _process.CloseMainWindow();

				// If no wndproc, fall back on kill
				if (!ret)
					Kill();
			}

			public void Kill()
			{
				_process.Kill();
			}

			public bool WaitForExit(int timeout)
			{
				return _process.WaitForExit(timeout);
			}
		}

		#endregion
	}
}