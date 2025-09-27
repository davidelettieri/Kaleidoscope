namespace Kaleidoscope.AST;

using System.Collections.Generic;

public sealed class BinaryOperatorExpression(string name, double precedence, List<string> args) : PrototypeExpression("binary_" + name, args)
{
    public double Precedence { get; } = precedence;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitPrototype(ctx, this);
}
