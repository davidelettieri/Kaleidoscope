namespace Kaleidoscope.AST;

public sealed class VarInExpression(string name, Expression? value, Expression body) : Expression
{
    public string Name { get; } = name;
    public Expression? Value { get; } = value;
    public Expression Body { get; } = body;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitVarInExpression(ctx, this);
}
