namespace Kaleidoscope.Shared.AST;

public sealed record UnaryOperatorExpression : PrototypeExpression
{
    public string Argument => Arguments[0];
    public override ExpressionType NodeType { get; } = ExpressionType.UnaryOperator;

    public UnaryOperatorExpression(string name, List<string> arguments)
        : base("unary_" + name, arguments)
    {
    }

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
