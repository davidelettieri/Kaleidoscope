using Kaleidoscope.Shared.AST;
using Xunit;

namespace Kaleidoscope.Shared.Tests;

// This could be placed in a new file, e.g. InterpreterTestBase.cs
public abstract class InterpreterTestBase : IDisposable
{
    protected readonly Parser Parser = new();
    private readonly StringWriter _stringWriter = new();
    private readonly TextWriter _originalOut;

    protected InterpreterTestBase()
    {
        _originalOut = Console.Out;
        Console.SetOut(_stringWriter);
    }

    protected void AssertSingleLineOutput(string expected)
         => Assert.Equal($"> {expected}{Environment.NewLine}", _stringWriter.ToString());

    protected List<Expression> Parse(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();
        var expressions = Parser.Parse(tokens);
        if (expressions is null)
        {
            // The parser prints the error to Console.Out, which is our StringWriter.
            // We throw an exception here to fail the test with the parser's error message.
            throw new InvalidOperationException($"Parsing failed with output:\n{_stringWriter.ToString()}");
        }
        return expressions;
    }

    public void Dispose()
    {
        _stringWriter.Dispose();
        Console.SetOut(_originalOut);
    }
}
