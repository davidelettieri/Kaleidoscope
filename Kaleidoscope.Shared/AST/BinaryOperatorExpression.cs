namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public sealed class BinaryOperatorExpression(string name, double precedence, List<string> args) : PrototypeExpression("binary_" + name, args)
{
    public double Precedence { get; } = precedence;
    public override ExpressionType NodeType { get; } = ExpressionType.BinaryOperator;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx) => visitor.VisitPrototype(ctx, this);
}
