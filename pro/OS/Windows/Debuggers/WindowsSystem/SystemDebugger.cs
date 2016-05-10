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
using System.Runtime.InteropServices;
using System.Text;
using NLog;
using Peach.Core;

namespace Peach.Pro.OS.Windows.Debuggers.WindowsSystem
{
	public interface IWindowsDebugger
	{
		Func<ExceptionEvent, bool> HandleAccessViolation { get; set; }
		Action<int> ProcessCreated { get; set; }

		void MainLoop();
		void TerminateProcess();
	}

	public class ExceptionEvent
	{
		public uint FirstChance { get; set; }
		public uint Code { get; set; }
		public long[] Info { get; set; }
	}

	/// <summary>
	/// A lightweight Windows debugger written using the 
	/// system debugger APIs.
	/// </summary>
	/// <remarks>
	/// This debugger does not support symbols or other usefull 
	/// things.  When a crash is located using the system debugger
	/// it should be reproduced using the Windows Debug Engine to
	/// gather more information.
	/// </remarks>
	public class SystemDebugger : IWindowsDebugger
	{
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		#region Constants
		// ReSharper disable UnusedMember.Local

		private const int ERROR_SEM_TIMEOUT = 0x00000079;

		private const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
		private const uint DEBUG_PROCESS = 0x00000001;
		private const uint INFINITE = 0;
		private const uint DBG_CONTINUE = 0x00010002;
		private const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;

		private const uint STATUS_GUARD_PAGE_VIOLATION = 0x80000001;
		private const uint STATUS_DATATYPE_MISALIGNMENT = 0x80000002;
		private const uint STATUS_BREAKPOINT = 0x80000003;
		private const uint STATUS_SINGLE_STEP = 0x80000004;
		private const uint STATUS_LONGJUMP = 0x80000026;
		private const uint STATUS_UNWIND_CONSOLIDATE = 0x80000029;
		private const uint STATUS_ACCESS_VIOLATION = 0xC0000005;
		private const uint STATUS_IN_PAGE_ERROR = 0xC0000006;
		private const uint STATUS_INVALID_HANDLE = 0xC0000008;
		private const uint STATUS_INVALID_PARAMETER = 0xC000000D;
		private const uint STATUS_NO_MEMORY = 0xC0000017;
		private const uint STATUS_ILLEGAL_INSTRUCTION = 0xC000001D;
		private const uint STATUS_NONCONTINUABLE_EXCEPTION = 0xC0000025;
		private const uint STATUS_INVALID_DISPOSITION = 0xC0000026;
		private const uint STATUS_ARRAY_BOUNDS_EXCEEDED = 0xC000008C;
		private const uint STATUS_FLOAT_DENORMAL_OPERAND = 0xC000008D;
		private const uint STATUS_FLOAT_DIVIDE_BY_ZERO = 0xC000008E;
		private const uint STATUS_FLOAT_INEXACT_RESULT = 0xC000008F;
		private const uint STATUS_FLOAT_INVALID_OPERATION = 0xC0000090;
		private const uint STATUS_FLOAT_OVERFLOW = 0xC0000091;
		private const uint STATUS_FLOAT_STACK_CHECK = 0xC0000092;
		private const uint STATUS_FLOAT_UNDERFLOW = 0xC0000093;
		private const uint STATUS_INTEGER_DIVIDE_BY_ZERO = 0xC0000094;
		private const uint STATUS_INTEGER_OVERFLOW = 0xC0000095;
		private const uint STATUS_PRIVILEGED_INSTRUCTION = 0xC0000096;
		private const uint STATUS_STACK_OVERFLOW = 0xC00000FD;
		private const uint STATUS_DLL_NOT_FOUND = 0xC0000135;
		private const uint STATUS_ORDINAL_NOT_FOUND = 0xC0000138;
		private const uint STATUS_ENTRYPOINT_NOT_FOUND = 0xC0000139;
		private const uint STATUS_CONTROL_C_EXIT = 0xC000013A;
		private const uint STATUS_DLL_INIT_FAILED = 0xC0000142;
		private const uint STATUS_FLOAT_MULTIPLE_FAULTS = 0xC00002B4;
		private const uint STATUS_FLOAT_MULTIPLE_TRAPS = 0xC00002B5;
		private const uint STATUS_REG_NAT_CONSUMPTION = 0xC00002C9;
		private const uint STATUS_STACK_BUFFER_OVERRUN = 0xC0000409;
		private const uint STATUS_INVALID_CRUNTIME_PARAMETER = 0xC0000417;
		private const uint STATUS_POSSIBLE_DEADLOCK = 0xC0000194;

