namespace Kaleidoscope.AST
{
    public sealed class VariableExpression : Expression
    {
        public VariableExpression(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitVariable(ctx, this);
        }
    }
}