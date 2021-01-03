namespace Kaleidoscope.AST
{
    public interface ExprVisitor<TResult, TContext>
    {
        TResult VisitBinaryExprAST(TContext ctx, BinaryExprAST expr);
        TResult VisitCallExprAST(TContext ctx, CallExprAST expr);
        TResult VisitForExprAST(TContext ctx, ForExprAST expr);
        TResult VisitFunctionAST(TContext ctx, FunctionAST expr);
        TResult VisitIfExpAST(TContext ctx, IfExpAST expr);
        TResult VisitNumberExprAST(TContext ctx, NumberExprAST expr);
        TResult VisitPrototypeAST(TContext ctx, PrototypeAST expr);
        TResult VisitVariableExprAST(TContext ctx, VariableExprAST expr);
    }
}