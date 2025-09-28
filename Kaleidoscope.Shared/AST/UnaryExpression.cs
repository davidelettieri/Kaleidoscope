namespace Kaleidoscope.Shared.AST;

public sealed class UnaryExpression(Token @operator, Expression operand) : Expression
{
    public Token Operator { get; } = @operator;
    public Expression Operand { get; } = operand;
    public ExpressionType NodeType { get; } = ExpressionType.Unary;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitUnary(this);
}
