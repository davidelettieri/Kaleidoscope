namespace Kaleidoscope.AST
{
    public abstract class ExprAST
    {
        public abstract ExprType NodeType { get; protected set; }

        public abstract TResult Accept<TResult, TContext>(ExprVisitor<TResult, TContext> visitor, TContext ctx);
    }
}