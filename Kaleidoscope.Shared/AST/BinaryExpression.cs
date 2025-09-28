using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class BinaryExpression : Expression
{
    public BinaryExpression(Token token, Expression lhs, Expression rhs)
    {
        OperatorToken = token;
        NodeType = token.Lexeme switch
        {
            "+" => ExpressionType.Add,
            "-" => ExpressionType.Subtract,
            "*" => ExpressionType.Multiply,
            "<" => ExpressionType.LessThan,
            "==" => ExpressionType.Equal,
            "=" => ExpressionType.Assign,
            _ => token.Type == TokenType.IDENTIFIER
                    ? ExpressionType.BinaryOperator
                    : throw new ArgumentException($"op {token.Type} is not a valid operator")
        };
        Lhs = lhs;
        Rhs = rhs;
    }

    public Expression Lhs { get; }
    public Expression Rhs { get; }
    public Token OperatorToken { get; }
    public ExpressionType NodeType { get; }

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx)
        => visitor.VisitBinary(ctx, this);
}
