using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class FunctionExpression(PrototypeExpression proto, Expression body) : Expression
{
    public PrototypeExpression Proto { get; } = proto;
    public Expression Body { get; } = body;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx) => visitor.VisitFunction(ctx, this);
}
