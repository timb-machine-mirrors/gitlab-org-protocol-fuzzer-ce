using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Peach.Core;
using System.Linq;
using System.IO;

namespace Peach.Pro.OS.OSX
{
	[PlatformImpl(Platform.OS.OSX)]
	public class ProcessInfoImpl : IProcessInfo
	{
		#region P/Invoke Stuff

		// <libproc.h>
		[DllImport("libc")]
		private static extern int proc_pidinfo(int pid, int flavor, ulong arg, IntPtr buffer, int buffersize);

		// <sys/proc_info.h>
		[StructLayout(LayoutKind.Sequential)]
		struct proc_taskinfo
		{
			public ulong pti_virtual_size;       /* virtual memory size (bytes) */
			public ulong pti_resident_size;      /* resident memory size (bytes) */
			public ulong pti_total_user;         /* total time */
			public ulong pti_total_system;
			public ulong pti_threads_user;       /* existing threads only */
			public ulong pti_threads_system;
			public int pti_policy;               /* default policy for new threads */
			public int pti_faults;               /* number of page faults */
			public int pti_pageins;              /* number of actual pageins */
			public int pti_cow_faults;           /* number of copy-on-write faults */
			public int pti_messages_sent;        /* number of messages sent */
			public int pti_messages_received;    /* number of messages received */
			public int pti_syscalls_mach;        /* number of mach system calls */
			public int pti_syscalls_unix;        /* number of unix system calls */
			public int pti_csw;                  /* number of context switches */
			public int pti_threadnum;            /* number of threads in the task */
			public int pti_numrunning;           /* number of running threads */
			public int pti_priority;             /* task priority*/
		}

		// <sys/proc_info.h>
		private static int PROC_PIDTASKINFO { get { return 4; } }

		// sizeof(struct kinfo_proc)
		private static int kinfo_proc_size { get { return 648; } }

		// <sys/proc.h>
		// Only contains the interesting parts at the beginning of the struct.
		// However, we allocate kinfo_proc_size when calling the sysctl.
		[StructLayout(LayoutKind.Sequential)]
		struct extern_proc
		{
			public int p_starttime_tv_sec;
			public int p_starttime_tv_usec;
			public IntPtr p_vmspace;
			public IntPtr p_sigacts;
			public int p_flag;
			public byte p_stat;
			public int p_pid;
			public int p_oppid;
			public int p_dupfd;
			public IntPtr user_stack;
			public IntPtr exit_thread;
			public int p_debugger;
			public int sigwait;
			public uint p_estcpu;
			public int p_cpticks;
			public uint p_pctcpu;
			public IntPtr p_wchan;
			public IntPtr p_wmesg;
			public uint p_swtime;
			public uint p_slptime;
			public uint p_realtimer_it_interval_tv_sec;
			public uint p_realtimer_it_interval_tv_usec;
			public uint p_realtimer_it_value_tv_sec;
			public uint p_realtimer_it_value_tv_usec;
			public uint p_rtime_tv_sec;
			public uint p_rtime_tv_usec;
			public ulong p_uticks;
			public ulong p_sticks;
			public ulong p_iticks;
		}

		// <sys/sysctl.h>
		private static int CTL_KERN = 1;
		private static int KERN_PROC = 14;
		private static int KERN_PROC_PID = 1;
		private static int KERN_ARGMAX = 8;
		private static int KERN_PROCARGS2 = 49;

		// <sys/proc.h>
		private enum p_stat : byte
		{
			SIDL = 1, // Process being created by fork.
			SRUN = 2, // Currently runnable.
			SSLEEP = 3, // Sleeping on an address.
			SSTOP = 4, // Process debugging or suspension.
			SZOMB = 5, // Awiting collection by parent.
		}

		[DllImport("libc")]
		private static extern int sysctl([MarshalAs(UnmanagedType.LPArray)] int[] name, uint namelen, IntPtr oldp, ref int oldlenp, IntPtr newp, int newlen);

		#endregion

		class HGlobal : IDisposable
		{
			public IntPtr Pointer { get; private set; }

			public HGlobal(int size)
			{
				Pointer = Marshal.AllocHGlobal(size);
			}

			public void Dispose()
			{
				Marshal.FreeHGlobal(Pointer);
			}

