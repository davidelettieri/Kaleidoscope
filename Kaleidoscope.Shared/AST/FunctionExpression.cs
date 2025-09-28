namespace Kaleidoscope.Shared.AST;

public sealed class FunctionExpression(PrototypeExpression proto, Expression body) : Expression
{
    public PrototypeExpression Proto { get; } = proto;
    public Expression Body { get; } = body;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitFunction(this);
}
