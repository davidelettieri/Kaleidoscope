﻿namespace Kaleidoscope.AST
{
    public sealed class VariableExpression : Expression
    {
        public VariableExpression(string name)
        {
            Name = name;
            NodeType = ExpressionType.Variable;
        }

        public string Name { get; }
        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitVariable(ctx, this);
        }
    }
}