using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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

    public class IREmitter : ExprVisitor<(Context, LLVMValueRef), Context>
    {
        private readonly LLVMModuleRef _module;
        private readonly LLVMBuilderRef _builder;
        private readonly LLVMPassManagerRef _passManager;
        private readonly LLVMExecutionEngineRef _engine;
        public IREmitter()
        {
            _module = LLVM.ModuleCreateWithName("Kaleidoscope Module");
            _builder = LLVM.CreateBuilder();
            _passManager = LLVM.CreateFunctionPassManagerForModule(_module);
            LLVM.AddBasicAliasAnalysisPass(_passManager);
            LLVM.AddPromoteMemoryToRegisterPass(_passManager);
            LLVM.AddInstructionCombiningPass(_passManager);
            LLVM.AddReassociatePass(_passManager);
            LLVM.AddGVNPass(_passManager);
            LLVM.AddCFGSimplificationPass(_passManager);
            LLVM.InitializeFunctionPassManager(_passManager);

            LLVM.CreateExecutionEngineForModule(out _engine, _module, out var error);

            if (!string.IsNullOrWhiteSpace(error))
                throw new Exception(error);
        }

        public void Intepret(List<ExprAST> exprs)
        {
            var ctx = new Context();
            foreach (var item in exprs)
            {
                var (ctxn, v) = Visit(ctx, item);
                LLVM.DumpValue(v);
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
            throw new NotImplementedException();
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
