using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Kaleidoscope.AST;
using LLVMSharp;
using static Kaleidoscope.AST.ExprType;

namespace Kaleidoscope
{
    public class Context
    {
        private readonly ImmutableDictionary<string, LLVMValueRef> _source;

        public Context()
        {
            _source = ImmutableDictionary<string, LLVMValueRef>.Empty;
        }

        private Context(ImmutableDictionary<string, LLVMValueRef> source)
        {
            _source = source;
        }

        public Context Add(string key, LLVMValueRef value)
            => new Context(_source.Remove(key).Add(key, value));

        public Context AddArguments(LLVMValueRef function, List<string> arguments)
        {
            var s = _source;

            for (int i = 0; i < arguments.Count; i++)
            {
                var name = arguments[i];
                var param = LLVM.GetParam(function, (uint)i);
                LLVM.SetValueName(param, name);
                s = s.Add(name, param);
            }

            return new Context(s);
        }

        public LLVMValueRef? Get(string key)
        {
            if (_source.TryGetValue(key, out var value))
                return value;

            return null;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate double Print(double d);

    public class IREmitter : ExprVisitor<(Context, LLVMValueRef), Context>
    {
        private readonly LLVMModuleRef _module;
        private readonly LLVMBuilderRef _builder;
        private readonly LLVMPassManagerRef _passManager;
        private readonly LLVMExecutionEngineRef _engine;

        private double Printd(double x)
        {
            try
            {
                Console.WriteLine(x);
                return 0.0F;
            }
            catch
            {
                return 0.0;
            }
        }

        public IREmitter()
        {
            _module = LLVM.ModuleCreateWithName("Kaleidoscope Module");
            _builder = LLVM.CreateBuilder();
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
            _passManager = LLVM.CreateFunctionPassManagerForModule(_module);
            LLVM.AddBasicAliasAnalysisPass(_passManager);
            LLVM.AddPromoteMemoryToRegisterPass(_passManager);
            LLVM.AddInstructionCombiningPass(_passManager);
            LLVM.AddReassociatePass(_passManager);
            LLVM.AddGVNPass(_passManager);
            LLVM.AddCFGSimplificationPass(_passManager);
            LLVM.InitializeFunctionPassManager(_passManager);
             LLVM.CreateExecutionEngineForModule(out _engine, _module, out var error);
            // var options = new LLVMMCJITCompilerOptions { NoFramePointerElim = 1 };
            // LLVM.InitializeMCJITCompilerOptions(options);
            // LLVM.CreateMCJITCompilerForModule(out _engine, _module, options, out var error);
            // if (!string.IsNullOrWhiteSpace(error))
            //     Console.WriteLine($"Error: {error}");

            var ft = LLVM.FunctionType(LLVM.DoubleType(), new[] { LLVM.DoubleType() }, false);
            var write = LLVM.AddFunction(_module, "write", ft);
            LLVM.SetLinkage(write, LLVMLinkage.LLVMExternalLinkage);
            Delegate d = new Print(Printd);
            var p = Marshal.GetFunctionPointerForDelegate(d);
            LLVM.AddGlobalMapping(_engine, write, p);
            if (!string.IsNullOrWhiteSpace(error))
                throw new Exception(error);
            LLVM.DumpModule(_module);
        }

        public void Intepret(List<ExprAST> exprs)
        {
            var ctx = new Context();
            foreach (var item in exprs)
            {
                var (ctxn, v) = Visit(ctx, item);
                LLVM.DumpValue(v);
                // LLVM.DumpModule(_module);
                Console.WriteLine();
                if (item is FunctionAST f && string.IsNullOrWhiteSpace(f.Proto.Name))
                {
                    var res = LLVM.RunFunction(_engine, v, Array.Empty<LLVMGenericValueRef>());
                    var fres = LLVM.GenericValueToFloat(LLVM.DoubleType(), res);
                    Console.WriteLine("> {0}", fres);
                }
                ctx = ctxn;
            }
        }

        private (Context, LLVMValueRef) Visit(Context ctx, ExprAST body)
        {
            return body.Accept(this, ctx);
        }

        private LLVMValueRef BinaryVal(LLVMValueRef lhs_val, LLVMValueRef rhs_val, ExprType nodeType)
        {
            switch (nodeType)
            {
                case AddExpr:
                    return LLVM.BuildFAdd(_builder, lhs_val, rhs_val, "addtmp");
                case SubtractExpr:
                    return LLVM.BuildFSub(_builder, lhs_val, rhs_val, "addtmp");
                case MultiplyExpr:
                    return LLVM.BuildFMul(_builder, lhs_val, rhs_val, "addtmp");
                case LessThanExpr:
                    var i = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealOLT, lhs_val, rhs_val, "cmptmp");
                    return LLVM.BuildUIToFP(_builder, i, LLVM.DoubleType(), "booltmp");
                default:
                    throw new InvalidOperationException();
            }
        }

        public (Context, LLVMValueRef) VisitBinaryExprAST(Context ctx, BinaryExprAST expr)
        {
            var (ctxl, lhs_val) = Visit(ctx, expr.Lhs);
            var (ctxr, rhs_val) = Visit(ctxl, expr.Rhs);
            return (ctxr, BinaryVal(lhs_val, rhs_val, expr.NodeType));
        }

        public (Context, LLVMValueRef) VisitCallExprAST(Context ctx, CallExprAST expr)
        {
            var valueRef = LLVM.GetLastGlobal(_module);
            var func = LLVM.GetNamedFunction(_module, expr.Callee);
            var funcParams = LLVM.GetParams(func);
            if (expr.Arguments.Count != funcParams.Length)
                throw new InvalidOperationException("incorrect number of arguments passed");

            var argsValues = expr.Arguments.Select(p => Visit(ctx, p).Item2).ToArray();
            return (ctx, LLVM.BuildCall(_builder, func, argsValues, "calltmp"));
        }

        public (Context, LLVMValueRef) VisitForExprAST(Context ctx, ForExprAST expr)
        {
            throw new NotImplementedException();
        }

        public (Context, LLVMValueRef) VisitFunctionAST(Context ctx, FunctionAST expr)
        {
            var (ctxn, tf) = Visit(ctx, expr.Proto);
            var bb = LLVM.AppendBasicBlock(tf, "entry");
            LLVM.PositionBuilderAtEnd(_builder, bb);
            var (ctxn2, returnVal) = Visit(ctxn, expr.Body);
            LLVM.BuildRet(_builder, returnVal);
            LLVM.VerifyFunction(tf, LLVMVerifierFailureAction.LLVMPrintMessageAction);
            LLVM.RunFunctionPassManager(_passManager, tf);
            return (ctxn2, tf);
        }

        public (Context, LLVMValueRef) VisitIfExpAST(Context ctx, IfExpAST expr)
        {
            var _cond = expr.Condition;
            var _then = expr.Then;
            var _else = expr.Else;
            var (_, cond) = Visit(ctx, _cond);
            var zero = LLVM.ConstReal(LLVM.DoubleType(), 0);
            var cond_val = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealONE, cond, zero, "ifcond");
            var startBB = LLVM.GetInsertBlock(_builder);
            var the_function = LLVM.GetBasicBlockParent(startBB);
            var then_bb = LLVM.AppendBasicBlock(the_function, "then");
            var else_bb = LLVM.AppendBasicBlock(the_function, "else");
            var merge_bb = LLVM.AppendBasicBlock(the_function, "ifcont");
            LLVM.BuildCondBr(_builder, cond_val, then_bb, else_bb);
            LLVM.PositionBuilderAtEnd(_builder, then_bb);
            var (_, then_val) = Visit(ctx, _then);
            then_bb = LLVM.GetInsertBlock(_builder);
            LLVM.PositionBuilderAtEnd(_builder, else_bb);
            var (_, else_val) = Visit(ctx, _else);
            else_bb = LLVM.GetInsertBlock(_builder);
            LLVM.PositionBuilderAtEnd(_builder, merge_bb);
            var phi = LLVM.BuildPhi(_builder, LLVM.DoubleType(), "iftmp");
            LLVM.AddIncoming(phi, new[] { then_val }, new[] { then_bb }, 1u);
            LLVM.AddIncoming(phi, new[] { else_val }, new[] { else_bb }, 1u);
            LLVM.PositionBuilderAtEnd(_builder, then_bb);
            LLVM.BuildBr(_builder, merge_bb);
            LLVM.PositionBuilderAtEnd(_builder, else_bb);
            LLVM.BuildBr(_builder, merge_bb);
            LLVM.PositionBuilderAtEnd(_builder, merge_bb);
            return (ctx, phi);
        }

