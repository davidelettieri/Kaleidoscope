namespace Kaleidoscope.AST
{
    public sealed class ForExpression : Expression
    {
        public ForExpression(string varName, Expression start, Expression end, Expression? step, Expression body)
        {
            VarName = varName;
            Start = start;
            End = end;
            Step = step;
            Body = body;
            NodeType = ExpressionType.For;
        }

        public string VarName { get; }

        public Expression Start { get; }

        public Expression End { get; }

        public Expression? Step { get; }

        public Expression Body { get; }

        public ExpressionType NodeType { get; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitFor(ctx, this);
        }
    }
}