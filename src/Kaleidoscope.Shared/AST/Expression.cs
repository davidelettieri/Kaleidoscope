namespace Kaleidoscope.Shared.AST;

public abstract record Expression
{
    public abstract LLVMValueRef Accept(IExpressionVisitor visitor);
}
