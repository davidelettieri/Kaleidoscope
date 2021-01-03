namespace Kaleidoscope.AST
{
    public sealed class NumberExprAST : ExprAST
    {
        public NumberExprAST(double value)
        {
            this.Value = value;
            this.NodeType = ExprType.NumberExpr;
        }

        public double Value { get; private set; }

        public override ExprType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExprVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitNumberExprAST(ctx,this);
        }
    }
}