namespace Kaleidoscope.AST
{
    public sealed class VariableExpression : Expression
    {
        public VariableExpression(string name)
        {
            this.Name = name;
            this.NodeType = ExpressionType.Variable;
        }

        public string Name { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitVariableExprAST(ctx, this);
        }
    }
}