using Kaleidoscope.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kaleidoscope;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate double KaleidoscopeDelegate();

public unsafe class OrcJitEngine : IDisposable
{
    private readonly List<Delegate> _nativeCallbacks = [];
    private LLVMOrcOpaqueLLJIT* _jit;
    private LLVMOrcOpaqueThreadSafeContext* _threadSafeContext;
    private readonly LLVMOrcOpaqueJITDylib* _mainJd;
    private LLVMOpaqueTargetMachine* _targetMachine;
    private LLVMOpaquePassBuilderOptions* _passBuilderOptions;
    private readonly string _triple;
    private readonly string _dataLayout;

    /// <summary>
    /// The LLVM context shared with the IR emitter. Owned by the thread-safe context.
    /// </summary>
    public LLVMContextRef Context { get; }

    public OrcJitEngine()
    {
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();
        LLVM.InitializeNativeAsmParser();

        try
        {
            var triplePtr = LLVM.GetDefaultTargetTriple();
            var cpu = LLVM.GetHostCPUName();
            var features = LLVM.GetHostCPUFeatures();
            try
            {
                var jitTargetMachine = CreateTargetMachine(triplePtr, cpu, features);
                var builder = LLVM.OrcCreateLLJITBuilder();
                var jitTargetMachineBuilder = LLVM.OrcJITTargetMachineBuilderCreateFromTargetMachine(jitTargetMachine);
                LLVM.OrcLLJITBuilderSetJITTargetMachineBuilder(builder, jitTargetMachineBuilder);

                LLVMOrcOpaqueLLJIT* jit;
                var error = LLVM.OrcCreateLLJIT(&jit, builder);
                _jit = jit;
                if (error != null)
                    throw new InvalidOperationException($"Failed to initialize OrcLLJIT: {Helpers.ToString(error)}");

                _targetMachine = CreateTargetMachine(triplePtr, cpu, features);
            }
            finally
            {
                LLVM.DisposeMessage(triplePtr);
                LLVM.DisposeMessage(cpu);
                LLVM.DisposeMessage(features);
            }

            _mainJd = LLVM.OrcLLJITGetMainJITDylib(_jit);
            _threadSafeContext = LLVM.OrcCreateNewThreadSafeContext();
            Context = LLVM.OrcThreadSafeContextGetContext(_threadSafeContext);
            _triple = FromCString(LLVM.OrcLLJITGetTripleString(_jit));
            _dataLayout = FromCString(LLVM.OrcLLJITGetDataLayoutStr(_jit));
            _passBuilderOptions = LLVM.CreatePassBuilderOptions();

            DefineBuiltin(Builtins.PutChar, new NativeUnary(PutChard));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void AddModule(LLVMModuleRef module)
    {
        try
        {
            module.Target = _triple;
            module.DataLayout = _dataLayout;
            if (!module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out var message))
                throw new InvalidOperationException($"Invalid LLVM IR: {message}");

            Optimize(module);
        }
        catch
        {
            module.Dispose();
            throw;
        }

        // Ownership of the module moves to the JIT whether or not adding it succeeds.
        var threadSafeModule = LLVM.OrcCreateNewThreadSafeModule(module, _threadSafeContext);
        var error = LLVM.OrcLLJITAddLLVMIRModule(_jit, _mainJd, threadSafeModule);
        if (error != null)
            throw new InvalidOperationException($"Failed to add module to JIT: {Helpers.ToString(error)}");
    }

    public KaleidoscopeDelegate GetFunctionDelegate(string name)
    {
        using var marshaledName = new MarshaledString(name);
        ulong address = 0;
        var error = LLVM.OrcLLJITLookup(_jit, &address, marshaledName);
        if (error != null)
            throw new InvalidOperationException($"Symbol '{name}' not found in JIT: {Helpers.ToString(error)}");

        return Marshal.GetDelegateForFunctionPointer<KaleidoscopeDelegate>((IntPtr)address);
    }

    public void Dispose()
    {
        if (_passBuilderOptions != null)
        {
            LLVM.DisposePassBuilderOptions(_passBuilderOptions);
            _passBuilderOptions = null;
        }

        if (_targetMachine != null)
        {
            LLVM.DisposeTargetMachine(_targetMachine);
            _targetMachine = null;
        }

        if (_jit != null)
        {
            var error = LLVM.OrcDisposeLLJIT(_jit);
            if (error != null)
            {
                var message = LLVM.GetErrorMessage(error);
                LLVM.DisposeErrorMessage(message);
            }

            _jit = null;
        }

        if (_threadSafeContext != null)
        {
            LLVM.OrcDisposeThreadSafeContext(_threadSafeContext);
            _threadSafeContext = null;
        }
    }

    private void Optimize(LLVMModuleRef module)
    {
        using var passes = new MarshaledString("mem2reg,instcombine,reassociate,gvn,simplifycfg");
        var error = LLVM.RunPasses(module, passes, _targetMachine, _passBuilderOptions);
        if (error != null)
            throw new InvalidOperationException($"Failed to optimize module: {Helpers.ToString(error)}");
    }

    private void DefineBuiltin(string name, Delegate callback)
    {
        // The JIT holds the raw function pointer, so the delegate must stay rooted.
        _nativeCallbacks.Add(callback);
        var pointer = Marshal.GetFunctionPointerForDelegate(callback);

        LLVMOrcOpaqueSymbolStringPoolEntry* entry;
        using (var marshaled = new MarshaledString(name))
            entry = LLVM.OrcLLJITMangleAndIntern(_jit, marshaled);

        try
        {
            var pair = new LLVMOrcCSymbolMapPair
            {
                Name = entry,
                Sym = new LLVMJITEvaluatedSymbol
                {
                    Address = (ulong)pointer,
                    Flags = new LLVMJITSymbolFlags
                    {
                        GenericFlags = (byte)(LLVMJITSymbolGenericFlags.LLVMJITSymbolGenericFlagsExported |
                                              LLVMJITSymbolGenericFlags.LLVMJITSymbolGenericFlagsCallable),
                        TargetFlags = 0
                    }
                }
            };

            var unit = LLVM.OrcAbsoluteSymbols(&pair, 1);
            var error = LLVM.OrcJITDylibDefine(_mainJd, unit);
            if (error != null)
                throw new InvalidOperationException($"Failed to define '{name}' in the JIT: {Helpers.ToString(error)}");
        }
        finally
        {
            LLVM.OrcReleaseSymbolStringPoolEntry(entry);
        }
    }

    private static LLVMOpaqueTargetMachine* CreateTargetMachine(sbyte* triple, sbyte* cpu, sbyte* features)
    {
        LLVMTarget* target = null;
        sbyte* error = null;
        if (LLVM.GetTargetFromTriple(triple, &target, &error) != 0)
        {
            var message = error == null ? "unknown error" : FromCString(error);
            if (error != null)
                LLVM.DisposeMessage(error);

            throw new InvalidOperationException($"Failed to resolve the host target: {message}");
        }

        return LLVM.CreateTargetMachine(
            target,
            triple,
            cpu,
            features,
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelLarge);
    }

    private static string FromCString(sbyte* value)
        => MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)value).AsString();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate double NativeUnary(double value);

    private double PutChard(double value)
    {
        Console.Write((char)value);
        return 0;
    }
}