		private const uint DBG_CONTROL_C = 0x40010005;
		private const uint EXCEPTION_ACCESS_VIOLATION = STATUS_ACCESS_VIOLATION;
		private const uint EXCEPTION_DATATYPE_MISALIGNMENT = STATUS_DATATYPE_MISALIGNMENT;
		private const uint EXCEPTION_BREAKPOINT = STATUS_BREAKPOINT;
		private const uint EXCEPTION_SINGLE_STEP = STATUS_SINGLE_STEP;
		private const uint EXCEPTION_ARRAY_BOUNDS_EXCEEDED = STATUS_ARRAY_BOUNDS_EXCEEDED;
		private const uint EXCEPTION_FLT_DENORMAL_OPERAND = STATUS_FLOAT_DENORMAL_OPERAND;
		private const uint EXCEPTION_FLT_DIVIDE_BY_ZERO = STATUS_FLOAT_DIVIDE_BY_ZERO;
		private const uint EXCEPTION_FLT_INEXACT_RESULT = STATUS_FLOAT_INEXACT_RESULT;
		private const uint EXCEPTION_FLT_INVALID_OPERATION = STATUS_FLOAT_INVALID_OPERATION;
		private const uint EXCEPTION_FLT_OVERFLOW = STATUS_FLOAT_OVERFLOW;
		private const uint EXCEPTION_FLT_STACK_CHECK = STATUS_FLOAT_STACK_CHECK;
		private const uint EXCEPTION_FLT_UNDERFLOW = STATUS_FLOAT_UNDERFLOW;
		private const uint EXCEPTION_INT_DIVIDE_BY_ZERO = STATUS_INTEGER_DIVIDE_BY_ZERO;
		private const uint EXCEPTION_INT_OVERFLOW = STATUS_INTEGER_OVERFLOW;
		private const uint EXCEPTION_PRIV_INSTRUCTION = STATUS_PRIVILEGED_INSTRUCTION;
		private const uint EXCEPTION_IN_PAGE_ERROR = STATUS_IN_PAGE_ERROR;
		private const uint EXCEPTION_ILLEGAL_INSTRUCTION = STATUS_ILLEGAL_INSTRUCTION;
		private const uint EXCEPTION_NONCONTINUABLE_EXCEPTION = STATUS_NONCONTINUABLE_EXCEPTION;
		private const uint EXCEPTION_STACK_OVERFLOW = STATUS_STACK_OVERFLOW;
		private const uint EXCEPTION_INVALID_DISPOSITION = STATUS_INVALID_DISPOSITION;
		private const uint EXCEPTION_GUARD_PAGE = STATUS_GUARD_PAGE_VIOLATION;
		private const uint EXCEPTION_INVALID_HANDLE = STATUS_INVALID_HANDLE;
		private const uint EXCEPTION_POSSIBLE_DEADLOCK = STATUS_POSSIBLE_DEADLOCK;

		// Win32 x86 Emulation Exceptions
		private const uint STATUS_WX86_UNSIMULATE = 0x4000001C;
		private const uint STATUS_WX86_CONTINUE = 0x4000001D;
		private const uint STATUS_WX86_SINGLE_STEP = 0x4000001E;
		private const uint STATUS_WX86_BREAKPOINT = 0x4000001F;
		private const uint STATUS_WX86_EXCEPTION_CONTINUE = 0x40000020;
		private const uint STATUS_WX86_EXCEPTION_LASTCHANCE = 0x40000021;
		private const uint STATUS_WX86_EXCEPTION_CHAIN = 0x40000022;

		// Exception code for a c++ exception
		// http://support.microsoft.com/kb/185294
		private const uint C_PLUS_PLUS_EXCEPTION = 0xE06D7363;

		// ReSharper restore UnusedMember.Local
		#endregion

		/// <summary>
		/// Callback to handle an A/V exception
		/// </summary>
		/// <returns>
		/// true to keep debugging, false to stop debugging
		/// </returns>
		public Func<ExceptionEvent, bool> HandleAccessViolation { get; set; }

		public Action<int> ProcessCreated { get; set; }

		public int ProcessId { get; private set; }

