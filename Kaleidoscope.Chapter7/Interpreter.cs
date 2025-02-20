using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Kaleidoscope.AST;
using LLVMSharp.Interop;
using static Kaleidoscope.AST.ExpressionType;

namespace Kaleidoscope
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Print(double d);

    public unsafe class Interpreter : ExpressionVisitor<(Context, LLVMValueRef), Context>
    {
        private LLVMModuleRef _module;
        private LLVMBuilderRef _builder;
        private LLVMExecutionEngineRef _engine;
        private LLVMOpaquePassBuilderOptions* _passBuilderOptions;
        private readonly Dictionary<string, Expression> _functions;

        private void PutChard(double x)
        {
            try
            {
                Console.Write((char) x);
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
        }

        private void InitializeModule()
        {
            _module = LLVMModuleRef.CreateWithName("Kaleidoscope Module");
            _builder = _module.Context.CreateBuilder();
            _passBuilderOptions = LLVM.CreatePassBuilderOptions();

            // here we can also use _module.CreateInterpreter() which is slower but slightly simpler to handle
            _engine = _module.CreateMCJITCompiler();

            var ft = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, [LLVMTypeRef.Double]);
            var write = _module.AddFunction("putchard", ft);
            write.Linkage = LLVMLinkage.LLVMExternalLinkage;
            Delegate d = new Print(PutChard);
            var p = Marshal.GetFunctionPointerForDelegate(d);
            _engine.AddGlobalMapping(write, p);
        }

        public void Run(List<Expression> exprs)
        {
            // If we modify the module after we already executed some function with
            // _engine.RunFunction it will brake so for each run we instantiate the module again
            // any previous defined function will be emitted again in the current module

            InitializeModule();
            var toRun = new List<LLVMValueRef>();
            foreach (var item in exprs)
            {
                var ctx = new Context();
                var (_, v) = Visit(ctx, item);

                // Since we could have several expression to be evaluated we need to complete the emission of all
                // the code before running any of them, we keep track of what we need to run and then execute later in order
                if (item is FunctionExpression {Proto.Name: "anon_expr"})
                {
                    toRun.Add(v);
                }
            }

            var passes = new MarshaledString("mem2reg,instcombine,reassociate,gvn,simplifycfg");
            var passesError = LLVM.RunPasses(_module, passes, _engine.TargetMachine, _passBuilderOptions);

            if (passesError != null)
            {
                sbyte* errorMessage = LLVM.GetErrorMessage(passesError);
                var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)errorMessage);
                Console.WriteLine(span.AsString());
                return;
            }

            foreach (var v in toRun)
            {
                var res = _engine.RunFunction(v, Array.Empty<LLVMGenericValueRef>());
                var fres = LLVMTypeRef.Double.GenericValueToFloat(res);
                Console.WriteLine("> {0}", fres);
            }

            LLVM.DisposePassBuilderOptions(_passBuilderOptions);
            _builder.Dispose();
            _module.Dispose();
        }

        private (Context, LLVMValueRef) Visit(Context ctx, Expression body)
        {
            return body.Accept(this, ctx);
        }

        private LLVMValueRef BinaryVal(LLVMValueRef lhsVal, LLVMValueRef rhsVal, ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case Add:
                    return _builder.BuildFAdd(lhsVal, rhsVal, "addtmp");
                case Subtract:
                    return _builder.BuildFSub(lhsVal, rhsVal, "addtmp");
                case Multiply:
                    return _builder.BuildFMul(lhsVal, rhsVal, "addtmp");
                case LessThan:
                    var i = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhsVal, rhsVal, "cmptmp");
                    return _builder.BuildUIToFP(i, LLVMTypeRef.Double, "booltmp");
                case Equal:
                    var j = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, lhsVal, rhsVal, "cmptmp");
                    return _builder.BuildUIToFP(j, LLVMTypeRef.Double, "booltmp");
                default:
                    throw new InvalidOperationException();
            }
        }

        public (Context, LLVMValueRef) VisitBinary(Context ctx, BinaryExpression expr)
        {
            if (expr.NodeType == BinaryOperator)
            {
                var functionName = "binary_" + expr.OperatorToken.Value;
                var callExpr = new CallExpression(functionName, [expr.Lhs, expr.Rhs]);
                return Visit(ctx, callExpr);
            }
            else if (expr.NodeType == Assign)
            {
                if (expr.Lhs is VariableExpression ve)
                {
                    var (varExprCtx, rhs) = Visit(ctx, expr.Rhs);
                    var value = ctx.Get(ve.Name);

                    if (value is null)
                    {
                        throw new InvalidOperationException("Expected assigned value for variable");
                    }

                    _builder.BuildStore(rhs, value.Value);
                    return (varExprCtx, value.Value);
                }

                throw new InvalidOperationException("Expected variable in lhs of assign operator");
            }

            var (lhsCtx, lhsVal) = Visit(ctx, expr.Lhs);
            var (rhsCtx, rhsVal) = Visit(lhsCtx, expr.Rhs);
            return (rhsCtx, BinaryVal(lhsVal, rhsVal, expr.NodeType));
        }

        public (Context, LLVMValueRef) VisitCall(Context ctx, CallExpression expr)
        {
            var func = _module.GetNamedFunction(expr.Callee);

            if (func.Handle == IntPtr.Zero)
            {
                if (_functions.TryGetValue(expr.Callee, out var oldExpr))
                {
                    var pos = _builder.InsertBlock;
                    var (_, f) = Visit(ctx, oldExpr);
                    func = f;
                    _builder.PositionAtEnd(pos);
                }
                else
                {
                    return (ctx, null);
                }
            }

            var funcParams = func.GetParams();
            if (expr.Arguments.Count != funcParams.Length)
                throw new InvalidOperationException("incorrect number of arguments passed");

            var argsValues = expr.Arguments.Select(p => Visit(ctx, p).Item2).ToArray();
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double,
                Enumerable.Repeat(LLVMTypeRef.Double, expr.Arguments.Count).ToArray());
            return (ctx, _builder.BuildCall2(funcType, func, argsValues, "calltmp"));
        }

        public (Context, LLVMValueRef) VisitFor(Context ctx, ForExpression expr)
        {
            var varName = expr.VarName;
            var ctx1 = ctx.Add(varName, _builder);
            var start = expr.Start;
            var end = expr.End;
            var step = expr.Step;
            var body = expr.Body;
            var (ctx2, startVal) = Visit(ctx1, start);
            var varCtx1ValueRef = ctx1.Get(varName);
            if (varCtx1ValueRef is null)
            {
                throw new InvalidOperationException("Expected value for variable");
            }

            _builder.BuildStore(startVal, varCtx1ValueRef.Value);
            var preHeaderBb = _builder.InsertBlock;
            var theFunction = preHeaderBb.Parent;
            var loopBb = theFunction.AppendBasicBlock("loop");
            _builder.BuildBr(loopBb);
            _builder.PositionAtEnd(loopBb);
            Visit(ctx2, body);
            var varCtx2ValueRef = ctx2.Get(varName);
            if (varCtx2ValueRef is null)
            {
                throw new InvalidOperationException("Expected value for variable");
            }

            var variable = _builder.BuildLoad2(LLVMTypeRef.Double, varCtx2ValueRef.Value, varName);
            var (ctx3, stepVal) = step is not null
                ? Visit(ctx2, step)
                : (ctx2, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 1));
            var nextVar = _builder.BuildFAdd(variable, stepVal, "nextvar");
            var varCtx3ValueRef = ctx3.Get(varName);
            if (varCtx3ValueRef is null)
            {
                throw new InvalidOperationException("Expected value for variable");
            }

            _builder.BuildStore(nextVar, varCtx3ValueRef.Value);
            var (_, endCond) = Visit(ctx3, end);
            var zero = LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 0);
            var endCond2 = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, endCond, zero, "loopcond");
            var afterBb = theFunction.AppendBasicBlock("afterloop");
            _builder.BuildCondBr(endCond2, loopBb, afterBb);
            _builder.PositionAtEnd(afterBb);
            return (ctx, zero);
        }

        public (Context, LLVMValueRef) VisitFunction(Context ctx, FunctionExpression expr)
        {
            if (!string.IsNullOrWhiteSpace(expr.Proto.Name))
                _functions[expr.Proto.Name] = expr;

            var (ctxn, tf) = Visit(ctx, expr.Proto);

            var bb = tf.AppendBasicBlock("entry");
            _builder.PositionAtEnd(bb);

            for (int i = 0; i < expr.Proto.Arguments.Count; i++)
            {
                var n = expr.Proto.Arguments[i];
                var param = tf.GetParam((uint) i);
                param.Name = n;
                ctxn = ctxn.Add(n, _builder);
                var nValueRef = ctxn.Get(n);
                if (nValueRef is null)
                {
                    throw new InvalidOperationException("Expected value for parameter");
                }
                _builder.BuildStore(param, nValueRef.Value);
            }

            var (ctxn2, returnVal) = Visit(ctxn, expr.Body);
            _builder.BuildRet(returnVal);
            return (ctxn2, tf);
        }

        public (Context, LLVMValueRef) VisitExtern(Context ctx, ExternExpression expr)
        {
            _functions[expr.Proto.Name] = expr;
            return Visit(ctx, expr.Proto);
        }

        public (Context, LLVMValueRef) VisitIf(Context ctx, IfExpression expr)
        {
            var exprCondition = expr.Condition;
            var exprThen = expr.Then;
            var exprElse = expr.Else;
            var (_, cond) = Visit(ctx, exprCondition);
            var zero = LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 0);
            var condVal = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond, zero, "ifcond");
            var startBb = _builder.InsertBlock;
            var theFunction = startBb.Parent;
            var thenBb = theFunction.AppendBasicBlock("then");
            var elseBb = theFunction.AppendBasicBlock("else");
            var mergeBb = theFunction.AppendBasicBlock("ifcont");
            _builder.BuildCondBr(condVal, thenBb, elseBb);
            _builder.PositionAtEnd(thenBb);
            var (_, thenVal) = Visit(ctx, exprThen);
            thenBb = _builder.InsertBlock;
            _builder.PositionAtEnd(elseBb);
            var (_, elseVal) = Visit(ctx, exprElse);
            elseBb = _builder.InsertBlock;
            _builder.PositionAtEnd(mergeBb);
            var phi = _builder.BuildPhi(LLVMTypeRef.Double, "iftmp");
            phi.AddIncoming(new[] {thenVal}, new[] {thenBb}, 1u);
            phi.AddIncoming(new[] {elseVal}, new[] {elseBb}, 1u);
            _builder.PositionAtEnd(thenBb);
            _builder.BuildBr(mergeBb);
            _builder.PositionAtEnd(elseBb);
            _builder.BuildBr(mergeBb);
            _builder.PositionAtEnd(mergeBb);
            return (ctx, phi);
        }

        public (Context, LLVMValueRef) VisitNumber(Context ctx, NumberExpression expr)
        {
            return (ctx, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, expr.Value));
        }

        public (Context, LLVMValueRef) VisitPrototype(Context ctx, PrototypeExpression expr)
        {
            var name = expr.Name;
            var args = expr.Arguments;
            var doubles = new LLVMTypeRef[args.Count];
            Array.Fill(doubles, LLVMTypeRef.Double);
            var f = _module.GetNamedFunction(name);

            if (name != "anon_expr" && f.Handle != IntPtr.Zero)
            {
                if (f.BasicBlocksCount != 0)
                    throw new InvalidOperationException("redefinition of function.");

                if (f.ParamsCount != args.Count)
                    throw new InvalidOperationException("redefinition of function with different # args.");
            }
            else
            {
                var retType = LLVMTypeRef.Double;
                var ft = LLVMTypeRef.CreateFunction(retType, doubles);
                f = _module.AddFunction(name, ft);
                f.Linkage = LLVMLinkage.LLVMExternalLinkage;
            }

            return (ctx, f);
        }

        public (Context, LLVMValueRef) VisitVariable(Context ctx, VariableExpression expr)
        {
            var value = ctx.Get(expr.Name);

            if (value is null)
                throw new InvalidOperationException("variable not bound");

            return (ctx, _builder.BuildLoad2(LLVMTypeRef.Double, value.GetValueOrDefault(), expr.Name));
        }

        public (Context, LLVMValueRef) VisitUnary(Context ctx, UnaryExpression expr)
        {
            var functionName = "unary_" + expr.Operator.Value;
            var callExpr = new CallExpression(functionName, [expr.Operand]);
            return Visit(ctx, callExpr);
        }

        public (Context, LLVMValueRef) VisitVarInExpression(Context ctx, VarInExpression expr)
        {
            if (expr.Value is not null)
            {
                var (ctx1, value) = Visit(ctx, expr.Value);
                var ctx2 = ctx1.Add(expr.Name, _builder);
                var exprValueRef = ctx2.Get(expr.Name);
                if (exprValueRef is null)
                {
                    throw new InvalidOperationException("Expected value for var");
                }
                _builder.BuildStore(value, exprValueRef.Value);
                return Visit(ctx2, expr.Body);
            }

            var ctxn = ctx.Add(expr.Name, _builder);
            return Visit(ctxn, expr.Body);
        }
    }
}