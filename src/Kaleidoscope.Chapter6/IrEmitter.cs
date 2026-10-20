using Kaleidoscope.Shared;
using Kaleidoscope.Shared.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kaleidoscope.Shared.AST.ExpressionType;

namespace Kaleidoscope;

public sealed class IrEmitter : IExpressionVisitor, IDisposable
{
    private readonly LLVMContextRef _context;
    private LLVMModuleRef _module;
    private readonly LLVMBuilderRef _builder;
    private readonly Dictionary<string, PrototypeExpression> _functions;
    private NamedValues _namedValues;
    private LLVMTypeRef DoubleType => _context.DoubleType;
    
    public IrEmitter(LLVMContextRef context, string moduleName = "KaleidoscopeModule")
    {
        _context = context;
        _module = _context.CreateModuleWithName(moduleName);
        _builder = _context.CreateBuilder();
        _functions = new Dictionary<string, PrototypeExpression>();
        _namedValues = NamedValues.Empty;
        
        _functions["putchard"] = new PrototypeExpression("putchard", ["ch"]);
        _functions["printd"]   = new PrototypeExpression("printd", ["val"]);
    }

    public LLVMModuleRef ExtractModuleAndReset(string newModuleName)
    {
        var activeModule = _module;
        _module = _context.CreateModuleWithName(newModuleName);
        _namedValues = NamedValues.Empty;
        return activeModule;
    }

    public LLVMValueRef Emit(Expression expr)
        => Visit(expr);

    private LLVMValueRef Visit(Expression body) => body.Accept(this);

