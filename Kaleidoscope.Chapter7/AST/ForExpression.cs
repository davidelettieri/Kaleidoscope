namespace Kaleidoscope.AST;

public sealed class ForExpression(string varName, Expression start, Expression end, Expression? step, Expression body) : Expression
{
    public string VarName { get; private set; } = varName;
    public Expression Start { get; private set; } = start;
    public Expression End { get; private set; } = end;
    public Expression? Step { get; private set; } = step;
    public Expression Body { get; private set; } = body;


    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitFor(ctx, this);
    }
}
