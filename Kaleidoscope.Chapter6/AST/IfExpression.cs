namespace Kaleidoscope.AST;

public sealed class IfExpression(Expression condition, Expression then, Expression @else) : Expression
{
    public Expression Condition { get; } = condition;
    public Expression Then { get; } = then;
    public Expression Else { get; } = @else;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitIf(ctx, this);
}
