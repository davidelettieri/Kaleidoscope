using Kaleidoscope.Shared;
using Kaleidoscope.Shared.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kaleidoscope.Shared.AST.ExpressionType;

namespace Kaleidoscope;

public sealed class IrEmitter : IExpressionVisitor, IDisposable
{
    private readonly record struct FunctionSymbol(PrototypeExpression Prototype, bool HasBody);
    
    private readonly LLVMContextRef _context;
    private string _moduleName;
    private LLVMModuleRef _module;
    private readonly LLVMBuilderRef _builder;
    private readonly Dictionary<string, FunctionSymbol> _symbols = new();
    private Dictionary<string, FunctionSymbol>? _batchSnapshot;
    private NamedValues _namedValues = NamedValues.Empty;

    private LLVMTypeRef DoubleType => _context.DoubleType;

    public IrEmitter(LLVMContextRef context, string moduleName = "KaleidoscopeModule")
    {
        _context = context;
        _moduleName = moduleName;
        _module = _context.CreateModuleWithName(moduleName);
        _builder = _context.CreateBuilder();

        foreach (var prototype in Builtins.Prototypes)
            _symbols[prototype.Name] = new FunctionSymbol(prototype, HasBody: true);
    }

    internal void BeginBatch()
        => _batchSnapshot = new Dictionary<string, FunctionSymbol>(_symbols);

    internal void CommitBatch()
        => _batchSnapshot = null;

    internal void AbortBatch()
    {
        if (_batchSnapshot is null)
            return;

        _symbols.Clear();
        foreach (var (name, symbol) in _batchSnapshot)
            _symbols[name] = symbol;

        _batchSnapshot = null;
        DiscardModule();
    }

    internal LLVMModuleRef ExtractModuleAndReset(string newModuleName)
    {
        var activeModule = _module;
        _moduleName = newModuleName;
        _module = _context.CreateModuleWithName(newModuleName);
        _namedValues = NamedValues.Empty;
        _builder.ClearInsertionPosition();
        return activeModule;
    }

    public LLVMValueRef Emit(Expression expr)
        => Visit(expr);

    private LLVMValueRef Visit(Expression body) => body.Accept(this);

