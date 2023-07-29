namespace Kaleidoscope.AST
{
    using System.Collections.Generic;

    public sealed class CallExpression : Expression
    {
        public CallExpression(string callee, List<Expression> args)
        {
            Callee = callee;
            Arguments = args;
        }

        public string Callee { get; private set; }
        public List<Expression> Arguments { get; private set; }


        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitCall(ctx, this);
        }
    }
}