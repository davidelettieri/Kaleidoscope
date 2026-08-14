namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public sealed record BinaryOperatorExpression : PrototypeExpression
{
    public double Precedence { get; init; }
    public override ExpressionType NodeType => ExpressionType.BinaryOperator;

    public BinaryOperatorExpression(string name, double precedence, List<string> arguments)
        : base("binary_" + name, arguments)
    {
        Precedence = precedence;
    }

    public override LLVMValueRef Accept(IExpressionVisitor visitor) => visitor.VisitPrototype(this);
}
