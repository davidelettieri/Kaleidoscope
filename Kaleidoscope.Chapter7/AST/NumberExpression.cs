namespace Kaleidoscope.AST;

public sealed class NumberExpression(double value) : Expression
{
    public double Value { get; } = value;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitNumber(ctx, this);
}
