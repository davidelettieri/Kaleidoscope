﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Kaleidoscope.AST;
using LLVMSharp.Interop;
using static Kaleidoscope.AST.ExpressionType;

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
                var param = function.GetParam((uint)i);
                param.Name = name;
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
    public delegate void Print(double d);

    public class Interpreter : ExpressionVisitor<(Context, LLVMValueRef), Context>
    {
        private LLVMModuleRef _module;
        private LLVMBuilderRef _builder;
        private LLVMPassManagerRef _passManager;
        private LLVMExecutionEngineRef _engine;
        private readonly Dictionary<string, Expression> _functions;

        private void Printd(double x)
        {
            try
            {
                Console.WriteLine("> {0}", x);
            }
            catch
            {
            }
        }

        public Interpreter()
        {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
            _functions = new Dictionary<string, Expression>();

            // var ft = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, new[] { LLVMTypeRef.Double }, false);
            // var write = _module.AddFunction("print", ft);
            // write.Linkage = LLVMLinkage.LLVMExternalLinkage;
            // Delegate d = new Print(Printd);
            // var p = Marshal.GetFunctionPointerForDelegate(d);
            // _engine.AddGlobalMapping(write, p);
        }

        private void InitializeModule()
        {
            _module = LLVMModuleRef.CreateWithName("Kaleidoscope Module");
            _builder = _module.Context.CreateBuilder();
            _passManager = _module.CreateFunctionPassManager();
            _passManager.AddBasicAliasAnalysisPass();
            _passManager.AddPromoteMemoryToRegisterPass();
            _passManager.AddInstructionCombiningPass();
            _passManager.AddReassociatePass();
            _passManager.AddGVNPass();
            _passManager.AddCFGSimplificationPass();
            _passManager.InitializeFunctionPassManager();
            _engine = _module.CreateMCJITCompiler();
        }

        public void Run(List<Expression> exprs)
        {
            InitializeModule();
            var ctx = new Context();
            foreach (var item in exprs)
            {
                var (ctxn, v) = Visit(ctx, item);
                if (item is FunctionExpression f && string.IsNullOrWhiteSpace(f.Proto.Name))
                {
                    var res = _engine.RunFunction(v, Array.Empty<LLVMGenericValueRef>());
                    var fres = LLVMTypeRef.Double.GenericValueToFloat(res);
                    Console.WriteLine("> {0}", fres);
                }
                ctx = ctxn;
            }
            _passManager.Dispose();
            _builder.Dispose();
            _module.Dispose();
        }

        private (Context, LLVMValueRef) Visit(Context ctx, Expression body)
        {
            return body.Accept(this, ctx);
        }

        private LLVMValueRef BinaryVal(LLVMValueRef lhs_val, LLVMValueRef rhs_val, ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case Add:
                    return _builder.BuildFAdd(lhs_val, rhs_val, "addtmp");
                case Subtract:
                    return _builder.BuildFSub(lhs_val, rhs_val, "addtmp");
                case Multiply:
                    return _builder.BuildFMul(lhs_val, rhs_val, "addtmp");
                case LessThan:
                    var i = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhs_val, rhs_val, "cmptmp");
                    return _builder.BuildUIToFP(i, LLVMTypeRef.Double, "booltmp");
                default:
                    throw new InvalidOperationException();
            }
        }

        public (Context, LLVMValueRef) VisitBinaryExprAST(Context ctx, BinaryExpression expr)
        {
            var (ctxl, lhs_val) = Visit(ctx, expr.Lhs);
            var (ctxr, rhs_val) = Visit(ctxl, expr.Rhs);
            return (ctxr, BinaryVal(lhs_val, rhs_val, expr.NodeType));
        }

        public (Context, LLVMValueRef) VisitCallExprAST(Context ctx, CallExpression expr)
        {
            var func = _module.GetNamedFunction(expr.Callee);

            if (func.Handle == IntPtr.Zero)
            {
                if (_functions.TryGetValue(expr.Callee, out var oldExpr))
                {
                    var pos = _builder.InsertBlock;
                    var (ctx1, f) = Visit(ctx, oldExpr);
                    ctx = ctx1;
                    func = f;
                    _builder.PositionAtEnd(pos);
                }
                else
                {
                    return (ctx, null);
                }
            }

            var funcParams = func.Params;
            if (expr.Arguments.Count != funcParams.Length)
                throw new InvalidOperationException("incorrect number of arguments passed");

            var argsValues = expr.Arguments.Select(p => Visit(ctx, p).Item2).ToArray();
            return (ctx, _builder.BuildCall(func, argsValues, "calltmp"));
        }

        public (Context, LLVMValueRef) VisitForExprAST(Context ctx, ForExpression expr)
        {
            var var_name = expr.VarName;
            var start = expr.Start;
            var end_ = expr.End;
            var step = expr.Step;
            var body = expr.Body;
            var (ctx1, start_val) = Visit(ctx, start);
            var preheader_bb = _builder.InsertBlock;
            var the_function = preheader_bb.Parent;
            var loop_bb = the_function.AppendBasicBlock("loop");
            _builder.BuildBr(loop_bb);
            _builder.PositionAtEnd(loop_bb);
            var variable = _builder.BuildPhi(LLVMTypeRef.Double, var_name);
            variable.AddIncoming(new[] { start_val }, new[] { preheader_bb }, 1u);
            var ctx2 = ctx1.Add(var_name, variable);
            Visit(ctx2, body);
            var (ctx3, step_val) = step is not null ? Visit(ctx2, step) : (ctx2, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 1));
            var next_var = _builder.BuildFAdd(variable, step_val, "nextvar");
            var (ctx4, end_cond) = Visit(ctx3, end_);
            var zero = LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 0);
            var end_cond2 = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, end_cond, zero, "loopcond");
            var loop_end_bb = _builder.InsertBlock;
            var after_bb = the_function.AppendBasicBlock("afterloop");
            _builder.BuildCondBr(end_cond2, loop_bb, after_bb);
            _builder.PositionAtEnd(after_bb);
            variable.AddIncoming(new[] { next_var }, new[] { loop_end_bb }, 1u);
            return (ctx, zero);
        }

        public (Context, LLVMValueRef) VisitFunctionAST(Context ctx, FunctionExpression expr)
        {
            if (!string.IsNullOrWhiteSpace(expr.Proto.Name))
                _functions[expr.Proto.Name] = expr;

            var (ctxn, tf) = Visit(ctx, expr.Proto);
            var bb = tf.AppendBasicBlock("entry");
            _builder.PositionAtEnd(bb);
            var (ctxn2, returnVal) = Visit(ctxn, expr.Body);
            _builder.BuildRet(returnVal);
            _passManager.RunFunctionPassManager(tf);
            return (ctxn2, tf);
        }

        public (Context, LLVMValueRef) VisitExternAST(Context ctx, ExternExpression expr)
        {
            _functions[expr.Proto.Name] = expr;
            return Visit(ctx, expr.Proto);
        }

        public (Context, LLVMValueRef) VisitIfExpAST(Context ctx, IfExpression expr)
        {
            var _cond = expr.Condition;
            var _then = expr.Then;
            var _else = expr.Else;
            var (_, cond) = Visit(ctx, _cond);
            var zero = LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 0);
            var cond_val = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond, zero, "ifcond");
            var startBB = _builder.InsertBlock;
            var the_function = startBB.Parent;
            var then_bb = the_function.AppendBasicBlock("then");
            var else_bb = the_function.AppendBasicBlock("else");
            var merge_bb = the_function.AppendBasicBlock("ifcont");
            _builder.BuildCondBr(cond_val, then_bb, else_bb);
            _builder.PositionAtEnd(then_bb);
            var (_, then_val) = Visit(ctx, _then);
            then_bb = _builder.InsertBlock;
            _builder.PositionAtEnd(else_bb);
            var (_, else_val) = Visit(ctx, _else);
            else_bb = _builder.InsertBlock;
            _builder.PositionAtEnd(merge_bb);
            var phi = _builder.BuildPhi(LLVMTypeRef.Double, "iftmp");
            phi.AddIncoming(new[] { then_val }, new[] { then_bb }, 1u);
            phi.AddIncoming(new[] { else_val }, new[] { else_bb }, 1u);
            _builder.PositionAtEnd(then_bb);
            _builder.BuildBr(merge_bb);
            _builder.PositionAtEnd(else_bb);
            _builder.BuildBr(merge_bb);
            _builder.PositionAtEnd(merge_bb);
            return (ctx, phi);
        }

        public (Context, LLVMValueRef) VisitNumberExprAST(Context ctx, NumberExpression expr)
        {
            return (ctx, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, expr.Value));
        }

        public (Context, LLVMValueRef) VisitPrototypeAST(Context ctx, PrototypeExpression expr)
        {
            var name = expr.Name;
            var args = expr.Arguments;
            var doubles = new LLVMTypeRef[args.Count];
            Array.Fill(doubles, LLVMTypeRef.Double);
            var f = _module.GetNamedFunction(name);

            if (f.Handle != IntPtr.Zero)
            {
                if (f.BasicBlocksCount != 0)
                    throw new InvalidOperationException("redefinition of function.");

                if (f.ParamsCount != args.Count)
                    throw new InvalidOperationException("redefinition of function with different # args.");
            }
            else
            {
                var retType = expr.Name == "write" ? LLVMTypeRef.Void : LLVMTypeRef.Double;
                var ft = LLVMTypeRef.CreateFunction(retType, doubles, false);
                f = _module.AddFunction(name, ft);
                f.Linkage = LLVMLinkage.LLVMExternalLinkage;
            }


            return (ctx.AddArguments(f, args), f);
        }

        public (Context, LLVMValueRef) VisitVariableExprAST(Context ctx, VariableExpression expr)
        {
            var value = ctx.Get(expr.Name);

            if (value is null)
                throw new InvalidOperationException("variable not bound");

            return (ctx, value.GetValueOrDefault());
        }
    }
}
