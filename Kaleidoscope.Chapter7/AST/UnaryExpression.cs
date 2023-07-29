namespace Kaleidoscope.AST
{
    public sealed class UnaryExpression : Expression
    {
        public Token Operator { get; }
        public Expression Operand { get; }
        public UnaryExpression(Token @operator, Expression operand)
        {
            Operator = @operator;
            Operand = operand;
        }


        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitUnary(ctx, this);
        }
    }
}