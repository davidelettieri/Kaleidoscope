using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class VarInExpression(string name, Expression? value, Expression body) : Expression
{
    public string Name { get; } = name;
    public Expression? Value { get; } = value;
    public Expression Body { get; } = body;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitVarInExpression(this);
}
