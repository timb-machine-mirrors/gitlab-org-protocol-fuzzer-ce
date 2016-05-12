using System;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public interface IDebuggerInstance : IDisposable
	{
		string Name { get; }
		int ProcessId { get; }
		bool IsRunning { get; }

		void AttachProcess(string processName);
		void StartProcess(string commandLine);
		void StartService(string serviceName, TimeSpan startTimeout);

		void Stop();

		bool DetectedFault { get; }
		MonitorData Fault { get; }
	}
}
