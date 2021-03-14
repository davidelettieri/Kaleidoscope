namespace Kaleidoscope.AST
{
    using System.Collections.Generic;

    public sealed class PrototypeExpression : Expression
    {
        public PrototypeExpression(string name, List<string> args)
        {
            this.Name = name;
            this.Arguments = args;
            this.NodeType = ExpressionType.Prototype;
        }

        public string Name { get; private set; }

        public List<string> Arguments { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitPrototypeAST(ctx, this);
        }
    }
}