		private readonly List<IntPtr> processHandles = new List<IntPtr>();
		private readonly Dictionary<uint, IntPtr> openHandles = new Dictionary<uint, IntPtr>();
		private readonly object mutex = new object();

		private SystemDebugger(int dwProcessId)
		{
			ProcessId = dwProcessId;
		}

		public static SystemDebugger CreateProcess(string command)
		{
			// CreateProcess
			var si = new UnsafeMethods.STARTUPINFO();
			UnsafeMethods.PROCESS_INFORMATION pi;

			if (!UnsafeMethods.CreateProcess(
					null,          // lpApplicationName 
					command,       // lpCommandLine 
					0,             // lpProcessAttributes 
					0,             // lpThreadAttributes 
					false,         // bInheritHandles 
					DEBUG_PROCESS, // dwCreationFlags, DEBUG_PROCESS
					IntPtr.Zero,   // lpEnvironment 
					null,          // lpCurrentDirectory 
					ref si,        // lpStartupInfo 
					out pi))       // lpProcessInformation 
			{
				var ex = new Win32Exception(Marshal.GetLastWin32Error());
				throw new PeachException("System debugger could not start process '" + command + "'.  " + ex.Message, ex);
			}

			UnsafeMethods.CloseHandle(pi.hProcess);
			UnsafeMethods.CloseHandle(pi.hThread);
			UnsafeMethods.DebugSetProcessKillOnExit(true);

			return new SystemDebugger(pi.dwProcessId);
		}

		public static SystemDebugger AttachToProcess(int dwProcessId)
		{
			using (new Privilege(Privilege.SeDebugPrivilege))
			{
				// DebugActiveProcess
				if (!UnsafeMethods.DebugActiveProcess((uint)dwProcessId))
				{
					var ex = new Win32Exception(Marshal.GetLastWin32Error());
					throw new PeachException("System debugger could not attach to process id " + dwProcessId + ".  " + ex.Message, ex);
				}
			}

			UnsafeMethods.DebugSetProcessKillOnExit(true);

			return new SystemDebugger(dwProcessId);
		}

		public void MainLoop()
		{
			try
			{
				do
				{
					UnsafeMethods.DEBUG_EVENT debug_event;

					if (!UnsafeMethods.WaitForDebugEvent(out debug_event, 1000))
					{
						var err = new Win32Exception(Marshal.GetLastWin32Error());
						if (err.NativeErrorCode == ERROR_SEM_TIMEOUT)
							continue;

						var ex = new PeachException("Failed to wait for debug event.  " + err.Message, err);
						logger.Trace(ex.Message);
						throw ex;
					}

					var dwContinueStatus = ProcessDebugEvent(ref debug_event);

					for (; ; )
					{
						try
						{
							if (!UnsafeMethods.ContinueDebugEvent(debug_event.dwProcessId, debug_event.dwThreadId, dwContinueStatus))
							{
								var err = new Win32Exception(Marshal.GetLastWin32Error());
								var ex = new PeachException("Failed to continue debugging.  " + err.Message, err);
								logger.Trace(ex.Message);
								throw ex;
							}

							break;
						}
						catch (SEHException)
						{
							logger.Trace("SEH when continuing debugging. Trying again...");
						}
					}
				}
				while (openHandles.Count > 0);
			}
			finally
			{
				lock (mutex)
				{
					processHandles.Clear();
				}

				foreach (var kv in openHandles)
					UnsafeMethods.CloseHandle(kv.Value);

				openHandles.Clear();
			}
		}

		public void TerminateProcess()
		{
			lock (mutex)
			{
				for (int i = processHandles.Count - 1; i >= 0; --i)
				{
					var proc = processHandles[i];

					if (!UnsafeMethods.TerminateProcess(proc, 0))
					{
						var ex = new Win32Exception(Marshal.GetLastWin32Error());
						logger.Trace("Failed to stop process 0x{0:X}.  {1}", proc.ToInt32(), ex.Message);
					}
				}
			}
		}

