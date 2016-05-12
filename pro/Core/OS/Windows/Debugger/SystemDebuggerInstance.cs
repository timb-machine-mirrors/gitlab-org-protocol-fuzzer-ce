using System;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = System.Threading.Monitor;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class SystemDebuggerInstance : IDebuggerInstance
	{
		public bool IgnoreFirstChanceGuardPage { get; set; }
		public bool IgnoreSecondChanceGuardPage { get; set; }

		private readonly object _mutex = new object();

		private int _processId;
		private bool _detectedFault;
		private MonitorData _fault;
		private Exception _exception;
		private IDebugger _debugger;

		protected virtual IDebugger OnStartProcess(string commandLine)
		{
			return SystemDebugger.CreateProcess(commandLine);
		}

		protected virtual IDebugger OnAttachProcess(int pid)
		{
			return SystemDebugger.AttachToProcess(pid);
		}

		internal static int GetProcessPid(string processName)
		{
			var procs = SysProcess.GetProcessesByName(processName);
			if (procs.Length > 0)
			{
				foreach (var p in procs)
					p.Close();

				return procs[0].Id;
			}

			int pid;
			if (int.TryParse(processName, out pid))
				return pid;

			throw new PeachException("Unable to locate pid of process named \"" + processName + "\".");
		}

		internal static int GetServicePid(string serviceName, TimeSpan startTimeout)
		{
			using (var sc = new ServiceController(serviceName))
			{
				var wait = true;

				sc.Refresh();

				switch (sc.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						break;
					case ServiceControllerStatus.Paused:
						sc.Continue();
						break;
					case ServiceControllerStatus.PausePending:
						sc.WaitForStatus(ServiceControllerStatus.Paused, startTimeout);
						sc.Continue();
						break;
					case ServiceControllerStatus.Running:
						wait = false;
						break;
					case ServiceControllerStatus.StartPending:
						break;
					case ServiceControllerStatus.Stopped:
						sc.Start();
						break;
					case ServiceControllerStatus.StopPending:
						sc.WaitForStatus(ServiceControllerStatus.Stopped, startTimeout);
						sc.Start();
						break;
				}

				if (wait)
					sc.WaitForStatus(ServiceControllerStatus.Running, startTimeout);

				using (var mo = new ManagementObject(@"Win32_service.Name='" + sc.ServiceName + "'"))
				{
					var pid = mo.GetPropertyValue("ProcessId");
					return (int)(uint)pid;
				}
			}
		}

		public virtual string Name
		{
			get { return "SystemDebugger"; }
		}

		public int ProcessId
		{
			get
			{
				lock (_mutex)
				{
					return _processId;
				}
			}
		}

		public bool IsRunning
		{
			get
			{
				lock (_mutex)
				{
					return _debugger != null;
				}
			}
		}

		public bool DetectedFault
		{
			get
			{
				lock (_mutex)
				{
					return _detectedFault;
				}
			}
		}

		public MonitorData Fault
		{
			get
			{
				lock (_mutex)
				{
					if (!_detectedFault)
						return null;

					if (_fault == null)
						Monitor.Wait(_mutex);

					Debug.Assert(_fault != null);

					return _fault;
				}
			}
		}

		public void Dispose()
		{
		}

		public void Stop()
		{
			lock (_mutex)
			{
				if (_debugger == null)
					return;

				_debugger.Stop();
				Monitor.Wait(_mutex);
			}
		}

		public void StartProcess(string commandLine)
		{
			Start(() => OnStartProcess(commandLine));
		}

		public void AttachProcess(string processName)
		{
			var pid = GetProcessPid(processName);

			Start(() => OnAttachProcess(pid));
		}

		public void StartService(string serviceName, TimeSpan startTimeout)
		{
			var pid = GetServicePid(serviceName, startTimeout);

			Start(() => OnAttachProcess(pid));
		}

		private bool HandleAccessViolation(ExceptionEvent ev)
		{
			if (IgnoreFirstChanceGuardPage && ev.FirstChance != 0 && ev.Code == 0x80000001)
				return true;

			if (IgnoreSecondChanceGuardPage && ev.FirstChance == 0 && ev.Code == 0x80000001)
				return true;

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
				return true;
			}

			lock (_mutex)
			{
				_detectedFault = true;
			}

			return false;
		}

		private void Start(Func<IDebugger> createFn)
		{
			lock (_mutex)
			{
				if (_debugger != null)
					throw new InvalidOperationException("Can not start system debugger, it is alread running.");

				_processId = 0;
				_fault = null;
				_exception = null;

				var th = new Thread(() => Run(createFn));

				th.Start();

				Monitor.Wait(_mutex);

				if (_debugger != null)
					return;

				Debug.Assert(_exception != null);

				throw new PeachException(_exception.Message, _exception);
			}
		}

		private void Run(Func<IDebugger> createFn)
		{
			try
			{
				var dbg = createFn();

				dbg.HandleAccessViolation = HandleAccessViolation;

				dbg.ProcessCreated = pid =>
				{
					lock (_mutex)
					{
						_debugger = dbg;
						_processId = pid;
						Monitor.Pulse(_mutex);
					}
				};

				dbg.Run();
			}
			catch (Exception ex)
			{
				_exception = ex;
			}
			finally
			{
				lock (_mutex)
				{
					if (_debugger != null)
					{
						_fault = _debugger.Fault;
						_debugger.Dispose();
						_debugger = null;
					}

					Monitor.Pulse(_mutex);
				}
			}
		}
	}
}
