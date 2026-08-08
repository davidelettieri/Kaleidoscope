namespace Kaleidoscope.Shared.AST;

public sealed record UnaryOperatorExpression(string Name, List<string> Arguments) : PrototypeExpression("unary_" + Name, Arguments)
{
    public string Argument => Arguments[0];
    public override ExpressionType NodeType { get; } = ExpressionType.UnaryOperator;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
