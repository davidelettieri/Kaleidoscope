// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT). See License.md at https://github.com/dotnet/LLVMSharp?tab=MIT-1-ov-file for more information.

using LLVMSharp.Interop;
using System;

namespace Kaleidoscope;

/// <summary>
/// A minimal ORC "LLJIT" wrapper — the modern replacement for the removed <c>ExecutionEngine</c>/MCJIT
/// path. It creates an LLJIT instance, wires up a generator so JIT'd code can call symbols already in
/// the host process (libc <c>sin</c>/<c>cos</c>, …), and lets the driver add a module per top-level
/// item. Each top-level expression is added under its own resource tracker so it can be removed after
/// it runs, which is what lets the REPL evaluate more than one expression (upstream issue #1).
/// </summary>
public sealed unsafe class KaleidoscopeJit : IDisposable
{
    private LLVMOrcOpaqueLLJIT* _jit;
    private readonly LLVMOrcOpaqueJITDylib* _mainJD;

    public KaleidoscopeJit()
    {
        LLVMOrcOpaqueLLJIT* jit;
        LLVM.OrcCreateLLJIT(&jit, null);
        _jit = jit;

        _mainJD = LLVM.OrcLLJITGetMainJITDylib(jit);

        // Resolve symbols already present in the host process (e.g. libc math functions).
        LLVMOrcOpaqueDefinitionGenerator* generator;
        LLVM.OrcCreateDynamicLibrarySearchGeneratorForProcess(&generator, LLVM.OrcLLJITGetGlobalPrefix(jit), null,
            null);
        LLVM.OrcJITDylibAddGenerator(_mainJD, generator);
    }

    /// <summary>The data layout string of the JIT, so emitted modules can match it.</summary>
    public string DataLayout => new(LLVM.OrcLLJITGetDataLayoutStr(_jit));

    /// <summary>The target triple of the JIT.</summary>
    public string Triple => new(LLVM.OrcLLJITGetTripleString(_jit));

    /// <summary>Stamps a module with the JIT's data layout and triple before it is added.</summary>
    public void ConfigureModule(LLVMModuleRef module)
    {
        using var dataLayout = new MarshaledString(DataLayout);
        using var triple = new MarshaledString(Triple);
        LLVM.SetDataLayout(module, dataLayout);
        LLVM.SetTarget(module, triple);
    }

    /// <summary>Adds a module permanently (used for <c>def</c> so the function stays callable).</summary>
    public void AddModule(LLVMModuleRef module)
    {
        LLVMOrcOpaqueThreadSafeModule* threadSafeModule = WrapModule(module);
        LLVM.OrcLLJITAddLLVMIRModule(_jit, _mainJD, threadSafeModule);
    }

    /// <summary>
    /// Adds a module under a fresh resource tracker and returns that tracker. Used for the anonymous
    /// top-level expression so the caller can <see cref="RemoveModule"/> it after evaluating, freeing
    /// the <c>__anon_expr</c> name for the next expression.
    /// </summary>
    public nint AddModuleRemovable(LLVMModuleRef module)
    {
        LLVMOrcOpaqueResourceTracker* tracker = LLVM.OrcJITDylibCreateResourceTracker(_mainJD);
        LLVMOrcOpaqueThreadSafeModule* threadSafeModule = WrapModule(module);
        LLVM.OrcLLJITAddLLVMIRModuleWithRT(_jit, tracker, threadSafeModule);
        return (nint)tracker;
    }

    /// <summary>Removes a module previously added via <see cref="AddModuleRemovable"/>.</summary>
    public void RemoveModule(nint tracker)
    {
        var resourceTracker = (LLVMOrcOpaqueResourceTracker*)tracker;
        LLVM.OrcResourceTrackerRemove(resourceTracker);
        LLVM.OrcReleaseResourceTracker(resourceTracker);
    }

    /// <summary>Looks up a symbol's address in the JIT.</summary>
    public ulong Lookup(string name)
    {
        ulong address;
        using var marshaled = new MarshaledString(name);
        LLVM.OrcLLJITLookup(_jit, &address, marshaled);
        return address;
    }

    /// <summary>
    /// Defines an absolute symbol pointing at a host function address. This is how the tutorial exposes
    /// its own <c>putchard</c>/<c>printd</c> helpers to JIT'd Kaleidoscope code (issues #69 and #133).
    /// </summary>
    public void DefineSymbol(string name, nint address)
    {
        LLVMOrcOpaqueSymbolStringPoolEntry* entry;
        using (var marshaled = new MarshaledString(name))
        {
            entry = LLVM.OrcLLJITMangleAndIntern(_jit, marshaled);
        }

        LLVMOrcCSymbolMapPair pair;
        pair.Name = entry;
        pair.Sym.Address = (ulong)address;
        pair.Sym.Flags.GenericFlags = (byte)(LLVMJITSymbolGenericFlags.LLVMJITSymbolGenericFlagsExported |
                                             LLVMJITSymbolGenericFlags.LLVMJITSymbolGenericFlagsCallable);
        pair.Sym.Flags.TargetFlags = 0;

        LLVMOrcOpaqueMaterializationUnit* unit = LLVM.OrcAbsoluteSymbols(&pair, 1);
        LLVM.OrcJITDylibDefine(_mainJD, unit);
    }

    private static LLVMOrcOpaqueThreadSafeModule* WrapModule(LLVMModuleRef module)
    {
        // Wrap the module (built in its own LLVMContext) in a thread-safe module the JIT can take. The
        // LLVM 21 API takes ownership of the context we pass, so we dispose our handle afterward.
        LLVMOrcOpaqueThreadSafeContext* threadSafeContext =
            LLVM.OrcCreateNewThreadSafeContext();
        LLVMOrcOpaqueThreadSafeModule* threadSafeModule = LLVM.OrcCreateNewThreadSafeModule(module, threadSafeContext);
        LLVM.OrcDisposeThreadSafeContext(threadSafeContext);
        return threadSafeModule;
    }

    public void Dispose()
    {
        if (_jit is not null)
        {
            _ = LLVM.OrcDisposeLLJIT(_jit);
            _jit = null;
        }
    }
}
