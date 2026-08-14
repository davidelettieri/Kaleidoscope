namespace Kaleidoscope.Shared.AST;

public sealed record UnaryExpression(Token Operator, Expression Operand) : Expression
{
    public ExpressionType NodeType { get; } = ExpressionType.Unary;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitUnary(this);
}
