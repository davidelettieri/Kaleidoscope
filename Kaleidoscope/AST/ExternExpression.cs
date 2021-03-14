namespace Kaleidoscope.AST
{
    public sealed class ExternExpression : Expression
    {
        public ExternExpression(PrototypeExpression proto)
        {
            this.Proto = proto;
            this.NodeType = ExpressionType.Extern;
        }

        public PrototypeExpression Proto { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitExternAST(ctx, this);
        }
    }
}