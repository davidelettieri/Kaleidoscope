namespace Kaleidoscope.AST
{
    public sealed class NumberExpression : Expression
    {
        public NumberExpression(double value)
        {
            this.Value = value;
            this.NodeType = ExpressionType.Number;
        }

        public double Value { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitNumber(ctx,this);
        }
    }
}