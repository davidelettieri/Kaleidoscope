namespace Kaleidoscope.AST
{
    public sealed class VariableExpression : Expression
    {
        public VariableExpression(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitVariable(ctx, this);
        }
    }
}