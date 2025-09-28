namespace Kaleidoscope.Shared.AST;

using LLVMSharp.Interop;
using System.Collections.Generic;

public class PrototypeExpression(string name, List<string> args) : Expression
{
    public string Name { get; } = name;
    public List<string> Arguments { get; } = args;
    public virtual ExpressionType NodeType { get; } = ExpressionType.Prototype;

    public override (Context, LLVMValueRef) Accept(IExpressionVisitor visitor, Context ctx) => visitor.VisitPrototype(ctx, this);
}
