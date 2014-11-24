using System;
using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct _DEBUG_READ_USER_MINIDUMP_STREAM
    {
        public uint StreamType;
        public uint Flags;
        public ulong Offset;
        public IntPtr Buffer;
        public uint BufferSize;
        public uint BufferUsed;
    }
}

