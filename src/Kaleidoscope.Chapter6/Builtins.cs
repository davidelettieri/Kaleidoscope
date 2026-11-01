using Kaleidoscope.Shared.AST;

namespace Kaleidoscope;

/// <summary>
/// Runtime functions implemented in C# and resolved by the JIT.
/// Every Kaleidoscope function returns a double; builtins return 0.
/// </summary>
internal static class Builtins
{
    internal const string PutChar = "putchard";

    internal static readonly PrototypeExpression[] Prototypes =
    [
        new(PutChar, ["ch"]),
    ];
}
