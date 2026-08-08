namespace Kaleidoscope.Shared.AST;

public sealed record ForExpression(string VarName, Expression Start, Expression End, Expression? Step, Expression Body) : Expression
{
    public ExpressionType NodeType { get; } = ExpressionType.For;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitFor(this);
}
