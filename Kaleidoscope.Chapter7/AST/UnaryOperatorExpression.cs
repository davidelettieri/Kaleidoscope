using System.Collections.Generic;

namespace Kaleidoscope.AST
{
    public sealed class UnaryOperatorExpression : PrototypeExpression
    {
        public UnaryOperatorExpression(string name, List<string> args) : base("unary_" + name, args)
        {
        }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitPrototype(ctx, this);
        }
    }
}