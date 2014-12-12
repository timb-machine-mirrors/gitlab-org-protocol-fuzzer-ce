using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct _DEBUG_OFFSET_REGION
    {
        public ulong Base;
        public ulong Size;
    }
}

