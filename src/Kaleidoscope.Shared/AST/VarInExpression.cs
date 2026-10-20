namespace Kaleidoscope.Shared.AST;

public sealed record VarInExpression(string Name, Expression? Value, Expression Body) : Expression
{
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitVarInExpression(this);
}
