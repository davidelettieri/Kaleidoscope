namespace Kaleidoscope.AST;

using System.Collections.Generic;

public sealed class CallExpression(string callee, List<Expression> args) : Expression
{
    public string Callee { get; } = callee;
    public List<Expression> Arguments { get; } = args;


    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitCall(ctx, this);
    }
}
