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
            NodeType = ExpressionType.Unary;
        }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitUnary(ctx, this);
        }
    }
}