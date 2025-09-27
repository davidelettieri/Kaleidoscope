namespace Kaleidoscope.AST;

using System.Collections.Generic;

public sealed class CallExpression(string callee, List<Expression> args) : Expression
{
    public string Callee { get; } = callee;

    public List<Expression> Arguments { get; } = args;

    public ExpressionType NodeType { get; } = ExpressionType.Call;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitCall(ctx, this);
}
