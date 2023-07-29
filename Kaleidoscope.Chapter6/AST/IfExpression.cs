namespace Kaleidoscope.AST
{
    public sealed class IfExpression : Expression
    {
        public IfExpression(Expression condition, Expression then, Expression @else)
        {
            Condition = condition;
            Then = then;
            Else = @else;
        }

        public Expression Condition { get; private set; }
        public Expression Then { get; private set; }
        public Expression Else { get; private set; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitIf(ctx, this);
        }
    }
}