using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Peach.Pro.OS.Windows.Debuggers.DebugEngine.Tlb
{
	[ComImport, Guid("9F50E42C-F136-499E-9A97-73036C94ED2D"), InterfaceType((short) 1)]
    public interface IDebugInputCallbacks
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void StartInput([In] uint BufferSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void EndInput();
    }
}

