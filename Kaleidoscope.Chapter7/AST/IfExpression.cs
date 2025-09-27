namespace Kaleidoscope.AST;

public sealed class IfExpression(Expression condition, Expression then, Expression @else) : Expression
{
    public Expression Condition { get; private set; } = condition;
    public Expression Then { get; private set; } = then;
    public Expression Else { get; private set; } = @else;


    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitIf(ctx, this);
    }
}
