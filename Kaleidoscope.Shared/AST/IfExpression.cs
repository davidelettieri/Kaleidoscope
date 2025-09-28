using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class IfExpression(Expression condition, Expression then, Expression @else) : Expression
{
    public Expression Condition { get; } = condition;
    public Expression Then { get; } = then;
    public Expression Else { get; } = @else;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitIf(this);
}
