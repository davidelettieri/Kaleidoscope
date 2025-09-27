namespace Kaleidoscope.AST;

public sealed class VariableExpression(string name) : Expression
{
    public string Name { get; } = name;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitVariable(ctx, this);
    }
}
