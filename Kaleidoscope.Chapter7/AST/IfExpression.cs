namespace Kaleidoscope.AST
{
    public sealed class IfExpression : Expression
    {
        public IfExpression(Expression condition, Expression then, Expression @else)
        {
            this.Condition = condition;
            this.Then = then;
            this.Else = @else;
            this.NodeType = ExpressionType.If;
        }

        public Expression Condition { get; private set; }

        public Expression Then { get; private set; }

        public Expression Else { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitIf(ctx, this);
        }
    }
}