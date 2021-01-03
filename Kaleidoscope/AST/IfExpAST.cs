namespace Kaleidoscope.AST
{
    public sealed class IfExpAST : ExprAST
    {
        public IfExpAST(ExprAST condition, ExprAST then, ExprAST @else)
        {
            this.Condition = condition;
            this.Then = then;
            this.Else = @else;
            this.NodeType = ExprType.IfExpr;
        }

        public ExprAST Condition { get; private set; }

        public ExprAST Then { get; private set; }

        public ExprAST Else { get; private set; }

        public override ExprType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExprVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitIfExpAST(ctx, this);
        }
    }
}