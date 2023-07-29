namespace Kaleidoscope.AST
{
    public sealed class VarInExpression : Expression
    {
        public VarInExpression(string name, Expression? value, Expression body)
        {
            Name = name;
            Value = value;
            Body = body;
        }

        public string Name { get; }
        public Expression? Value { get; }
        public Expression Body { get; }
        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitVarInExpression(ctx, this);
        }
    }
}