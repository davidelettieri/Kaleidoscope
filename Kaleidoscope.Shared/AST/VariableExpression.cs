using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class VariableExpression(string name) : Expression
{
    public string Name { get; } = name;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx) => visitor.VisitVariable(ctx, this);
}
