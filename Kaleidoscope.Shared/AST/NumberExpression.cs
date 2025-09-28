using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class NumberExpression(double value) : Expression
{
    public double Value { get; } = value;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx)
        => visitor.VisitNumber(ctx, this);
}
