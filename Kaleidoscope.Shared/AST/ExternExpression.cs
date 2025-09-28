using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class ExternExpression(PrototypeExpression proto) : Expression
{
    public PrototypeExpression Proto { get; } = proto;

    public ExpressionType NodeType { get; } = ExpressionType.Extern;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitExtern(this);
}
