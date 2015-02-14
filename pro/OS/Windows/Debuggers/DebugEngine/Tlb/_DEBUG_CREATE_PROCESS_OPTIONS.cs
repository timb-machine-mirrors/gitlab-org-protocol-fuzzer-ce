using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct _DEBUG_CREATE_PROCESS_OPTIONS
    {
        public uint CreateFlags;
        public uint EngCreateFlags;
        public uint VerifierFlags;
        public uint Reserved;
    }
}

