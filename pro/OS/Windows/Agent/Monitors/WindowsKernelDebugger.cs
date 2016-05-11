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
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.OS.Windows;
using Peach.Pro.OS.Windows.Debuggers.WindowsSystem;
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
	[Parameter("RestartAfterFault", typeof(bool), "Restart process after any fault occurs", "false")]
	[Parameter("ConnectTimeout", typeof(uint), "How long to wait for kernel connection.", "3000")]
	public class WindowsKernelDebugger : Monitor2
	{
		public string KernelConnectionString { get; set; }
		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }
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
	}

	public class KernelDebugger : IKernelDebugger
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		Remotable<DebuggerServer> _process;
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

		public void Dispose()
		{
			_dbg = null;

			// No way to gracefully stop kernel debugger so just kill process
			if (_process != null)
			{
				_process.Dispose();
				_process = null;
			}
		}

		public void AcceptKernel(string kernelConnectionString)
		{
			if (_process != null)
				throw new NotSupportedException("Kernel debugger is already running.");

			_process = new Remotable<DebuggerServer>();

			Logger.Debug("Creating KernelDebuggerInstance from {0}", _process.Url);

			try
			{
				var remote = _process.GetObject();

				var logLevel = Logger.IsTraceEnabled ? 2 : (Logger.IsDebugEnabled ? 1 : 0);

				_dbg = remote.GetKernelDebugger(logLevel);
				_dbg.SymbolsPath = SymbolsPath;
				_dbg.WinDbgPath = WinDbgPath;

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

	}

	public class KernelDebuggerInstance : MarshalByRefObject, IKernelDebugger
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		#region WinDbg Wrapper

		internal class WinDbg : IDebugEventCallbacks, IDebugOutputCallbacks, IWindowsDebugger
		{
			static readonly Regex ReMajorHash = new Regex(@"^MAJOR_HASH:(0x.*)\r$", RegexOptions.Multiline);
			static readonly Regex ReMinorHash = new Regex(@"^MINOR_HASH:(0x.*)\r$", RegexOptions.Multiline);
			static readonly Regex ReRisk = new Regex(@"^CLASSIFICATION:(.*)\r$", RegexOptions.Multiline);
			static readonly Regex ReTitle = new Regex(@"^SHORT_DESCRIPTION:(.*)\r$", RegexOptions.Multiline);

			const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
			const uint DEBUG_PROCESS = 0x00000001;

			[DllImport("kernel32.dll", SetLastError = true)]
			static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReserved, uint dwFlags);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool FreeLibrary(IntPtr hModule);

			[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
			static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

			[DllImport("kernel32.dll", SetLastError = true)]
			static extern int GetProcessId(uint hProcess);

			delegate uint DebugCreate(ref Guid InterfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object Interface);

			readonly StringBuilder _output = new StringBuilder();

			IntPtr _hDll;
			object _dbgEng;
			bool _handlingException;
			bool _processCreated;

			public Action OnConnected { private get; set; }

			public IDebugClient DebugClient { get; private set; }
			public IDebugControl DebugControl { get; private set; }
			public IDebugSymbols DebugSymbols { get; private set; }

			public MonitorData Fault { get; private set; }

			public bool Interrupt { private get; set; }

			public WinDbg(string winDbgPath)
			{
				try
				{
					Initialize(winDbgPath);
				}
				catch
				{
					Dispose();
					throw;
				}
			}

			private void Initialize(string winDbgPath)
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

			#region IDebugEventCallbacks

			int IDebugEventCallbacks.Breakpoint(IDebugBreakpoint Bp)
			{
				Logger.Trace("Breakpoint: {0}", Bp);
				return 0;
			}

			int IDebugEventCallbacks.ChangeDebuggeeState(DEBUG_CDS Flags, ulong Argument)
			{
				Logger.Trace("ChangeDebuggeeState: {0} {1}", Flags, Argument);
				return 0;
			}

			int IDebugEventCallbacks.ChangeEngineState(DEBUG_CES Flags, ulong Argument)
			{
				Logger.Trace("ChangeEngineState: {0} {1}", Flags, Argument);
				return 0;
			}

			int IDebugEventCallbacks.ChangeSymbolState(DEBUG_CSS Flags, ulong Argument)
			{
				Logger.Trace("ChangeSymbolState: {0} {1}", Flags, Argument);
				return 0;
			}

			int IDebugEventCallbacks.CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
			{
				Logger.Trace("CreateProcess: {0} {1} {2} {3} {4} {5}", ImageFileHandle, Handle, BaseOffset, ModuleSize, ModuleName, ImageName);

				if (!_processCreated)
				{
					_processCreated = true;

					if (ProcessCreated != null)
					{
						var pid = GetProcessId((uint)Handle);
						ProcessCreated(pid);
					}
				}

				return 0;
			}

			int IDebugEventCallbacks.CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
			{
				Logger.Trace("CreateThread: 0x{0:x8} 0x{1:x8}0x {2:x8}", Handle, DataOffset, StartOffset);
				return 0;
			}

			unsafe int IDebugEventCallbacks.Exception(ref EXCEPTION_RECORD64 Exception, uint FirstChance)
			{
				Logger.Debug("Exception: 0x{0:x8}, FirstChance: {1}", Exception.ExceptionCode, FirstChance);

				var ev = new ExceptionEvent
				{
					FirstChance = FirstChance,
					Code = Exception.ExceptionCode,
					Info = new long[2]
				};

				fixed (ulong* ptr = Exception.ExceptionInformation)
				{
					ev.Info[0] = (long)ptr[0];
					ev.Info[1] = (long)ptr[1];
				}

				OnException(ev);

				return (int)DEBUG_STATUS.NO_CHANGE;
			}

			int IDebugEventCallbacks.ExitProcess(uint ExitCode)
			{
				Logger.Trace("ExitProcess: {0}", ExitCode);
				return 0;
			}

			int IDebugEventCallbacks.ExitThread(uint ExitCode)
			{
				Logger.Trace("ExitThread: {0}", ExitCode);
				return 0;
			}

			int IDebugEventCallbacks.GetInterestMask(out DEBUG_EVENT Mask)
			{
				Mask = DEBUG_EVENT.EXCEPTION |
					DEBUG_EVENT.SESSION_STATUS |
					DEBUG_EVENT.SYSTEM_ERROR |
					DEBUG_EVENT.CHANGE_DEBUGGEE_STATE |
					DEBUG_EVENT.CHANGE_ENGINE_STATE |
					DEBUG_EVENT.BREAKPOINT |
					DEBUG_EVENT.CREATE_PROCESS |
					DEBUG_EVENT.EXIT_PROCESS
					;
				return 0;
			}

			int IDebugEventCallbacks.LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
			{
				Logger.Trace("LoadModule: {0} {1} {2} {3} {4}", ImageFileHandle, BaseOffset, ModuleSize, ModuleName, ImageName);
				return 0;
			}

			int IDebugEventCallbacks.SessionStatus(DEBUG_SESSION Status)
			{
				Logger.Trace("SessionStatus: {0}", Status);

				if (Status == DEBUG_SESSION.ACTIVE && OnConnected != null)
					OnConnected();

				return 0;
			}

			int IDebugEventCallbacks.SystemError(uint Error, uint Level)
			{
				Logger.Trace("SystemError: {0} {1}", Error, Level);
				return 0;
			}

			int IDebugEventCallbacks.UnloadModule(string ImageBaseName, ulong BaseOffset)
			{
				Logger.Trace("UnloadModule: {0} {1}", ImageBaseName, BaseOffset);
				return 0;
			}

			#endregion

			#region IDebugOutputCallbacks

			int IDebugOutputCallbacks.Output(DEBUG_OUTPUT Mask, string Text)
			{
				_output.Append(Text);
				return 0;
			}

			#endregion

			private void OnException(ExceptionEvent ev)
			{
				bool keepGoing;

				if (ev.Code == 0x80000003)
				{
					// We stop the debugger by triggering a first change breakpoint
					if (ev.FirstChance == 1 && Interrupt)
						return;

					// Kernel crashes come in as bugcheck breakpoints
					keepGoing = false;
				}
				else if (HandleAccessViolation != null)
				{
					// If handler is registered, ask whether to keep going
					keepGoing = HandleAccessViolation(ev);
				}
				else
				{
					// Default is to break on all non-first chance exceptions
					keepGoing = ev.FirstChance == 1;
				}

				if (keepGoing)
					return;

				// Don't recurse (does this really happen?)
				if (_handlingException)
					return;

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

			public Func<ExceptionEvent, bool> HandleAccessViolation { get; set; }
			public Action<int> ProcessCreated { get; set; }

			internal static WinDbg CreateProcess(string winDbgPath, string symbolsPath, string commandLine)
			{
				var dbg = new WinDbg(Path.Combine(winDbgPath, "dbgeng.dll"));

				dbg.DebugControl.SetInterruptTimeout(1);
				dbg.DebugSymbols.SetSymbolPath(symbolsPath);

				try
				{
					var hr = dbg.DebugClient.CreateProcessAndAttach(
						0,
						commandLine,
						(DEBUG_CREATE_PROCESS)DEBUG_PROCESS,
						0,
						DEBUG_ATTACH.DEFAULT);

					var ex = Marshal.GetExceptionForHR(hr);
					if (ex != null)
						throw ex;

					return dbg;
				}
				catch
				{
					dbg.Dispose();
					throw;
				}
			}

			internal static WinDbg AttachToProcess(string winDbgPath, string symbolsPath, int pid)
			{
				var dbg = new WinDbg(Path.Combine(winDbgPath, "dbgeng.dll"));

				dbg.DebugControl.SetInterruptTimeout(1);
				dbg.DebugSymbols.SetSymbolPath(symbolsPath);

				try
				{
					var hr = dbg.DebugClient.AttachProcess(
						0,
						(uint)pid,
						DEBUG_ATTACH.DEFAULT);

					var ex = Marshal.GetExceptionForHR(hr);
					if (ex != null)
						throw ex;

					return dbg;
				}
				catch
				{
					dbg.Dispose();
					throw;
				}
			}

			public void MainLoop()
			{
				while (Fault == null && !Interrupt)
				{
					var hr = DebugControl.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);

					Logger.Trace("WaitForEvent: 0x{0:x8}", hr);

					// E_UNEXPECTED means ran to completion
					if ((uint)hr == 0x8000ffff)
						break;

					var ex = Marshal.GetExceptionForHR(hr);
					if (ex != null)
						throw ex;
				}

				DebugClient.EndSession(DEBUG_END.PASSIVE);

				Logger.Debug("Debugger ended gracefully");
			}

			public void TerminateProcess()
			{
				// Cause the debugger to break by generating a synthetic exception
				Interrupt = true;
				DebugControl.SetInterrupt(DEBUG_INTERRUPT.ACTIVE);
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
							_winDbg.Interrupt = true;
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