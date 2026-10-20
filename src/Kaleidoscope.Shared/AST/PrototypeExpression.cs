namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public record PrototypeExpression(string Name, List<string> Arguments) : Expression
{
    public virtual ExpressionType NodeType { get; } = ExpressionType.Prototype;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
