namespace Kaleidoscope.AST;

public sealed class FunctionExpression(PrototypeExpression proto, Expression body) : Expression
{
    public PrototypeExpression Proto { get; private set; } = proto;
    public Expression Body { get; private set; } = body;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitFunction(ctx, this);
    }
}