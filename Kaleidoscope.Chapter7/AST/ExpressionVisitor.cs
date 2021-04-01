namespace Kaleidoscope.AST
{
    public interface ExpressionVisitor<TResult, TContext>
    {
        TResult VisitBinary(TContext ctx, BinaryExpression expr);
        TResult VisitCall(TContext ctx, CallExpression expr);
        TResult VisitFor(TContext ctx, ForExpression expr);
        TResult VisitFunction(TContext ctx, FunctionExpression expr);
        TResult VisitIf(TContext ctx, IfExpression expr);
        TResult VisitNumber(TContext ctx, NumberExpression expr);
        TResult VisitPrototype(TContext ctx, PrototypeExpression expr);
        TResult VisitVariable(TContext ctx, VariableExpression expr);
        TResult VisitExtern(TContext ctx, ExternExpression expr);
        TResult VisitUnary(TContext context, UnaryExpression expr);
        TResult VisitVarInExpression(TContext context, VarInExpression expr);
    }
}