using Kaleidoscope.Shared;
using System;
using System.Runtime.InteropServices;

namespace Kaleidoscope;

public delegate double KaleidoscopeDelegate();

public unsafe class OrcJitEngine : IDisposable
{
    private LLVMOrcOpaqueLLJIT* _jit;

    /// The ThreadSafeContext owned by the JIT engine.
    private readonly LLVMOrcOpaqueThreadSafeContext* _threadSafeContext;

    /// The underlying LLVMContextRef used by IRGenerator to emit modules.
    public LLVMContextRef Context { get; }

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
            var message = Helpers.ToString(error);
            throw new InvalidOperationException($"Failed to initialize OrcLLJIT: {message}");
        }

        _threadSafeContext = LLVM.OrcCreateNewThreadSafeContext();
        Context = LLVM.OrcThreadSafeContextGetContext(_threadSafeContext);
    }

    public void AddModule(LLVMModuleRef module)
    {
        var tsMod = LLVM.OrcCreateNewThreadSafeModule(module, _threadSafeContext);

        var mainDlib = LLVM.OrcLLJITGetMainJITDylib(_jit);
        var error = LLVM.OrcLLJITAddLLVMIRModule(_jit, mainDlib, tsMod);
        if (error != null)
        {
            throw new InvalidOperationException($"Failed to add module to JIT: {Helpers.ToString(error)}");
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
            var error = LLVM.OrcDisposeLLJIT(_jit);
            if (error != null)
            {
                var ptr = LLVM.GetErrorMessage(error);
                LLVM.DisposeErrorMessage(ptr);
            }
            _jit = null;
        }
        
        if (_threadSafeContext != null)
        {
            LLVM.OrcDisposeThreadSafeContext(_threadSafeContext);
        }
    }
}
