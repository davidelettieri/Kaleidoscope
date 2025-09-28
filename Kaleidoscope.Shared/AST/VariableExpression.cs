using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class VariableExpression(string name) : Expression
{
    public string Name { get; } = name;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitVariable(this);
}
