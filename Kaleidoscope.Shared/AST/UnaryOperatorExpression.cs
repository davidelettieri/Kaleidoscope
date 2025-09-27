namespace Kaleidoscope.Shared.AST;

public sealed class UnaryOperatorExpression(string name, List<string> args) : PrototypeExpression("unary_" + name, args)
{
    public string Argument { get => Arguments[0]; }
    public override ExpressionType NodeType { get; } = ExpressionType.UnaryOperator;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitPrototype(ctx, this);
}
