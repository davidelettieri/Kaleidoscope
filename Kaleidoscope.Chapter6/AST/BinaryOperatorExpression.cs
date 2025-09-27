namespace Kaleidoscope.AST;

using System.Collections.Generic;

public sealed class BinaryOperatorExpression(string name, double precedence, List<string> args) : PrototypeExpression("binary_" + name, args)
{
    public double Precedence { get; } = precedence;
    public override ExpressionType NodeType { get;  } = ExpressionType.BinaryOperator;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitPrototype(ctx, this);
}
