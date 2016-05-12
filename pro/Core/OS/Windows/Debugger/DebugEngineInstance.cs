using System;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class DebugEngineInstance : SystemDebuggerInstance
	{
		public const string DefaultSymbolPath = "SRV*http://msdl.microsoft.com/download/symbols";

		public string WinDbgPath { get; set; }
		public string SymbolsPath { get; set; }

		public override string Name
		{
			get { return "WindowsDebugEngine"; }
		}

		protected override IDebugger OnStartProcess(string commandLine)
		{
			return DebugEngine.CreateProcess(WinDbgPath, SymbolsPath, commandLine);
		}

		protected override IDebugger OnAttachProcess(int pid)
		{
			return DebugEngine.AttachToProcess(WinDbgPath, SymbolsPath, pid);
		}
	}

	public class DebugEngineProxy : MarshalByRefObject, IDebuggerInstance
	{
		private readonly DebugEngineInstance instance = new DebugEngineInstance();

		public string WinDbgPath
		{
			get { return instance.WinDbgPath; }
			set { instance.WinDbgPath = value; }
		}

		public string SymbolsPath
		{
			get { return instance.SymbolsPath; }
			set { instance.SymbolsPath = value; }
		}

		public void Dispose()
		{
			instance.Dispose();
		}

		public string Name
		{
			get { return instance.Name; }
		}

		public int ProcessId
		{
			get { return instance.ProcessId; }
		}

		public bool IsRunning
		{
			get { return instance.IsRunning; }
		}

		public void AttachProcess(string processName)
		{
			instance.AttachProcess(processName);
		}

		public void StartProcess(string commandLine)
		{
			instance.StartProcess(commandLine);
		}

		public void StartService(string serviceName, TimeSpan startTimeout)
		{
			instance.StartService(serviceName, startTimeout);
		}

		public void Stop()
		{
			instance.Stop();
		}

		public bool DetectedFault
		{
			get { return instance.DetectedFault; }
		}

		public MonitorData Fault
		{
			get { return instance.Fault; }
		}
	}
}
