namespace Kaleidoscope.AST
{
    public sealed class ExternExpression : Expression
    {
        public ExternExpression(PrototypeExpression proto)
        {
            Proto = proto;
        }

        public PrototypeExpression Proto { get; private set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitExtern(ctx, this);
        }
    }
}