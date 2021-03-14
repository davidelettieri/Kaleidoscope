namespace Kaleidoscope.AST
{
    public interface ExpressionVisitor<TResult, TContext>
    {
        TResult VisitBinaryExprAST(TContext ctx, BinaryExpression expr);
        TResult VisitCallExprAST(TContext ctx, CallExpression expr);
        TResult VisitForExprAST(TContext ctx, ForExpression expr);
        TResult VisitFunctionAST(TContext ctx, FunctionExpression expr);
        TResult VisitIfExpAST(TContext ctx, IfExpression expr);
        TResult VisitNumberExprAST(TContext ctx, NumberExpression expr);
        TResult VisitPrototypeAST(TContext ctx, PrototypeExpression expr);
        TResult VisitVariableExprAST(TContext ctx, VariableExpression expr);
        TResult VisitExternAST(TContext ctx, ExternExpression expr);
    }
}