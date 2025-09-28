using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class IfExpression(Expression condition, Expression then, Expression @else) : Expression
{
    public Expression Condition { get; } = condition;
    public Expression Then { get; } = then;
    public Expression Else { get; } = @else;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx) => visitor.VisitIf(ctx, this);
}
