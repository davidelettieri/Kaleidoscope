using System.Runtime.InteropServices;

namespace Kaleidoscope.Shared;

public static unsafe class Helpers
{
    public static string ToString(LLVMOpaqueError* error)
    {
        var errorMessage = LLVM.GetErrorMessage(error);
        var message = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)errorMessage).AsString();
        LLVM.DisposeErrorMessage(errorMessage);
        return message;
    }
}
