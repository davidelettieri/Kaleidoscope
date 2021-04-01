namespace Kaleidoscope.AST
{
    using System.Collections.Generic;

    public sealed class BinaryOperatorExpression : PrototypeExpression
    {
        public BinaryOperatorExpression(string name, double precedence, List<string> args) : base("binary_" + name, args)
        {
            Precedence = precedence;
            NodeType = ExpressionType.BinaryOperator;
        }

        public double Precedence { get; }
        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitPrototype(ctx, this);
        }
    }
}