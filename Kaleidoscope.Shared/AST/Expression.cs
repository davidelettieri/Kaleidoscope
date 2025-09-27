namespace Kaleidoscope.Shared.AST;

public abstract class Expression
{
    public abstract TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx);
}
