using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class UnaryOperatorExpression(string name, List<string> args) : PrototypeExpression("unary_" + name, args)
{
    public string Argument => Arguments[0];
    public override ExpressionType NodeType { get; } = ExpressionType.UnaryOperator;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
