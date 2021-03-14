namespace Kaleidoscope.AST
{
    using System;

    public sealed class BinaryExpression : Expression
    {
        public BinaryExpression(TokenType op, Expression lhs, Expression rhs)
        {
            switch (op)
            {
                case TokenType.PLUS:
                    NodeType = ExpressionType.Add;
                    break;
                case TokenType.MINUS:
                    NodeType = ExpressionType.Subtract;
                    break;
                case TokenType.STAR:
                    NodeType = ExpressionType.Multiply;
                    break;
                case TokenType.LESS_THAN:
                    NodeType = ExpressionType.LessThan;
                    break;
                default:
                    throw new ArgumentException("op " + op + " is not a valid operator");
            }

            Lhs = lhs;
            Rhs = rhs;
        }

        public Expression Lhs { get; }

        public Expression Rhs { get; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitBinaryExprAST(ctx, this);
        }
    }
}