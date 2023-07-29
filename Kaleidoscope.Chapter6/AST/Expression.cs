namespace Kaleidoscope.AST
{
    public abstract class Expression
    {
        public abstract ExpressionType NodeType { get; protected set; }

        public abstract TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx);
    }
}