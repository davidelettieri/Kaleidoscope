namespace Kaleidoscope.AST;

public sealed class ForExpression(string varName, Expression start, Expression end, Expression? step, Expression body) : Expression
{
    public string VarName { get; } = varName;

    public Expression Start { get; } = start;

    public Expression End { get; } = end;

    public Expression? Step { get; } = step;

    public Expression Body { get; } = body;

    public ExpressionType NodeType { get; } = ExpressionType.For;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitFor(ctx, this);
    }
}