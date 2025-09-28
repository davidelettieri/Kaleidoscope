using Kaleidoscope.Shared.Tests;

namespace Kaleidoscope.Chapter7.Tests;

public class InterpreterTests : InterpreterTestBase
{
    private readonly Interpreter _sut = new();

    [Theory]
    [InlineData("1+5;", "6")]
    [InlineData("1-5;", "-4")]
    [InlineData("10*2;", "20")]
    [InlineData("10<2;", "0")]
    [InlineData("10==2;", "0")]
    [InlineData("10==10;", "1")]
    [InlineData("2 + 3 * 4;", "14")]
    [InlineData("2 * (3 + 4);", "14")]
    [InlineData("def binary : 1 (x,y) y;var sum = 0 in ((for i = 0, i < 11 in sum = sum +i): sum);", "55")]
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
}
