using System.Collections.Generic;

namespace Kaleidoscope.AST
{
    public sealed class UnaryOperatorExpression : PrototypeExpression
    {
        public UnaryOperatorExpression(string name, List<string> args) : base("unary_" + name, args)
        {
            NodeType = ExpressionType.UnaryOperator;
        }

        public string Argument { get => Arguments[0]; }
        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitPrototype(ctx, this);
        }
    }
}