namespace Kaleidoscope.Shared.AST;

public interface IExpressionVisitor
{
    LLVMValueRef VisitBinary(BinaryExpression expr);
    LLVMValueRef VisitCall(CallExpression expr);
    LLVMValueRef VisitFor(ForExpression expr);
    LLVMValueRef VisitFunction(FunctionExpression expr);
    LLVMValueRef VisitIf(IfExpression expr);
    LLVMValueRef VisitNumber(NumberExpression expr);
    LLVMValueRef VisitPrototype(PrototypeExpression expr);
    LLVMValueRef VisitVariable(VariableExpression expr);
    LLVMValueRef VisitExtern(ExternExpression expr);
    LLVMValueRef VisitUnary(UnaryExpression expr);
    LLVMValueRef VisitVarInExpression(VarInExpression expr);
}
