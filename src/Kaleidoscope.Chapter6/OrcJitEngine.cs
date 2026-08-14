using Kaleidoscope.Shared;
using System;
using System.Runtime.InteropServices;

namespace Kaleidoscope;

public delegate double KaleidoscopeDelegate();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void Print(double d);

public unsafe class OrcJitEngine : IDisposable
{
    private LLVMOrcOpaqueLLJIT* _jit;

    /// The ThreadSafeContext owned by the JIT engine.
    private readonly LLVMOrcOpaqueThreadSafeContext* _threadSafeContext;

    private readonly LLVMOrcOpaqueJITDylib* _mainJd;

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

        _mainJd = LLVM.OrcLLJITGetMainJITDylib(_jit);
        _threadSafeContext = LLVM.OrcCreateNewThreadSafeContext();
        Context = LLVM.OrcThreadSafeContextGetContext(_threadSafeContext);

        AddPutCharD();
    }

    private void AddPutCharD()
    {
        // Map the putchard function to the C# delegate
        Delegate d = new Print(PutChard);
        var p = Marshal.GetFunctionPointerForDelegate(d);

        LLVMOrcOpaqueSymbolStringPoolEntry* entry;
        using (var marshaled = new MarshaledString("putchard"))
        {
            entry = LLVM.OrcLLJITMangleAndIntern(_jit, marshaled);
        }

        LLVMOrcCSymbolMapPair pair;
        pair.Name = entry;
        pair.Sym.Address = (ulong)p;
        pair.Sym.Flags.GenericFlags = (byte)(LLVMJITSymbolGenericFlags.LLVMJITSymbolGenericFlagsExported |
                                             LLVMJITSymbolGenericFlags.LLVMJITSymbolGenericFlagsCallable);
        pair.Sym.Flags.TargetFlags = 0;

        LLVMOrcOpaqueMaterializationUnit* unit = LLVM.OrcAbsoluteSymbols(&pair, 1);
        var error = LLVM.OrcJITDylibDefine(_mainJd, unit);

        if (error != null)
        {
            var message = Helpers.ToString(error);
            throw new InvalidOperationException($"Failed to define putchard symbol in JIT: {message}");
        }
    }

    public void AddModule(LLVMModuleRef module)
    {
        var tsMod = LLVM.OrcCreateNewThreadSafeModule(module, _threadSafeContext);

        var error = LLVM.OrcLLJITAddLLVMIRModule(_jit, _mainJd, tsMod);
        if (error != null)
        {
            LLVM.OrcDisposeThreadSafeModule(tsMod);
            LLVM.OrcDisposeThreadSafeContext(_threadSafeContext);
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
            var message = Helpers.ToString(error);
            throw new InvalidOperationException($"Symbol '{name}' not found in JIT: {message}");
        }

        IntPtr funcPtr = (IntPtr)address;
        return Marshal.GetDelegateForFunctionPointer<KaleidoscopeDelegate>(funcPtr);
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

    private void PutChard(double x)
    {
        try
        {
            Console.Write((char)x);
        }
        catch
        {
        }
    }
}
