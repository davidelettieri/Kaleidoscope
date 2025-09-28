using Kaleidoscope.Shared;
using Kaleidoscope.Shared.AST;

namespace Kaleidoscope.Chapter6.Tests;

public class InterpreterTests
{
    private readonly Interpreter _sut = new();
    private readonly Parser _parser = new();
    private readonly StringWriter _stringWriter = new();

    public InterpreterTests() => Console.SetOut(_stringWriter);

    [Theory]
    [InlineData("1+5;", "6")]
    [InlineData("1-5;", "-4")]
    [InlineData("10*2;", "20")]
    [InlineData("10<2;", "0")]
    [InlineData("def unary!(v) if v then 0 else 1;!10;", "0")]
    [InlineData("if 1 then 2 else 3;", "2")]
    [InlineData("if 0 then 2 else 3;", "3")]
    [InlineData("def foo(x, y) x + y * 2; foo(40, 1);", "42")]
    [InlineData("def bar(a, b) a * b; bar(6, 8);", "48")]
    [InlineData("def baz() 34 * 2; baz();", "68")]
    [InlineData("def fib(n) if n < 2 then n else fib(n-1) + fib(n-2); fib(6);", "8")]
    [InlineData("def answer() 50; answer();", "50")]
    public void ExecutingASTProduceExpectedOutput(string source, string expected)
    {
        var ast = Parse(source);

        _sut.Run(ast);

        AssertSingleLineOutput(expected);
    }

    private void AssertSingleLineOutput(string expected)
         => Assert.Equal($"> {expected}\r\n", _stringWriter.ToString());

    private List<Expression> Parse(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();
        var expressions = _parser.Parse(tokens);
        if (expressions is null)
        {
            throw new InvalidOperationException("Parsing failed.");
        }
        return expressions;
    }
}
