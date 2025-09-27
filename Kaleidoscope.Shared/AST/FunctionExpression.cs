namespace Kaleidoscope.Shared.AST;

public sealed class FunctionExpression(PrototypeExpression proto, Expression body) : Expression
{
    public PrototypeExpression Proto { get; } = proto;
    public Expression Body { get; } = body;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitFunction(ctx, this);
}
