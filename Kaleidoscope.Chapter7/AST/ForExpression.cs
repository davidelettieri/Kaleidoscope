﻿namespace Kaleidoscope.AST
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

        public string VarName { get; private set; }

        public Expression Start { get; private set; }

        public Expression End { get; private set; }

        public Expression? Step { get; private set; }

        public Expression Body { get; private set; }

        public override ExpressionType NodeType { get; protected set; }

        public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx)
        {
            return visitor.VisitFor(ctx, this);
        }
    }
}