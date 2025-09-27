namespace Kaleidoscope.AST;

public sealed class NumberExpression(double value) : Expression
{
    public double Value { get; private set; } = value;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitNumber(ctx,this);
    }
}
