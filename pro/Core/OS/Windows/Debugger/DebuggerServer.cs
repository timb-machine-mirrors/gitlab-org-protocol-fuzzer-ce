using System;
using Peach.Core;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class DebuggerServer : MarshalByRefObject
	{
		public IKernelDebugger GetKernelDebugger(int logLevel)
		{
			Utilities.ConfigureLogging(logLevel);

			return new KernelDebuggerInstance();
		}

		public IDebuggerInstance GetProcessDebugger(int logLevel)
		{
			Utilities.ConfigureLogging(logLevel);

			return new DebugEngineProxy();
		}
	}
}
