using System.Collections.Generic;

namespace Kaleidoscope.AST;

public sealed class UnaryOperatorExpression(string name, List<string> args) : PrototypeExpression("unary_" + name, args)
{
    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitPrototype(ctx, this);
}
