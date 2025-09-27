namespace Kaleidoscope.AST;

public sealed class ForExpression(string varName, Expression start, Expression end, Expression? step, Expression body) : Expression
{
    public string VarName { get; } = varName;
    public Expression Start { get; } = start;
    public Expression End { get; } = end;
    public Expression? Step { get; } = step;
    public Expression Body { get; } = body;


    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitFor(ctx, this);
}
