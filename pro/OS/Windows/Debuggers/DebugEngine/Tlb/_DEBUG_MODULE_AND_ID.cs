using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct _DEBUG_MODULE_AND_ID
    {
        public ulong ModuleBase;
        public ulong Id;
    }
}

