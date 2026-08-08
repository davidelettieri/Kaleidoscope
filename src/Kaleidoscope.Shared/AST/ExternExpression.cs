namespace Kaleidoscope.Shared.AST;

public sealed record ExternExpression(PrototypeExpression Proto) : Expression
{
    public ExpressionType NodeType { get; } = ExpressionType.Extern;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitExtern(this);
}
