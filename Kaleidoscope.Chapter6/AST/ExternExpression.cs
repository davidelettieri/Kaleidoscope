namespace Kaleidoscope.AST
{
    public sealed class ExternExpression : Expression
    {
        public ExternExpression(PrototypeExpression proto)
        {
            Proto = proto;
            NodeType = ExpressionType.Extern;
        }

        public PrototypeExpression Proto { get; }

        public ExpressionType NodeType { get; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitExtern(ctx, this);
        }
    }
}