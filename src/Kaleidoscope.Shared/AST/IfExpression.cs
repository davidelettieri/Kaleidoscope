namespace Kaleidoscope.Shared.AST;

public sealed record IfExpression(Expression Condition, Expression Then, Expression Else) : Expression
{
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitIf(this);
}
