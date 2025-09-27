namespace Kaleidoscope.AST;

public sealed class FunctionExpression(PrototypeExpression proto, Expression body) : Expression
{
    public PrototypeExpression Proto { get; } = proto;
    public Expression Body { get; } = body;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitFunction(ctx, this);
    }
}
