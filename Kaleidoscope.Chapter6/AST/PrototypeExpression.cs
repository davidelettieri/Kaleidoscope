namespace Kaleidoscope.AST;

using System.Collections.Generic;

public class PrototypeExpression(string name, List<string> args) : Expression
{
    public string Name { get; } = name;
    public List<string> Arguments { get; } = args;
    public virtual ExpressionType NodeType { get; } = ExpressionType.Prototype;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitPrototype(ctx, this);
    }
}
