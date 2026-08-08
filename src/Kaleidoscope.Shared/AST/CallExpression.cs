namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public sealed record CallExpression(string Callee, List<Expression> Arguments) : Expression
{
    public ExpressionType NodeType { get; } = ExpressionType.Call;
    
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitCall(this);
}
