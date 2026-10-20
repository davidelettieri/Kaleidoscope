namespace Kaleidoscope.Shared.AST;

public sealed record NumberExpression(double Value) : Expression
{
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitNumber(this);
}