		private uint ProcessDebugEvent(ref UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			uint dwContinueStatus = DBG_EXCEPTION_NOT_HANDLED;

			switch (DebugEv.dwDebugEventCode)
			{
				case UnsafeMethods.DebugEventType.EXCEPTION_DEBUG_EVENT:
					// Process the exception code. When handling 
					// exceptions, remember to set the continuation 
					// status parameter (dwContinueStatus). This value 
					// is used by the ContinueDebugEvent function. 
					logger.Trace("EXCEPTION_DEBUG_EVENT");
					dwContinueStatus = OnExceptionDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.CREATE_THREAD_DEBUG_EVENT:
					// As needed, examine or change the thread's registers 
					// with the GetThreadContext and SetThreadContext functions; 
					// and suspend and resume thread execution with the 
					// SuspendThread and ResumeThread functions. 

					logger.Trace("CREATE_THREAD_DEBUG_EVENT");
					dwContinueStatus = OnCreateThreadDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
					// As needed, examine or change the registers of the
					// process's initial thread with the GetThreadContext and
					// SetThreadContext functions; read from and write to the
					// process's virtual memory with the ReadProcessMemory and
					// WriteProcessMemory functions; and suspend and resume
					// thread execution with the SuspendThread and ResumeThread
					// functions. Be sure to close the handle to the process image
					// file with CloseHandle.

					logger.Trace("CREATE_PROCESS_DEBUG_EVENT");
					dwContinueStatus = OnCreateProcessDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.EXIT_THREAD_DEBUG_EVENT:
					// Display the thread's exit code. 

					logger.Trace("EXIT_THREAD_DEBUG_EVENT");
					dwContinueStatus = OnExitThreadDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
					// Display the process's exit code. 

					logger.Trace("EXIT_PROCESS_DEBUG_EVENT");
					dwContinueStatus = OnExitProcessDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.LOAD_DLL_DEBUG_EVENT:
					// Read the debugging information included in the newly 
					// loaded DLL. Be sure to close the handle to the loaded DLL 
					// with CloseHandle.

					logger.Trace("LOAD_DLL_DEBUG_EVENT");
					dwContinueStatus = OnLoadDllDebugEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
					// Display a message that the DLL has been unloaded. 

					logger.Trace("UNLOAD_DLL_DEBUG_EVENT");
					dwContinueStatus = OnUnloadDllDebugEvent();
					break;

				case UnsafeMethods.DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
					// Display the output debugging string. 

					logger.Trace("OUTPUT_DEBUG_STRING_EVENT");
					dwContinueStatus = OnOutputDebugStringEvent(DebugEv);
					break;

				case UnsafeMethods.DebugEventType.RIP_EVENT:
					logger.Trace("RIP_EVENT");
					dwContinueStatus = OnRipEvent();
					break;

				default:
					logger.Trace("UNKNOWN DEBUG EVENT: 0x" + DebugEv.dwDebugEventCode.ToString("X8"));
					break;
			}

			return dwContinueStatus;
		}

		private uint OnRipEvent()
		{
			return DBG_CONTINUE;
		}

		private uint OnOutputDebugStringEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			if (!logger.IsTraceEnabled)
				return DBG_CONTINUE;

			var hProc = openHandles[DebugEv.dwProcessId];
			var DebugString = DebugEv.u.DebugString;

			uint len = DebugString.nDebugStringLength;
			if (DebugString.fUnicode != 0)
				len *= 2;

			var lpBuf = Marshal.AllocHGlobal((int)len);

			try
			{
				// For some reason, need to call thru to a function to pass in a copy of
				// the IntPtr because ReadProcessMemory sets it to NULL.
				if (!ReadProcessMemory(hProc, DebugString.lpDebugStringData, lpBuf, ref len))
				{
					var ex = new Win32Exception(Marshal.GetLastWin32Error());
					logger.Trace("  Failed to read debug string.  {0}.", ex.Message);
				}
				else
				{
					var str = DebugString.fUnicode != 0
						? Marshal.PtrToStringUni(lpBuf, (int)len)
						: Marshal.PtrToStringAnsi(lpBuf, (int)len);

					logger.Trace("  {0}", str);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(lpBuf);
			}

			return DBG_CONTINUE;
		}

		private uint OnUnloadDllDebugEvent()
		{
			return DBG_CONTINUE;
		}

		private uint OnLoadDllDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var LoadDll = DebugEv.u.LoadDll;
			UnsafeMethods.CloseHandle(LoadDll.hFile);
			return DBG_CONTINUE;
		}

