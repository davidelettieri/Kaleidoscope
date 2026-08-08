using System;
using System.Runtime.InteropServices;

namespace Kaleidoscope;

public delegate double KaleidoscopeDelegate();

public unsafe class OrcJitEngine : IDisposable
{
    private LLVMOrcOpaqueLLJIT* _jit;


    public OrcJitEngine()
    {
        // 1. Initialize native code generation targets for the host CPU
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();
        LLVM.InitializeNativeAsmParser();

        // 2. Instantiate LLJIT Engine via LLVM C-API
        var builder = LLVM.OrcCreateLLJITBuilder();
        LLVMOrcOpaqueLLJIT* jit;
        var error = LLVM.OrcCreateLLJIT(&jit, builder);
        _jit = jit;

        if (error != null)
        {
            throw new InvalidOperationException($"Failed to initialize OrcLLJIT");
        }
    }

    public void AddModule(LLVMModuleRef module)
    {
        // Wrap module and context in thread-safe containers required by LLVM ORC
        var tsCtx = LLVM.OrcCreateNewThreadSafeContext();
        var tsMod = LLVM.OrcCreateNewThreadSafeModule(module, tsCtx);

        var mainDlib = LLVM.OrcLLJITGetMainJITDylib(_jit);
        var error = LLVM.OrcLLJITAddLLVMIRModule(_jit, mainDlib, tsMod);
        if (error != null)
        {
            throw new InvalidOperationException($"Failed to add module to JIT");
        }
    }

    public KaleidoscopeDelegate GetFunctionDelegate(string name)
    {
        using var marshaledName = new MarshaledString(name);
        ulong address = 0;
        var error = LLVM.OrcLLJITLookup(_jit, &address, marshaledName);
        if (error != null)
        {
            throw new InvalidOperationException($"Symbol '{name}' not found in JIT");
        }

        IntPtr funcPtr = (IntPtr)address;
        return Marshal.GetDelegateForFunctionPointer<KaleidoscopeDelegate>(funcPtr);
    }

    public double ExecuteAnonymousExpression(LLVMModuleRef module, string exprName = "__anon_expr")
    {
        AddModule(module);
        var anonFunc = GetFunctionDelegate(exprName);
        return anonFunc();
    }

    public void Dispose()
    {
        if (_jit != null)
        {
            LLVM.OrcDisposeLLJIT(_jit);
            _jit = null;
        }
    }
}
