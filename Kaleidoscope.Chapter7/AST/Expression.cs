namespace Kaleidoscope.AST
{
    public abstract class Expression
    {
        public abstract TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx);
    }
}