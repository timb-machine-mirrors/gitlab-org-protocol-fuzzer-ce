using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Win32.SafeHandles;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using Monitor = System.Threading.Monitor;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("WindowsKernelDebugger")]
	[Description("Debugger monitor for the windows kernel")]
	[Parameter("KernelConnectionString", typeof(string), "Connection string for kernel debugging.")]
	[Parameter("SymbolsPath", typeof(string), "Optional Symbol path.  Default is Microsoft public symbols server.", "SRV*http://msdl.microsoft.com/download/symbols")]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", "")]
	[Parameter("IgnoreFirstChanceGuardPage", typeof(bool), "Ignore first chance guard page faults.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("IgnoreSecondChanceGuardPage", typeof(bool), "Ignore second chance guard page faults.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart process after any fault occurs", "false")]
	[Parameter("ConnectTimeout", typeof(uint), "How long to wait for kernel connection.", "3000")]
	public class WindowsKernelDebugger : Monitor2
	{
		public string KernelConnectionString { get; set; }
		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }
		public bool IgnoreFirstChanceGuardPage { get; set; }
		public bool IgnoreSecondChanceGuardPage { get; set; }
		public bool RestartAfterFault { get; set; }
		public uint ConnectTimeout { get; set; }

		IKernelDebugger _debugger;

		public WindowsKernelDebugger(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			WinDbgPath = FindWinDbg(WinDbgPath);

			LaunchDebugger();
		}

		public override void SessionStarting()
		{
			_debugger.WaitForConnection(ConnectTimeout);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (args.LastWasFault)
				_debugger.WaitForConnection(ConnectTimeout);
		}

		public override void StopMonitor()
		{
			if (_debugger != null)
			{
				_debugger.Dispose();
				_debugger = null;
			}
		}

		public override bool DetectedFault()
		{
			return _debugger.Fault != null;
		}

		public override MonitorData GetMonitorData()
		{
			var ret = _debugger.Fault;

			// Restart the debugger
			LaunchDebugger();

			return ret;
		}

		private void LaunchDebugger()
		{
			if (_debugger != null)
				_debugger.Dispose();

			_debugger = new KernelDebugger
			{
				SymbolsPath = SymbolsPath,
				WinDbgPath = WinDbgPath,
				IgnoreFirstChanceGuardPage = IgnoreFirstChanceGuardPage,
				IgnoreSecondChanceGuardPage = IgnoreSecondChanceGuardPage,
			};

			_debugger.AcceptKernel(KernelConnectionString);
		}

		public static string FindWinDbg(string winDbgPath)
		{
			if (!string.IsNullOrEmpty(winDbgPath))
			{
				var file = Path.Combine(winDbgPath, "dbgeng.dll");

				if (!File.Exists(file))
					throw new PeachException("Error, provided WinDbgPath '{0}' does not exist.".Fmt(winDbgPath));

				var type = FileInfoImpl.GetMachineType(file);

				if (Environment.Is64BitProcess && type != Platform.Architecture.x64)
					throw new PeachException("Error, provided WinDbgPath '{0}' is not x64.".Fmt(winDbgPath));

				if (!Environment.Is64BitProcess && type != Platform.Architecture.x86)
					throw new PeachException("Error, provided WinDbgPath '{0}' is not x86.".Fmt(winDbgPath));

				return winDbgPath;
			}

			// Lets try a few common places before failing.
			var pgPaths = new List<string>
			{
				Environment.GetEnvironmentVariable("SystemDrive"),
				Environment.GetEnvironmentVariable("ProgramFiles"),
				Environment.GetEnvironmentVariable("ProgramW6432"),
				Environment.GetEnvironmentVariable("ProgramFiles"),
				Environment.GetEnvironmentVariable("ProgramFiles(x86)")
			};


			var dbgPaths = new List<string>
			{
				"Debuggers",
				"Debugger",
				"Debugging Tools for Windows",
				"Debugging Tools for Windows (x64)",
				"Debugging Tools for Windows (x86)",
				"Windows Kits\\8.0\\Debuggers\\x64",
				"Windows Kits\\8.0\\Debuggers\\x86",
				"Windows Kits\\8.1\\Debuggers\\x64",
				"Windows Kits\\8.1\\Debuggers\\x86"
			};

			foreach (var pg in pgPaths)
			{
				foreach (var dbg in dbgPaths)
				{
					var path = Path.Combine(pg, dbg);

					if (!Directory.Exists(path))
						continue;

					var file = Path.Combine(path, "dbgeng.dll");

					if (!File.Exists(file))
						continue;

					//verify x64 vs x86
					var type = FileInfoImpl.GetMachineType(file);

					if (Environment.Is64BitProcess && type != Platform.Architecture.x64)
						continue;

					if (!Environment.Is64BitProcess && type != Platform.Architecture.x86)
						continue;

					return path;
				}
			}

			throw new PeachException("Error, unable to locate WinDbg, please specify using 'WinDbgPath' parameter.");
		}
	}

	public class DebuggerServer : MarshalByRefObject
	{
		public KernelDebuggerInstance GetKernelDebugger(int logLevel)
		{
			Utilities.ConfigureLogging(logLevel);

			return new KernelDebuggerInstance();
		}
	}

	public interface IKernelDebugger : IDisposable
	{
		void AcceptKernel(string kernelConnectionString);
		void WaitForConnection(uint timeout);

		MonitorData Fault { get; }

		string SymbolsPath { get; set; }
		string WinDbgPath { get; set; }
		bool IgnoreFirstChanceGuardPage { get; set; }
		bool IgnoreSecondChanceGuardPage { get; set; }
	}

	public class KernelDebugger : IKernelDebugger
	{
		#region Job Object

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

		static readonly IntPtr _hJob;

		static KernelDebugger()
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

		static void AssignProcessToJobObject(SysProcess p)
		{
			// Will return ACCESS_DENIED on Vista/Win7 if PCA gets in the way:
			// http://stackoverflow.com/questions/3342941/kill-child-process-when-parent-process-is-killed
			var ret = AssignProcessToJobObject(_hJob, p.Handle);
			if (!ret)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		#endregion

		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		SysProcess _process;
		KernelDebuggerInstance _dbg;

		public string KernelConnectionString
		{
			get { return Guard(() => _dbg.KernelConnectionString); }
		}

		public MonitorData Fault
		{
			get { return Guard(() => _dbg.Fault); }
		}

		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }
		public bool IgnoreFirstChanceGuardPage { get; set; }
		public bool IgnoreSecondChanceGuardPage { get; set; }

		public void Dispose()
		{
			_dbg = null;

			if (_process == null)
				return;

			// No way to gracefully stop kernel debugger so just kill process

			try
			{
				_process.Kill();
			}
			catch (InvalidOperationException)
			{
				// Process already exited
			}

			_process.WaitForExit();
			_process.Dispose();
			_process = null;
		}

		public void AcceptKernel(string kernelConnectionString)
		{
			if (_process != null)
				throw new NotSupportedException("Kernel debugger is already running.");

			var channel = StartProcess();

			Logger.Debug("Creating KernelDebuggerInstance from {0}", channel);

			try
			{
				var remote = (DebuggerServer)Activator.GetObject(typeof(DebuggerServer), channel);

				var logLevel = Logger.IsTraceEnabled ? 2 : (Logger.IsDebugEnabled ? 1 : 0);

				_dbg = remote.GetKernelDebugger(logLevel);
				_dbg.SymbolsPath = SymbolsPath;
				_dbg.WinDbgPath = WinDbgPath;
				_dbg.IgnoreFirstChanceGuardPage = IgnoreFirstChanceGuardPage;
				_dbg.IgnoreSecondChanceGuardPage = IgnoreSecondChanceGuardPage;

				_dbg.AcceptKernel(kernelConnectionString);
			}
			catch (RemotingException ex)
			{
				throw new SoftException("Failed to initialize kernel debugger process.", ex);
			}
		}

		public void WaitForConnection(uint timeout)
		{
			try
			{
				_dbg.WaitForConnection(timeout);
			}
			catch (TimeoutException ex)
			{
				throw new SoftException(ex);
			}
			catch (RemotingException ex)
			{
				throw new SoftException("Error occured when waiting for kernel connection.", ex);
			}
		}

		string StartProcess()
		{
			var guid = Guid.NewGuid().ToString();

			using (var readyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + guid))
			{
				_process = new SysProcess()
				{
					StartInfo = new ProcessStartInfo
					{
						CreateNoWindow = true,
						UseShellExecute = false,
						Arguments = "--ipc {0} \"{1}\"".Fmt(guid, typeof(DebuggerServer).AssemblyQualifiedName),
						FileName = Utilities.GetAppResourcePath("PeachTrampoline.exe")
					}
				};

				if (Logger.IsTraceEnabled)
				{
					_process.EnableRaisingEvents = true;
					_process.OutputDataReceived += LogProcessData;
					_process.ErrorDataReceived += LogProcessData;
					_process.StartInfo.RedirectStandardError = true;
					_process.StartInfo.RedirectStandardOutput = true;
				}

				_process.Start();

				// Add process to JobObject so it will get killed if peach crashes
				AssignProcessToJobObject(_process);

				if (Logger.IsTraceEnabled)
				{
					_process.BeginErrorReadLine();
					_process.BeginOutputReadLine();
				}

				var procEvt = new ManualResetEvent(false)
				{
					SafeWaitHandle = new SafeWaitHandle(_process.Handle, false)
				};

				// Wait for either ready event or process exit
				var idx = WaitHandle.WaitAny(new WaitHandle[] { readyEvt, procEvt });

				if (idx == 2)
					throw new SoftException("Debugger process prematurley exited!");
			}

			return "ipc://" + guid + "/DebuggerServer";
		}

		T Guard<T>(Func<T> func)
		{
			if (_dbg == null)
				return default(T);

			try
			{
				return func();
			}
			catch (RemotingException ex)
			{
				throw new SoftException(ex);
			}
		}

		static void LogProcessData(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
				Logger.Debug(e.Data);
		}
	}

	public class KernelDebuggerInstance : MarshalByRefObject, IKernelDebugger
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		#region WinDbg Wrapper

		class WinDbg : IDisposable, IDebugEventCallbacks, IDebugOutputCallbacks
		{
			static readonly Regex ReMajorHash = new Regex(@"^MAJOR_HASH:(0x.*)\r$", RegexOptions.Multiline);
			static readonly Regex ReMinorHash = new Regex(@"^MINOR_HASH:(0x.*)\r$", RegexOptions.Multiline);
			static readonly Regex ReRisk = new Regex(@"^CLASSIFICATION:(.*)\r$", RegexOptions.Multiline);
			static readonly Regex ReTitle = new Regex(@"^SHORT_DESCRIPTION:(.*)\r$", RegexOptions.Multiline);

			const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

			[DllImport("kernel32.dll", SetLastError = true)]
			static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReserved, uint dwFlags);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool FreeLibrary(IntPtr hModule);

			[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
			static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

			delegate uint DebugCreate(ref Guid InterfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object Interface);

			readonly StringBuilder _output = new StringBuilder();

			IntPtr _hDll;
			object _dbgEng;
			bool _handlingException;

			public Action OnConnected { private get; set; }

			public IDebugClient DebugClient { get; private set; }
			public IDebugControl DebugControl { get; private set; }
			public IDebugSymbols DebugSymbols { get; private set; }

			public MonitorData Fault { get; private set; }

			public bool IgnoreFirstChanceGuardPage { private get; set; }
			public bool IgnoreSecondChanceGuardPage { private get; set; }
			public bool IgnoreBreakpoint { private get; set; }

			public WinDbg(string winDbgPath)
			{
				if (!Path.IsPathRooted(winDbgPath))
					throw new ArgumentException("Must be an absolute path.", "winDbgPath");

				_hDll = LoadLibraryEx(winDbgPath, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
				if (_hDll == IntPtr.Zero)
					throw new Win32Exception();

				var addr = GetProcAddress(_hDll, "DebugCreate");
				var func = (DebugCreate)Marshal.GetDelegateForFunctionPointer(addr, typeof(DebugCreate));
				var guid = Marshal.GenerateGuidForType(typeof(IDebugClient5));
				var ret = func(ref guid, out _dbgEng);

				if (ret != 0)
					throw new InvalidOperationException();

				DebugClient = (IDebugClient5)_dbgEng;
				DebugControl = (IDebugControl)_dbgEng;
				DebugSymbols = (IDebugSymbols)_dbgEng;

				var evtCb = Marshal.GetComInterfaceForObject(this, typeof(IDebugEventCallbacks));
				DebugClient.SetEventCallbacks(evtCb);

				var outCb = Marshal.GetComInterfaceForObject(this, typeof(IDebugOutputCallbacks));
				DebugClient.SetOutputCallbacks(outCb);

				var filter = new[]
				{
					new DEBUG_EXCEPTION_FILTER_PARAMETERS
					{
						ExceptionCode = 0x80000001,
						ExecutionOption = DEBUG_FILTER_EXEC_OPTION.BREAK,
						ContinueOption = DEBUG_FILTER_CONTINUE_OPTION.GO_NOT_HANDLED,
					},
					new DEBUG_EXCEPTION_FILTER_PARAMETERS
					{
						ExceptionCode = 0xC000001D,
						ExecutionOption = DEBUG_FILTER_EXEC_OPTION.BREAK,
						ContinueOption = DEBUG_FILTER_CONTINUE_OPTION.GO_NOT_HANDLED,
					},
					new DEBUG_EXCEPTION_FILTER_PARAMETERS
					{
						ExceptionCode = 0xC0000005,
						ExecutionOption = DEBUG_FILTER_EXEC_OPTION.BREAK,
						ContinueOption = DEBUG_FILTER_CONTINUE_OPTION.GO_NOT_HANDLED,
					}
				};

				var hr = DebugControl.SetExceptionFilterParameters((uint)filter.Length, filter);
				if (hr != 0)
					Marshal.ThrowExceptionForHR(hr);
			}

			public void Dispose()
			{
				DebugClient = null;
				DebugControl = null;
				DebugSymbols = null;

				if (_dbgEng != null)
				{
					Marshal.FinalReleaseComObject(_dbgEng);
					_dbgEng = null;
				}

				if (_hDll != IntPtr.Zero)
				{
					FreeLibrary(_hDll);
					_hDll = IntPtr.Zero;
				}
			}

			public int Breakpoint(IDebugBreakpoint Bp)
			{
				Logger.Trace("Breakpoint: {0}", Bp);
				return 0;
			}

			public int ChangeDebuggeeState(DEBUG_CDS Flags, ulong Argument)
			{
				Logger.Trace("ChangeDebuggeeState: {0} {1}", Flags, Argument);
				return 0;
			}

			public int ChangeEngineState(DEBUG_CES Flags, ulong Argument)
			{
				Logger.Trace("ChangeEngineState: {0} {1}", Flags, Argument);
				return 0;
			}

			public int ChangeSymbolState(DEBUG_CSS Flags, ulong Argument)
			{
				Logger.Trace("ChangeSymbolState: {0} {1}", Flags, Argument);
				return 0;
			}

			public int CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
			{
				Logger.Trace("CreateProcess: {0} {1} {2} {3} {4} {5}", ImageFileHandle, Handle, BaseOffset, ModuleSize, ModuleName, ImageName);
				return 0;
			}

			public int CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
			{
				Logger.Trace("CreateThread: 0x{0:x8} 0x{1:x8}0x {2:x8}", Handle, DataOffset, StartOffset);
				return 0;
			}

			public unsafe int Exception(ref EXCEPTION_RECORD64 Exception, uint FirstChance)
			{
				Logger.Debug("Exception: 0x{0:x8}, FirstChance: {1}", Exception.ExceptionCode, FirstChance);

				bool handle;

				if (FirstChance == 1)
				{
					if (Exception.ExceptionCode == 0x80000003)
					{
						handle = !IgnoreBreakpoint;
					}
					else if (IgnoreFirstChanceGuardPage && Exception.ExceptionCode == 0x80000001)
					{
						handle = false;
					}
					else if (Exception.ExceptionCode == 0x80000001 || Exception.ExceptionCode == 0xC000001D)
					{
						// Guard page or illegal op
						handle = true;
					}

					else if (Exception.ExceptionCode == 0xC0000005)
					{
						// Access violation
						// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363082(v=vs.85).aspx

						fixed (ulong* ptr = Exception.ExceptionInformation)
						{
							// A/V on EIP
							if (ptr[0] == 0)
								handle = true;

							// write a/v
							else if (ptr[0] == 1 && ptr[1] != 0)
								handle = true;

							// DEP
							else if (ptr[0] == 8)
								handle = true;

							// Skip uninteresting A/V
							else
								handle = false;
						}
					}
					else
					{
						// Skip uninteresting first chance
						handle = false;
					}
				}
				else
				{
					if (IgnoreSecondChanceGuardPage && Exception.ExceptionCode == 0x80000001)
					{
						handle = false;
					}
					else
					{
						// All 2nd chance exceptions are interesting
						handle = true;
					}
				}

				if (handle)
				{
					// Don't recurse (does this really happen?)
					if (_handlingException)
						return (int)DEBUG_STATUS.NO_CHANGE;

					_handlingException = true;

					Logger.Debug("Fault detected, collecting info from windbg...");

					// 1. Output registers

					DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "r", DEBUG_EXECUTE.ECHO);
					DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "rF", DEBUG_EXECUTE.ECHO);
					DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "rX", DEBUG_EXECUTE.ECHO);
					DebugClient.FlushCallbacks();
					_output.Append("\n\n");

					// 2. Output stacktrace

					// Note: There is a known issue with dbgeng that can cause stack traces to take days due to issues in 
					// resolving symbols.  There is no known work arround.  We need the ability to skip a stacktrace
					// when this occurs.

					DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "kb", DEBUG_EXECUTE.ECHO);
					DebugClient.FlushCallbacks();
					_output.Append("\n\n");

					// 3. Dump File

					// Note: This can cause hangs on a bad day.  Don't think it's all that important, so skipping.

					// 4. !exploitable

					var path = IntPtr.Size == 4
						? "Debuggers\\DebugEngine\\msec86.dll"
						: "Debuggers\\DebugEngine\\msec64.dll";

					path = Path.Combine(Utilities.ExecutionDirectory, path);
					DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, ".load " + path, DEBUG_EXECUTE.ECHO);
					DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "!exploitable -m", DEBUG_EXECUTE.ECHO);
					_output.Append("\n\n");

					_output.Replace("\x0a", "\r\n");

					var output = _output.ToString();

					var fault = new MonitorData
					{
						Title = ReTitle.Match(output).Groups[1].Value,
						Fault = new MonitorData.Info
						{
							Description = output,
							MajorHash = ReMajorHash.Match(output).Groups[1].Value,
							MinorHash = ReMinorHash.Match(output).Groups[1].Value,
							Risk = ReRisk.Match(output).Groups[1].Value,
							MustStop = false,
						},
						Data = new Dictionary<string, Stream>()
					};

					Logger.Debug("Completed gathering windbg information");

					Fault = fault;
				}

				return (int)DEBUG_STATUS.NO_CHANGE;
			}

			public int ExitProcess(uint ExitCode)
			{
				Logger.Trace("ExitProcess: {0}", ExitCode);
				return 0;
			}

			public int ExitThread(uint ExitCode)
			{
				Logger.Trace("ExitThread: {0}", ExitCode);
				return 0;
			}

			public int GetInterestMask(out DEBUG_EVENT Mask)
			{
				Mask = DEBUG_EVENT.EXCEPTION |
					DEBUG_EVENT.SESSION_STATUS |
					DEBUG_EVENT.SYSTEM_ERROR |
					DEBUG_EVENT.CHANGE_DEBUGGEE_STATE |
					DEBUG_EVENT.CHANGE_ENGINE_STATE |
					DEBUG_EVENT.BREAKPOINT;
				return 0;
			}

			public int LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
			{
				Logger.Trace("LoadModule: {0} {1} {2} {3} {4}", ImageFileHandle, BaseOffset, ModuleSize, ModuleName, ImageName);
				return 0;
			}

			public int SessionStatus(DEBUG_SESSION Status)
			{
				Logger.Trace("SessionStatus: {0}", Status);

				if (Status == DEBUG_SESSION.ACTIVE && OnConnected != null)
					OnConnected();

				return 0;
			}

			public int SystemError(uint Error, uint Level)
			{
				Logger.Trace("SystemError: {0} {1}", Error, Level);
				return 0;
			}

			public int UnloadModule(string ImageBaseName, ulong BaseOffset)
			{
				Logger.Trace("UnloadModule: {0} {1}", ImageBaseName, BaseOffset);
				return 0;
			}

			public int Output(DEBUG_OUTPUT Mask, string Text)
			{
				_output.Append(Text);
				return 0;
			}
		}

		#endregion

		bool _connected;
		bool _stop;

		WinDbg _winDbg;
		Thread _thread;
		Exception _lastError;

		public string KernelConnectionString { get; private set; }
		public MonitorData Fault { get; private set; }

		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }
		public bool IgnoreFirstChanceGuardPage { get; set; }
		public bool IgnoreSecondChanceGuardPage { get; set; }

		public void Dispose()
		{
			if (_thread != null)
			{
				lock (_thread)
				{
					_stop = true;

					if (_winDbg != null)
					{
						if (_connected)
						{
							// Stops active debugger connections
							_winDbg.IgnoreBreakpoint = true;
							_winDbg.DebugControl.SetInterrupt(DEBUG_INTERRUPT.ACTIVE);
						}
						else
						{
							// Doesn't seem to be possible to stop the debugger
							// when it is waiting for a connection
							throw new InvalidOperationException("Can't dispose debugger when it is not connected.");
						}
					}
				}

				_thread.Join();
				_thread = null;
			}
		}

		public void AcceptKernel(string kernelConnectionString)
		{
			if (_thread != null)
				throw new NotSupportedException("Kernel debugger is already running.");

			_lastError = null;

			KernelConnectionString = kernelConnectionString;

			_thread = new Thread(DebugLoop);

			lock (_thread)
			{
				_thread.Start();

				Monitor.Wait(_thread);

				if (_lastError != null)
					throw new PeachException(_lastError.Message, _lastError);
			}
		}

		public void WaitForConnection(uint timeout)
		{
			if (_thread == null)
				throw new NotSupportedException("Kernel debugger is not running.");

			lock (_thread)
			{
				if (!_connected && !Monitor.Wait(_thread, TimeSpan.FromMilliseconds(timeout)))
					throw new TimeoutException("Kenel connection timed out.");

				if (_connected)
					return;

				Debug.Assert(_lastError != null);
				throw new PeachException(_lastError.Message, _lastError);
			}
		}

		private void DebugLoop()
		{
			try
			{
				using (var dbg = new WinDbg(Path.Combine(WinDbgPath, "dbgeng.dll")))
				{
					dbg.OnConnected = () =>
					{
						lock (_thread)
						{
							Logger.Debug("Kernel connection established");

							_connected = true;
							Monitor.Pulse(_thread);
						}
					};

					dbg.DebugControl.SetInterruptTimeout(1);
					dbg.DebugSymbols.SetSymbolPath(SymbolsPath);

					dbg.IgnoreFirstChanceGuardPage = IgnoreFirstChanceGuardPage;
					dbg.IgnoreSecondChanceGuardPage = IgnoreSecondChanceGuardPage;

					Logger.Debug("Starting kernel debugger");

					var hr = dbg.DebugClient.AttachKernel(DEBUG_ATTACH.KERNEL_CONNECTION, KernelConnectionString);
					if (hr != 0)
						Marshal.ThrowExceptionForHR(hr);

					// Signal that we are ready to accept kernel connections

					lock (_thread)
					{
						Logger.Debug("Waiting for kernel connection");

						_winDbg = dbg;
						Monitor.Pulse(_thread);
					}

					while (!_stop && dbg.Fault == null)
					{
						hr = dbg.DebugControl.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
						Logger.Trace("WaitForEvent: {0}", hr);
					}

					lock (_thread)
					{
						_winDbg = null;
						Fault = dbg.Fault;
					}

					dbg.DebugClient.EndSession(DEBUG_END.PASSIVE);

					Logger.Debug("Kernel connection ended gracefully");
				}
			}
			catch (Exception ex)
			{
				lock (_thread)
				{
					_winDbg = null;
					_lastError = ex;
					Monitor.Pulse(_thread);
				}
			}
		}
	}
}