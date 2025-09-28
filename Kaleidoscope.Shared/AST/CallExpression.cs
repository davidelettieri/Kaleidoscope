namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public sealed class CallExpression(string callee, List<Expression> args) : Expression
{
    public string Callee { get; } = callee;

    public List<Expression> Arguments { get; } = args;

    public ExpressionType NodeType { get; } = ExpressionType.Call;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx) => visitor.VisitCall(ctx, this);
}
