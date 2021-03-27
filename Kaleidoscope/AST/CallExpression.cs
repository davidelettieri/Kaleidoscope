namespace Kaleidoscope.AST
{
    using System.Collections.Generic;

    public sealed class CallExpression : Expression
    {
        public CallExpression(string callee, List<Expression> args)
        {
            this.Callee = callee;
            this.Arguments = args;
            this.NodeType = ExpressionType.Call;
        }

        public string Callee { get; private set; }

        public List<Expression> Arguments { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitCall(ctx, this);
        }
    }
}