    private LLVMValueRef BinaryVal(LLVMValueRef lhsVal, LLVMValueRef rhsVal, ExpressionType nodeType)
    {
        switch (nodeType)
        {
            case Add:
                return _builder.BuildFAdd(lhsVal, rhsVal, "addtmp");
            case Subtract:
                return _builder.BuildFSub(lhsVal, rhsVal, "subtmp");
            case Multiply:
                return _builder.BuildFMul(lhsVal, rhsVal, "multml");
            case LessThan:
                var i = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhsVal, rhsVal, "cmptmp");
                return _builder.BuildUIToFP(i, DoubleType, "booltmp");
            default:
                throw new InvalidOperationException();
        }
    }

    public LLVMValueRef VisitBinary(BinaryExpression expr)
    {
        if (expr.NodeType == BinaryOperator)
        {
            var functionName = "binary_" + expr.OperatorToken.Value;
            var callExpr = new CallExpression(functionName, [expr.Lhs, expr.Rhs]);
            return Visit(callExpr);
        }

        var lhsVal = Visit(expr.Lhs);
        var rhsVal = Visit(expr.Rhs);
        return BinaryVal(lhsVal, rhsVal, expr.NodeType);
    }

    public LLVMValueRef VisitCall(CallExpression expr)
    {
        var func = _module.GetNamedFunction(expr.Callee);

        if (func.Handle == IntPtr.Zero)
        {
            if (_functions.TryGetValue(expr.Callee, out var oldExpr))
            {
                var pos = _builder.InsertBlock;
                var f = Visit(oldExpr);
                func = f;
                _builder.PositionAtEnd(pos);
            }
            else
            {
                return null;
            }
        }

        var funcParams = func.GetParams();
        if (expr.Arguments.Count != funcParams.Length)
            throw new InvalidOperationException("incorrect number of arguments passed");

        var argsValues = expr.Arguments.Select(Visit).ToArray();
        var funcType = LLVMTypeRef.CreateFunction(DoubleType,
            Enumerable.Repeat(DoubleType, expr.Arguments.Count).ToArray());
        return _builder.BuildCall2(funcType, func, argsValues, "calltmp");
    }

    public LLVMValueRef VisitFor(ForExpression expr)
    {
        var varName = expr.VarName;
        var start = expr.Start;
        var end = expr.End;
        var step = expr.Step;
        var body = expr.Body;
        var startVal = Visit(start);
        var preHeaderBb = _builder.InsertBlock;
        var theFunction = preHeaderBb.Parent;
        var loopBb = theFunction.AppendBasicBlock("loop");
        _builder.BuildBr(loopBb);
        _builder.PositionAtEnd(loopBb);
        var variable = _builder.BuildPhi(DoubleType, varName);
        variable.AddIncoming(new[] { startVal }, new[] { preHeaderBb }, 1u);
        var originalNamedValues = _namedValues;
        _namedValues = _namedValues.Add(varName, variable);
        Visit(body);
        LLVMValueRef stepVal = step is not null ? Visit(step) : LLVMValueRef.CreateConstReal(DoubleType, 1);
        var nextVar = _builder.BuildFAdd(variable, stepVal, "nextvar");
        var endCond = Visit(end);
        var zero = LLVMValueRef.CreateConstReal(DoubleType, 0);
        var endCond2 = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, endCond, zero, "loopcond");
        var loopEndBb = _builder.InsertBlock;
        var afterBb = theFunction.AppendBasicBlock("afterloop");
        _builder.BuildCondBr(endCond2, loopBb, afterBb);
        _builder.PositionAtEnd(afterBb);
        variable.AddIncoming(new[] { nextVar }, new[] { loopEndBb }, 1u);
        _namedValues = originalNamedValues;
        return zero;
    }

    public LLVMValueRef VisitFunction(FunctionExpression expr)
    {
        var originalNamedValues = _namedValues;
        if (!string.IsNullOrWhiteSpace(expr.Proto.Name))
            _functions[expr.Proto.Name] = expr.Proto;

        var tf = Visit(expr.Proto);
        var bb = tf.AppendBasicBlock("entry");
        _builder.PositionAtEnd(bb);
        var returnVal = Visit(expr.Body);
        _builder.BuildRet(returnVal);
        _namedValues = originalNamedValues;
        return tf;
    }

    public LLVMValueRef VisitExtern(ExternExpression expr)
    {
        _functions[expr.Proto.Name] = expr.Proto;
        var originalNamedValues = _namedValues;
        var result = Visit(expr.Proto);
        _namedValues = originalNamedValues;
        return result;
    }

    public LLVMValueRef VisitIf(IfExpression expr)
    {
        var cond = Visit(expr.Condition);
        var zero = LLVMValueRef.CreateConstReal(DoubleType, 0);
        var condVal = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond, zero, "ifcond");

        var startBb = _builder.InsertBlock;
        var theFunction = startBb.Parent;

        var thenBb = theFunction.AppendBasicBlock("then");
        var elseBb = theFunction.AppendBasicBlock("else");
        var mergeBb = theFunction.AppendBasicBlock("ifcont");

        _builder.BuildCondBr(condVal, thenBb, elseBb);

        // --- THEN BRANCH ---
        _builder.PositionAtEnd(thenBb);
        var thenVal = Visit(expr.Then);
        _builder.BuildBr(mergeBb); // Branch to merge immediately
        thenBb = _builder.InsertBlock; // Capture the actual ending block of 'then'

        // --- ELSE BRANCH ---
        _builder.PositionAtEnd(elseBb);
        var elseVal = Visit(expr.Else);
        _builder.BuildBr(mergeBb); // Branch to merge immediately
        elseBb = _builder.InsertBlock; // Capture the actual ending block of 'else'

        // --- PHI NODE IN MERGE BLOCK ---
        _builder.PositionAtEnd(mergeBb);
        var phi = _builder.BuildPhi(DoubleType, "iftmp");
        phi.AddIncoming(new[] { thenVal }, new[] { thenBb }, 1u);
        phi.AddIncoming(new[] { elseVal }, new[] { elseBb }, 1u);

        return phi;
    }

    public LLVMValueRef VisitNumber(NumberExpression expr) =>
        LLVMValueRef.CreateConstReal(DoubleType, expr.Value);

    public LLVMValueRef VisitPrototype(PrototypeExpression expr)
    {
        var name = expr.Name;
        var args = expr.Arguments;
        var doubles = new LLVMTypeRef[args.Count];
        Array.Fill(doubles, DoubleType);
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
            var retType = DoubleType;
            var ft = LLVMTypeRef.CreateFunction(retType, doubles);
            f = _module.AddFunction(name, ft);
            f.Linkage = LLVMLinkage.LLVMExternalLinkage;
        }

        _namedValues = _namedValues.AddArguments(f, args);
        return f;
    }

    public LLVMValueRef VisitVariable(VariableExpression expr)
    {
        var value = _namedValues.Get(expr.Name);

        if (value is null)
            throw new InvalidOperationException("variable not bound");

        return value.GetValueOrDefault();
    }

    public LLVMValueRef VisitUnary(UnaryExpression expr)
    {
        var functionName = "unary_" + expr.Operator.Value;
        var callExpr = new CallExpression(functionName, [expr.Operand]);
        return Visit(callExpr);
    }

    /// <summary>
    /// This will be implemented in chapter 7
    /// </summary>
    public LLVMValueRef VisitVarInExpression(VarInExpression expr)
        => throw new NotImplementedException();

    public void Dispose()
    {
        _module.Dispose();
        _builder.Dispose();
    }
}
