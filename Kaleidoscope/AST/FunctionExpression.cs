namespace Kaleidoscope.AST
{
    public sealed class FunctionExpression : Expression
    {
        public FunctionExpression(PrototypeExpression proto, Expression body)
        {
            this.Proto = proto;
            this.Body = body;
            this.NodeType = ExpressionType.Function;
        }

        public PrototypeExpression Proto { get; private set; }

        public Expression Body { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitFunctionAST(ctx, this);
        }
    }
}