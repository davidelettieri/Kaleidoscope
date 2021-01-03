namespace Kaleidoscope.AST
{
    public sealed class VariableExprAST : ExprAST
    {
        public VariableExprAST(string name)
        {
            this.Name = name;
            this.NodeType = ExprType.VariableExpr;
        }

        public string Name { get; private set; }

        public override ExprType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExprVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitVariableExprAST(ctx, this);
        }
    }
}