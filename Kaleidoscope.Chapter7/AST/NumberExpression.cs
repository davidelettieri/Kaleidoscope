namespace Kaleidoscope.AST
{
    public sealed class NumberExpression : Expression
    {
        public NumberExpression(double value)
        {
            Value = value;
        }

        public double Value { get; private set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitNumber(ctx,this);
        }
    }
}