namespace Kaleidoscope.AST;

public sealed class UnaryExpression(Token @operator, Expression operand) : Expression
{
    public Token Operator { get; } = @operator;
    public Expression Operand { get; } = operand;

    public override TResult Accept<TResult, TContext>(ExpressionVisitor<TResult, TContext> visitor, TContext ctx) => visitor.VisitUnary(ctx, this);
}
