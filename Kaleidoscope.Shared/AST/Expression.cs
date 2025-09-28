namespace Kaleidoscope.Shared.AST;

public abstract class Expression
{
    public abstract LLVMValueRef Accept(IExpressionVisitor visitor);
}