			public T ToStruct<T>()
			{
				return (T)Marshal.PtrToStructure(Pointer, typeof(T));
			}

			public int ReadInt32()
			{
				return Marshal.ReadInt32(Pointer);
			}

			public string ReadString(int offset)
			{
				return Marshal.PtrToStringAnsi(Pointer + offset);
			}
		}

		private static extern_proc? GetKernProc(int pid)
		{
			var mib = new[] {
				CTL_KERN,
				KERN_PROC,
				KERN_PROC_PID,
				pid
			};

			var len = kinfo_proc_size;
			using (var ptr = new HGlobal(len))
			{
				var ret = sysctl(mib, (uint)mib.Length, ptr.Pointer, ref len, IntPtr.Zero, 0);
				if (ret == -1)
					return null;
				return ptr.ToStruct<extern_proc>();
			}
		}

		private static proc_taskinfo? GetTaskInfo(int pid)
		{
			var len = Marshal.SizeOf(typeof(proc_taskinfo));
			using (var ptr = new HGlobal(len))
			{
				var err = proc_pidinfo(pid, PROC_PIDTASKINFO, 0, ptr.Pointer, len);
				if (err != len)
					return null;
				return ptr.ToStruct<proc_taskinfo>();
			}
		}

		private static int _argmax;

		private static int GetArgMax()
		{
			if (_argmax == 0)
			{
				var mib = new[]
				{
					CTL_KERN,
					KERN_ARGMAX
				};

				var len = sizeof(int);
				using (var ptr = new HGlobal(len))
				{
					var ret = sysctl(mib, (uint)mib.Length, ptr.Pointer, ref len, IntPtr.Zero, 0);
					if (ret == -1)
						throw new PeachException("ProcessInfoImpl: Could not get KERN_ARGMAX");
					_argmax = ptr.ReadInt32();
				}
			}
			return _argmax;
		}

		// Reference:
		// http://opensource.apple.com/source/adv_cmds/adv_cmds-153/ps/print.c
		//
		private static string GetName(Process p)
		{
			var mib = new[]
			{
				CTL_KERN,
				KERN_PROCARGS2,
				p.Id
			};

			var argmax = GetArgMax();
			using (var ptr = new HGlobal(argmax))
			{
				var ret = sysctl(mib, (uint)mib.Length, ptr.Pointer, ref argmax, IntPtr.Zero, 0);
				if (ret == -1)
					return ""; // ignore errors, usually access denied

				// skip past argc which is an int
				return Path.GetFileName(ptr.ReadString(sizeof(int)));
			}
		}

		private static void RaiseError(Process p)
		{
			bool hasExited;

			try
			{
				hasExited = p.HasExited;
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Failed to check running status of pid '{0}'.".Fmt(p.Id), ex);
			}

			if (hasExited)
				throw new ArgumentException("Can't query info for pid '{0}', it has already exited.".Fmt(p.Id));

			throw new UnauthorizedAccessException("Can't query info for pid '{0}', ensure user has appropriate permissions".Fmt(p.Id));
		}

		public ProcessInfo Snapshot(Process p)
		{
			var kp = GetKernProc(p.Id);
			if (!kp.HasValue)
				RaiseError(p);

			var ti = GetTaskInfo(p.Id);
			if (!ti.HasValue)
				RaiseError(p);

			var pi = new ProcessInfo
			{
				Id = p.Id,
				ProcessName = GetName(p),
				Responding = kp.Value.p_stat != (byte)p_stat.SZOMB,
				UserProcessorTicks = ti.Value.pti_total_user,
				PrivilegedProcessorTicks = ti.Value.pti_total_system,

				VirtualMemorySize64 = (long)ti.Value.pti_virtual_size,
				WorkingSet64 = (long)ti.Value.pti_resident_size,
				PrivateMemorySize64 = 0,
				PeakVirtualMemorySize64 = 0,
				PeakWorkingSet64 = 0,
			};
			pi.TotalProcessorTicks = pi.UserProcessorTicks + pi.PrivilegedProcessorTicks;

			return pi;
		}

		public Process[] GetProcessesByName(string name)
		{
			var ret = new List<Process>();

			foreach (var p in Process.GetProcesses())
			{
				if (GetName(p) == name)
					ret.Add(p);
				else
					p.Dispose();
			}

			return ret.ToArray();
		}
	}
}
