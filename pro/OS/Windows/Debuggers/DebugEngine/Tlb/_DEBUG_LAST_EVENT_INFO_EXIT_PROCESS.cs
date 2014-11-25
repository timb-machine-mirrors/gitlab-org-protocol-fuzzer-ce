using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct _DEBUG_LAST_EVENT_INFO_EXIT_PROCESS
    {
        public uint ExitCode;
    }
}

