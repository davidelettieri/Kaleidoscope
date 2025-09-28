using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public interface IExpressionVisitor
{
    (Context, LLVMValueRef) VisitBinary(Context ctx, BinaryExpression expr);
    (Context, LLVMValueRef) VisitCall(Context ctx, CallExpression expr);
    (Context, LLVMValueRef) VisitFor(Context ctx, ForExpression expr);
    (Context, LLVMValueRef) VisitFunction(Context ctx, FunctionExpression expr);
    (Context, LLVMValueRef) VisitIf(Context ctx, IfExpression expr);
    (Context, LLVMValueRef) VisitNumber(Context ctx, NumberExpression expr);
    (Context, LLVMValueRef) VisitPrototype(Context ctx, PrototypeExpression expr);
    (Context, LLVMValueRef) VisitVariable(Context ctx, VariableExpression expr);
    (Context, LLVMValueRef) VisitExtern(Context ctx, ExternExpression expr);
    (Context, LLVMValueRef) VisitUnary(Context context, UnaryExpression expr);
    (Context, LLVMValueRef) VisitVarInExpression(Context context, VarInExpression expr);
}