    private LLVMValueRef EmitArithmetic(LLVMValueRef lhsVal, LLVMValueRef rhsVal, ExpressionType nodeType)
    {
        switch (nodeType)
        {
            case Add:
                return _builder.BuildFAdd(lhsVal, rhsVal, "addtmp");
            case Subtract:
                return _builder.BuildFSub(lhsVal, rhsVal, "subtmp");
            case Multiply:
                return _builder.BuildFMul(lhsVal, rhsVal, "multmp");
            case LessThan:
                var less = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhsVal, rhsVal, "cmptmp");
                return _builder.BuildUIToFP(less, DoubleType, "booltmp");
            case Equal:
                var equal = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, lhsVal, rhsVal, "cmptmp");
                return _builder.BuildUIToFP(equal, DoubleType, "booltmp");
            default:
                throw new InvalidOperationException($"unsupported operator '{nodeType}'");
        }
    }

    public LLVMValueRef VisitBinary(BinaryExpression expr)
    {
        if (expr.NodeType == Assign)
            throw new InvalidOperationException("assignment is not supported in this chapter");

        if (expr.NodeType == BinaryOperator)
        {
            var functionName = "binary_" + expr.OperatorToken.Value;
            return Visit(new CallExpression(functionName, [expr.Lhs, expr.Rhs]));
        }

        var lhsVal = Visit(expr.Lhs);
        var rhsVal = Visit(expr.Rhs);
        return EmitArithmetic(lhsVal, rhsVal, expr.NodeType);
    }

    public LLVMValueRef VisitCall(CallExpression expr)
    {
        var function = Resolve(expr.Callee);
        if (expr.Arguments.Count != function.ParamsCount)
            throw new InvalidOperationException($"incorrect number of arguments passed to '{expr.Callee}'");

        var args = expr.Arguments.Select(Visit).ToArray();
        var signature = LLVMTypeRef.CreateFunction(
            DoubleType,
            Enumerable.Repeat(DoubleType, args.Length).ToArray());
        return _builder.BuildCall2(signature, function, args, "calltmp");
    }

    public LLVMValueRef VisitFor(ForExpression expr)
    {
        var startVal = Visit(expr.Start);
        var preHeader = _builder.InsertBlock;
        var function = preHeader.Parent;
        var loop = function.AppendBasicBlock("loop");
        _builder.BuildBr(loop);
        _builder.PositionAtEnd(loop);

        var variable = _builder.BuildPhi(DoubleType, expr.VarName);
        variable.AddIncoming(new[] { startVal }, new[] { preHeader }, 1u);

        var originalNamedValues = _namedValues;
        _namedValues = _namedValues.Add(expr.VarName, variable);
        try
        {
            Visit(expr.Body);
            var stepVal = expr.Step is not null
                ? Visit(expr.Step)
                : LLVMValueRef.CreateConstReal(DoubleType, 1);
            var nextVar = _builder.BuildFAdd(variable, stepVal, "nextvar");
            var endCond = Visit(expr.End);
            var zero = LLVMValueRef.CreateConstReal(DoubleType, 0);
            var continueLoop = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, endCond, zero, "loopcond");
            var loopEnd = _builder.InsertBlock;
            var after = function.AppendBasicBlock("afterloop");
            _builder.BuildCondBr(continueLoop, loop, after);
            _builder.PositionAtEnd(after);
            variable.AddIncoming(new[] { nextVar }, new[] { loopEnd }, 1u);
            return zero;
        }
        finally
        {
            _namedValues = originalNamedValues;
        }
    }

    public LLVMValueRef VisitFunction(FunctionExpression expr)
    {
        var originalNamedValues = _namedValues;
        LLVMValueRef function = default;
        var started = false;
        try
        {
            function = PrepareDefinition(expr.Proto);
            started = true;
            var entry = function.AppendBasicBlock("entry");
            _builder.PositionAtEnd(entry);
            _namedValues = NamedValues.Empty.AddArguments(function, expr.Proto.Arguments);
            _builder.BuildRet(Visit(expr.Body));

            if (!function.VerifyFunction(LLVMVerifierFailureAction.LLVMReturnStatusAction))
                throw new InvalidOperationException($"function '{expr.Proto.Name}' produced invalid IR");

            if (!IsAnonymous(expr.Proto.Name))
                _symbols[expr.Proto.Name] = new FunctionSymbol(expr.Proto, HasBody: true);

            return function;
        }
        catch
        {
            if (started && function.Handle != IntPtr.Zero)
                function.DeleteFunction();

            throw;
        }
        finally
        {
            _namedValues = originalNamedValues;
        }
    }

    public LLVMValueRef VisitExtern(ExternExpression expr)
    {
        var name = expr.Proto.Name;
        if (_symbols.TryGetValue(name, out var known))
        {
            if (known.Prototype.Arguments.Count != expr.Proto.Arguments.Count)
                throw new InvalidOperationException($"redefinition of function '{name}' with a different number of arguments");

            return Declare(expr.Proto);
        }

        var function = Declare(expr.Proto);
        _symbols[name] = new FunctionSymbol(expr.Proto, HasBody: false);
        return function;
    }

    public LLVMValueRef VisitIf(IfExpression expr)
    {
        var condition = Visit(expr.Condition);
        var zero = LLVMValueRef.CreateConstReal(DoubleType, 0);
        var conditionValue = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, condition, zero, "ifcond");

        var function = _builder.InsertBlock.Parent;
        var thenBlock = function.AppendBasicBlock("then");
        var elseBlock = function.AppendBasicBlock("else");
        var mergeBlock = function.AppendBasicBlock("ifcont");
        _builder.BuildCondBr(conditionValue, thenBlock, elseBlock);

        _builder.PositionAtEnd(thenBlock);
        var thenValue = Visit(expr.Then);
        _builder.BuildBr(mergeBlock);
        thenBlock = _builder.InsertBlock;

        _builder.PositionAtEnd(elseBlock);
        var elseValue = Visit(expr.Else);
        _builder.BuildBr(mergeBlock);
        elseBlock = _builder.InsertBlock;

        _builder.PositionAtEnd(mergeBlock);
        var phi = _builder.BuildPhi(DoubleType, "iftmp");
        phi.AddIncoming(new[] { thenValue }, new[] { thenBlock }, 1u);
        phi.AddIncoming(new[] { elseValue }, new[] { elseBlock }, 1u);
        return phi;
    }

    public LLVMValueRef VisitNumber(NumberExpression expr)
        => LLVMValueRef.CreateConstReal(DoubleType, expr.Value);

    public LLVMValueRef VisitPrototype(PrototypeExpression expr)
        => Declare(expr);

    public LLVMValueRef VisitVariable(VariableExpression expr)
    {
        var value = _namedValues.Get(expr.Name);
        if (value is null)
            throw new InvalidOperationException($"variable '{expr.Name}' is not bound");

        return value.GetValueOrDefault();
    }

    public LLVMValueRef VisitUnary(UnaryExpression expr)
    {
        var functionName = "unary_" + expr.Operator.Value;
        return Visit(new CallExpression(functionName, [expr.Operand]));
    }

    /// <summary>
    /// Mutable variables arrive in chapter 7.
    /// </summary>
    public LLVMValueRef VisitVarInExpression(VarInExpression expr)
        => throw new NotImplementedException("var/in is not supported in this chapter");

    public void Dispose()
    {
        _builder.ClearInsertionPosition();
        _module.Dispose();
        _builder.Dispose();
    }

    private LLVMValueRef Resolve(string name)
    {
        var function = _module.GetNamedFunction(name);
        if (function.Handle != IntPtr.Zero)
            return function;

        if (!_symbols.TryGetValue(name, out var known))
            throw new InvalidOperationException($"unknown function '{name}'");

        return Declare(known.Prototype);
    }

    private LLVMValueRef PrepareDefinition(PrototypeExpression prototype)
    {
        var name = prototype.Name;
        if (_symbols.TryGetValue(name, out var known))
        {
            if (known.HasBody)
                throw new InvalidOperationException($"redefinition of function '{name}'");

            if (known.Prototype.Arguments.Count != prototype.Arguments.Count)
                throw new InvalidOperationException($"redefinition of function '{name}' with a different number of arguments");
        }

        var function = Declare(prototype);
        if (function.BasicBlocksCount != 0)
            throw new InvalidOperationException($"redefinition of function '{name}'");

        return function;
    }

    private LLVMValueRef Declare(PrototypeExpression prototype)
    {
        var existing = _module.GetNamedFunction(prototype.Name);
        if (existing.Handle != IntPtr.Zero)
        {
            if (existing.ParamsCount != prototype.Arguments.Count)
                throw new InvalidOperationException($"redefinition of function '{prototype.Name}' with a different number of arguments");

            return existing;
        }

        var argumentTypes = new LLVMTypeRef[prototype.Arguments.Count];
        Array.Fill(argumentTypes, DoubleType);
        var function = _module.AddFunction(prototype.Name, LLVMTypeRef.CreateFunction(DoubleType, argumentTypes));
        function.Linkage = LLVMLinkage.LLVMExternalLinkage;

        for (var i = 0; i < prototype.Arguments.Count; i++)
            function.GetParam((uint)i).Name = prototype.Arguments[i];

        return function;
    }

    private void DiscardModule()
    {
        _builder.ClearInsertionPosition();
        _module.Dispose();
        _module = _context.CreateModuleWithName(_moduleName);
        _namedValues = NamedValues.Empty;
    }

    private static bool IsAnonymous(string name)
        => name.StartsWith("__anon_expr_", StringComparison.Ordinal);
}
