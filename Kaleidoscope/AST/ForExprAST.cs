namespace Kaleidoscope.AST
{
    public sealed class ForExprAST : ExprAST
    {
        public ForExprAST(string varName, ExprAST start, ExprAST end, ExprAST step, ExprAST body)
        {
            this.VarName = varName;
            this.Start = start;
            this.End = end;
            this.Step = step;
            this.Body = body;
            this.NodeType = ExprType.ForExpr;
        }

        public string VarName { get; private set; }

        public ExprAST Start { get; private set; }

        public ExprAST End { get; private set; }

        public ExprAST Step { get; private set; }

        public ExprAST Body { get; private set; }

        public override ExprType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExprVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitForExprAST(ctx, this);
        }
    }
}