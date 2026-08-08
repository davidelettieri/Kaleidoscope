namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public sealed record BinaryOperatorExpression(string Name, double Precedence, List<string> Arguments) : PrototypeExpression("binary_" + Name, Arguments)
{
    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
