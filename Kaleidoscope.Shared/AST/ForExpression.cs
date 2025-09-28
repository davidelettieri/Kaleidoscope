using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public sealed class ForExpression(string varName, Expression start, Expression end, Expression? step, Expression body) : Expression
{
    public string VarName { get; } = varName;

    public Expression Start { get; } = start;

    public Expression End { get; } = end;

    public Expression? Step { get; } = step;

    public Expression Body { get; } = body;

    public ExpressionType NodeType { get; } = ExpressionType.For;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitFor(this);
}
