namespace Kaleidoscope.Shared.AST;

public sealed record BinaryExpression(Token OperatorToken, Expression Lhs, Expression Rhs) : Expression
{
    public ExpressionType NodeType { get; } = OperatorToken.Lexeme switch
    {
        "+" => ExpressionType.Add,
        "-" => ExpressionType.Subtract,
        "*" => ExpressionType.Multiply,
        "<" => ExpressionType.LessThan,
        "==" => ExpressionType.Equal,
        "=" => ExpressionType.Assign,
        _ => OperatorToken.Type == TokenType.IDENTIFIER
            ? ExpressionType.BinaryOperator
            : throw new ArgumentException($"op {OperatorToken.Type} is not a valid operator")
    };

    public override LLVMValueRef Accept(IExpressionVisitor visitor)
        => visitor.VisitBinary(this);
}
