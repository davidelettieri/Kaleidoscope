namespace Kaleidoscope.Shared.AST;

public sealed class NumberExpression(double value) : Expression
{
    public double Value { get; } = value;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitNumber(this);
}