        public (Context, LLVMValueRef) VisitNumberExprAST(Context ctx, NumberExprAST expr)
        {
            return (ctx, LLVM.ConstReal(LLVM.DoubleType(), expr.Value));
        }

        public (Context, LLVMValueRef) VisitPrototypeAST(Context ctx, PrototypeAST expr)
        {
            var name = expr.Name;
            var args = expr.Arguments;
            var doubles = new LLVMTypeRef[args.Count];
            Array.Fill(doubles, LLVM.DoubleType());
            var f = LLVM.GetNamedFunction(_module, name);

            if (f.Pointer != IntPtr.Zero)
            {
                if (LLVM.CountBasicBlocks(f) != 0)
                    throw new InvalidOperationException("redefinition of function.");

                if (LLVM.CountParams(f) != args.Count)
                    throw new InvalidOperationException("redefinition of function with different # args.");
            }
            else
            {
                var ft = LLVM.FunctionType(LLVM.DoubleType(), doubles, false);
                f = LLVM.AddFunction(_module, name, ft);
                LLVM.SetLinkage(f, LLVMLinkage.LLVMExternalLinkage);
            }


            return (ctx.AddArguments(f, args), f);
        }

        public (Context, LLVMValueRef) VisitVariableExprAST(Context ctx, VariableExprAST expr)
        {
            var value = ctx.Get(expr.Name);

            if (value is null)
                throw new InvalidOperationException("variable not bound");

            return (ctx, value.GetValueOrDefault());
        }
    }
}
