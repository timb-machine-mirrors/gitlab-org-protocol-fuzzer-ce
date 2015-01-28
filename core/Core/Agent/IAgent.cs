using System.Collections.Generic;

namespace Peach.Core.Agent
{
	public interface IAgent
	{
		// Note: We can't remote Dictionary<> objects properly between
		// windows and linux using the BinaryFormatter.  For the IPC
		// channel use a List<> instead.
		void AgentConnect();
		void AgentDisconnect();
		void StartMonitor(string name, string cls, IEnumerable<KeyValuePair<string, Variant>> args);
		void StopAllMonitors();
		void SessionStarting();
		void SessionFinished();
		void IterationStarting(uint iterationCount, bool isReproduction);
		void IterationFinished();
		bool DetectedFault();
		Fault[] GetMonitorData();
		bool MustStop();
		void Message(string msg);
	}
}
