namespace Kaleidoscope.Shared.AST;

public sealed record FunctionExpression(PrototypeExpression Proto, Expression Body) : Expression
{
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitFunction(this);
}
