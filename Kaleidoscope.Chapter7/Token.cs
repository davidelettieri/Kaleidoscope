namespace Kaleidoscope;

public class Token(TokenType type, string lexeme, int line, object? value = null)
{
    public TokenType Type { get; } = type;
    public string Lexeme { get; } = lexeme;
    public int Line { get; } = line;
    public object? Value { get; } = value;
}
