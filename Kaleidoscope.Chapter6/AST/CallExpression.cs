namespace Kaleidoscope.AST
{
    using System.Collections.Generic;

    public sealed class CallExpression : Expression
    {
        public CallExpression(string callee, List<Expression> args)
        {
            Callee = callee;
            Arguments = args;
            NodeType = ExpressionType.Call;
        }

        public string Callee { get; }

        public List<Expression> Arguments { get; }

        public ExpressionType NodeType { get; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitCall(ctx, this);
        }
    }
}