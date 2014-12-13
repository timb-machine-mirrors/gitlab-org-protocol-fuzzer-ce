using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct _IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }
}

