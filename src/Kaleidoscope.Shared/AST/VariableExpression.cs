namespace Kaleidoscope.Shared.AST;

public sealed record VariableExpression(string Name) : Expression
{
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitVariable(this);
}