		private uint OnExitThreadDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			openHandles.Remove(DebugEv.dwThreadId);
			return DBG_CONTINUE;
		}

		private uint OnCreateProcessDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var CreateProcessInfo = DebugEv.u.CreateProcessInfo;
			UnsafeMethods.CloseHandle(CreateProcessInfo.hFile);

			lock (mutex)
			{
				processHandles.Add(CreateProcessInfo.hProcess);
			}

			openHandles.Add(DebugEv.dwProcessId, CreateProcessInfo.hProcess);
			openHandles.Add(DebugEv.dwThreadId, CreateProcessInfo.hThread);

			if (ProcessId == DebugEv.dwProcessId && ProcessCreated != null)
				ProcessCreated(ProcessId);

			return DBG_CONTINUE;
		}

		private uint OnCreateThreadDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var CreateThread = DebugEv.u.CreateThread;
			openHandles.Add(DebugEv.dwThreadId, CreateThread.hThread);
			return DBG_CONTINUE;
		}

		private uint OnExitProcessDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			lock (mutex)
			{
				processHandles.Remove(openHandles[DebugEv.dwProcessId]);
			}

			openHandles.Remove(DebugEv.dwProcessId);
			openHandles.Remove(DebugEv.dwThreadId);

			return DBG_CONTINUE;
		}

		private uint OnExceptionDebugEvent(UnsafeMethods.DEBUG_EVENT DebugEv)
		{
			var Exception = DebugEv.u.Exception;

			if (logger.IsTraceEnabled)
				logger.Trace("  Pid: {0}, Exception: {1}", DebugEv.dwProcessId, ExceptionToString(Exception));

			switch (Exception.ExceptionRecord.ExceptionCode)
			{
				case STATUS_WX86_BREAKPOINT:
				case EXCEPTION_BREAKPOINT:
					// From: http://stackoverflow.com/questions/3799294/im-having-problems-with-waitfordebugevent-exception-debug-event
					// If launch a process and expect to debug it using the Windows API calls,
					// you should know that Windows will send one EXCEPTION_BREAKPOINT (INT3)
					// when it first loads. You must DEBUG_CONTINUE this first breakpoint
					// exception... if you DBG_EXCEPTION_NOT_HANDLED you will get the popup
					// message box: The application failed to initialize properly (0x80000003).
					return DBG_CONTINUE;
			}

			var ev = new ExceptionEvent
			{
				FirstChance = DebugEv.u.Exception.dwFirstChance,
				Code = DebugEv.u.Exception.ExceptionRecord.ExceptionCode,
				Info = new[]
				{
					DebugEv.u.Exception.ExceptionRecord.ExceptionInformation[0].ToInt64(),
					DebugEv.u.Exception.ExceptionRecord.ExceptionInformation[1].ToInt64()
				}
			};

			// Only some first chance exceptions are interesting
			while (ev.FirstChance != 0)
			{
				// Guard page or illegal op
				if (ev.Code == 0x80000001 || ev.Code == 0xC000001D)
					break;

				// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363082(v=vs.85).aspx

				// Access violation
				if (ev.Code == 0xC0000005)
				{
					// A/V on EIP
					if (ev.Info[0] == 0)
						break;

					// write a/v not near null
					if (ev.Info[0] == 1 && ev.Info[1] != 0)
						break;

					// DEP
					if (ev.Info[0] == 8)
						break;
				}

				// Skip uninteresting first chance and keep going
				return DBG_EXCEPTION_NOT_HANDLED;
			}

			// If 1st chance, only stop if HandleAccessViolation returns false
			// If 2nd chance, always stop
			var stop = HandleAccessViolation != null && !HandleAccessViolation(ev);

			// Stop by calling TerminateProcess and continuing
			// so we get thread & process exit notifications
			if (stop || Exception.dwFirstChance == 0)
			{
				TerminateProcess();
				return DBG_CONTINUE;
			}

			return DBG_EXCEPTION_NOT_HANDLED;
		}

		private static bool ReadProcessMemory(IntPtr hProc, IntPtr lpBaseAddress, IntPtr lpBuffer, ref uint len)
		{
			var lenIn = len;
			uint lenOut;

			if (!UnsafeMethods.ReadProcessMemory(hProc, lpBaseAddress, lpBuffer, lenIn, out lenOut))
				return false;

			len = lenOut;
			return true;
		}

		private static string ExceptionToString(UnsafeMethods.EXCEPTION_DEBUG_INFO Exception)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(ExceptionCodeToString(Exception.ExceptionRecord.ExceptionCode));

			if (Exception.dwFirstChance != 0)
				sb.Append(", First Chance");

			if (Exception.ExceptionRecord.ExceptionCode != 0)
				sb.Append(", Not Continuable");

			return sb.ToString();
		}

		private static string ExceptionCodeToString(uint code)
		{
			switch (code)
			{
				case EXCEPTION_ACCESS_VIOLATION:
					return "EXCEPTION_ACCESS_VIOLATION";
				case EXCEPTION_BREAKPOINT:
					return "EXCEPTION_BREAKPOINT";
				case EXCEPTION_DATATYPE_MISALIGNMENT:
					return "EXCEPTION_DATATYPE_MISALIGNMENT";
				case EXCEPTION_SINGLE_STEP:
					return "EXCEPTION_SINGLE_STEP";
				case DBG_CONTROL_C:
					return "DBG_CONTROL_C";
				case EXCEPTION_ARRAY_BOUNDS_EXCEEDED:
					return "EXCEPTION_ARRAY_BOUNDS_EXCEEDED";
				case EXCEPTION_FLT_DENORMAL_OPERAND:
					return "EXCEPTION_FLT_DENORMAL_OPERAND";
				case EXCEPTION_FLT_DIVIDE_BY_ZERO:
					return "EXCEPTION_FLT_DIVIDE_BY_ZERO";
				case EXCEPTION_FLT_INEXACT_RESULT:
					return "EXCEPTION_FLT_INEXACT_RESULT";
				case EXCEPTION_FLT_INVALID_OPERATION:
					return "EXCEPTION_FLT_INVALID_OPERATION";
				case EXCEPTION_FLT_OVERFLOW:
					return "EXCEPTION_FLT_OVERFLOW";
				case EXCEPTION_FLT_STACK_CHECK:
					return "EXCEPTION_FLT_STACK_CHECK";
				case EXCEPTION_FLT_UNDERFLOW:
					return "EXCEPTION_FLT_UNDERFLOW";
				case EXCEPTION_INT_DIVIDE_BY_ZERO:
					return "EXCEPTION_INT_DIVIDE_BY_ZERO";
				case EXCEPTION_INT_OVERFLOW:
					return "EXCEPTION_INT_OVERFLOW";
				case EXCEPTION_PRIV_INSTRUCTION:
					return "EXCEPTION_PRIV_INSTRUCTION";
				case EXCEPTION_IN_PAGE_ERROR:
					return "EXCEPTION_IN_PAGE_ERROR";
				case EXCEPTION_ILLEGAL_INSTRUCTION:
					return "EXCEPTION_ILLEGAL_INSTRUCTION";
				case EXCEPTION_NONCONTINUABLE_EXCEPTION:
					return "EXCEPTION_NONCONTINUABLE_EXCEPTION";
				case EXCEPTION_STACK_OVERFLOW:
					return "EXCEPTION_STACK_OVERFLOW";
				case EXCEPTION_INVALID_DISPOSITION:
					return "EXCEPTION_INVALID_DISPOSITION";
				case EXCEPTION_GUARD_PAGE:
					return "EXCEPTION_GUARD_PAGE";
				case EXCEPTION_INVALID_HANDLE:
					return "EXCEPTION_INVALID_HANDLE";
				case EXCEPTION_POSSIBLE_DEADLOCK:
					return "EXCEPTION_POSSIBLE_DEADLOCK";
				case STATUS_WX86_UNSIMULATE:
					return "STATUS_WX86_UNSIMULATE";
				case STATUS_WX86_CONTINUE:
					return "STATUS_WX86_CONTINUE";
				case STATUS_WX86_SINGLE_STEP:
					return "STATUS_WX86_SINGLE_STEP";
				case STATUS_WX86_BREAKPOINT:
					return "STATUS_WX86_BREAKPOINT";
				case STATUS_WX86_EXCEPTION_CONTINUE:
					return "STATUS_WX86_EXCEPTION_CONTINUE";
				case STATUS_WX86_EXCEPTION_LASTCHANCE:
					return "STATUS_WX86_EXCEPTION_LASTCHANCE";
				case STATUS_WX86_EXCEPTION_CHAIN:
					return "STATUS_WX86_EXCEPTION_CHAIN";
				case C_PLUS_PLUS_EXCEPTION:
					return "C_PLUS_PLUS_EXCEPTION";
				default:
					return "UNKNOWN EXCEPTION: 0x" + code.ToString("X8");
			}
		}
	}
}

// end
