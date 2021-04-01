namespace Kaleidoscope.AST
{
    using System.Collections.Generic;

    public class PrototypeExpression : Expression
    {
        public PrototypeExpression(string name, List<string> args)
        {
            Name = name;
            Arguments = args;
            NodeType = ExpressionType.Prototype;
        }

        public string Name { get; }
        public List<string> Arguments { get; }
        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitPrototype(ctx, this);
        }
    }
}