namespace Kaleidoscope.AST;

public sealed class ExternExpression(PrototypeExpression proto) : Expression
{
    public PrototypeExpression Proto { get; } = proto;

    public ExpressionType NodeType { get; } = ExpressionType.Extern;

    public override TResult Accept<TResult, TContext>(IExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitExtern(ctx, this);
}
