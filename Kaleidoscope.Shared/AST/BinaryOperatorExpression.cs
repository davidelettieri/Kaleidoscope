namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public sealed class BinaryOperatorExpression(string name, double precedence, List<string> args) : PrototypeExpression("binary_" + name, args)
{
    public double Precedence { get; } = precedence;
    public override ExpressionType NodeType { get; } = ExpressionType.BinaryOperator;

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
