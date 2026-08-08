using Kaleidoscope.Shared;
using Kaleidoscope.Shared.AST;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kaleidoscope;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void Print(double d);

public unsafe class Interpreter
{
    private LLVMModuleRef _module;
    private LLVMBuilderRef _builder;
    private LLVMExecutionEngineRef _engine;
    private LLVMOpaquePassBuilderOptions* _passBuilderOptions;


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

    public Interpreter()
    {
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();
        LLVM.InitializeNativeAsmParser();
    }

    private void InitializeModule()
    {
        _module = LLVMModuleRef.CreateWithName("Kaleidoscope Module");
        _builder = _module.Context.CreateBuilder();
        _passBuilderOptions = LLVM.CreatePassBuilderOptions();

        _engine = _module.CreateMCJITCompiler();

        var ft = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, [LLVMTypeRef.Double]);
        var write = _module.AddFunction("putchard", ft);
        write.Linkage = LLVMLinkage.LLVMExternalLinkage;
        Delegate d = new Print(PutChard);
        var p = Marshal.GetFunctionPointerForDelegate(d);
        _engine.AddGlobalMapping(write, p);
    }

    // public void Run(List<Expression> exprs)
    // {
    //     // If we modify the module after we already executed some function with
    //     // _engine.RunFunction it will break, so for each run we instantiate the module again
    //     // any previous defined function will be emitted again in the current module
    //
    //     InitializeModule();
    //     var toRun = new List<LLVMValueRef>();
    //     foreach (var item in exprs)
    //     {
    //         _context = NamedValues.Empty;
    //         var v = Visit(item);
    //
    //         // Since we could have several expressions to be evaluated, we need to complete the emission of all
    //         // the code before running any of them, we keep track of what we need to run and then execute later in order
    //         if (item is FunctionExpression { Proto.Name: "anon_expr" })
    //         {
    //             toRun.Add(v);
    //         }
    //     }
    //
    //     var passes = new MarshaledString("mem2reg,instcombine,reassociate,gvn,simplifycfg");
    //     var passesError = LLVM.RunPasses(_module, passes, _engine.TargetMachine, _passBuilderOptions);
    //
    //     if (passesError != null)
    //     {
    //         sbyte* errorMessage = LLVM.GetErrorMessage(passesError);
    //         var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)errorMessage);
    //         Console.WriteLine(span.AsString());
    //         return;
    //     }
    //
    //     foreach (var v in toRun)
    //     {
    //         var res = _engine.RunFunction(v, Array.Empty<LLVMGenericValueRef>());
    //         var fres = LLVMTypeRef.Double.GenericValueToFloat(res);
    //         Console.WriteLine("> {0}", fres);
    //     }
    //
    //     LLVM.DisposePassBuilderOptions(_passBuilderOptions);
    //     _builder.Dispose();
    //     _module.Dispose();
    // }
}
