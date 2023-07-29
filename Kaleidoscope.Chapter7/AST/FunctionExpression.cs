namespace Kaleidoscope.AST
{
    public sealed class FunctionExpression : Expression
    {
        public FunctionExpression(PrototypeExpression proto, Expression body)
        {
            Proto = proto;
            Body = body;
        }

        public PrototypeExpression Proto { get; private set; }
        public Expression Body { get; private set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitFunction(ctx, this);
        }
    }
}