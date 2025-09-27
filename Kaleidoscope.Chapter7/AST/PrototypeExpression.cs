namespace Kaleidoscope.AST;

using System.Collections.Generic;

public class PrototypeExpression(string name, List<string> args) : Expression
{
    public string Name { get; } = name;
    public List<string> Arguments { get; } = args;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitPrototype(ctx, this);
    }
}