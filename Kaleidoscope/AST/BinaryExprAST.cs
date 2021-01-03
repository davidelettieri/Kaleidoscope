namespace Kaleidoscope.AST
{
    using System;

    public sealed class BinaryExprAST : ExprAST
    {
        public BinaryExprAST(TokenType op, ExprAST lhs, ExprAST rhs)
        {
            switch (op)
            {
                case TokenType.PLUS:
                    NodeType = ExprType.AddExpr;
                    break;
                case TokenType.MINUS:
                    NodeType = ExprType.SubtractExpr;
                    break;
                case TokenType.STAR:
                    NodeType = ExprType.MultiplyExpr;
                    break;
                case TokenType.LESS_THAN:
                    NodeType = ExprType.LessThanExpr;
                    break;
                default:
                    throw new ArgumentException("op " + op + " is not a valid operator");
            }

            Lhs = lhs;
            Rhs = rhs;
        }

        public ExprAST Lhs { get; }

        public ExprAST Rhs { get; }

        public override ExprType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExprVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitBinaryExprAST(ctx, this);
        }
    }
}