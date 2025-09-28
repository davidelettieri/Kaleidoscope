using LLVMSharp.Interop;

namespace Kaleidoscope.Shared.AST;

public abstract class Expression
{
    public abstract (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx);
}
