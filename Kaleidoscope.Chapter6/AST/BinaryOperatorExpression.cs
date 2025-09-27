namespace Kaleidoscope.AST;

using System.Collections.Generic;

public sealed class BinaryOperatorExpression(string name, double precedence, List<string> args) : PrototypeExpression("binary_" + name, args)
{
    public double Precedence { get; } = precedence;
    public override ExpressionType NodeType { get; protected set; } = ExpressionType.BinaryOperator;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx)
    {
        return visitor.VisitPrototype(ctx, this);
    }
}
