namespace Kaleidoscope.AST;

public sealed class ExternExpression(PrototypeExpression proto) : Expression
{
    public PrototypeExpression Proto { get; private set; } = proto;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitExtern(ctx, this);
    }
}