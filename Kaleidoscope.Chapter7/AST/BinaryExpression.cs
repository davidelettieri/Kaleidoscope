namespace Kaleidoscope.AST
{
    using System;

    public sealed class BinaryExpression : Expression
    {
        public BinaryExpression(Token token, Expression lhs, Expression rhs)
        {
            OperatorToken = token;
            switch (token.Lexeme)
            {
                case "+":
                    NodeType = ExpressionType.Add;
                    break;
                case "-":
                    NodeType = ExpressionType.Subtract;
                    break;
                case "*":
                    NodeType = ExpressionType.Multiply;
                    break;
                case "<":
                    NodeType = ExpressionType.LessThan;
                    break;
                case "==":
                    NodeType = ExpressionType.Equal;
                    break;
                case "=":
                    NodeType = ExpressionType.Assign;
                    break;
                default:
                    if (token.Type == TokenType.IDENTIFIER)
                        NodeType = ExpressionType.BinaryOperator;
                    else
                        throw new ArgumentException("op " + token.Type + " is not a valid operator");
                    break;
            }

            Lhs = lhs;
            Rhs = rhs;
        }

        public Expression Lhs { get; }
        public Expression Rhs { get; }
        public Token OperatorToken { get; }
        public ExpressionType NodeType { get; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitBinary(ctx, this);
        }
    }
}