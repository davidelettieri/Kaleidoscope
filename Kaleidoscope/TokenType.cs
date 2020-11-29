namespace Kaleidoscope
{
    public enum TokenType
    {
        EOF,
        DEF,
        EXTERN,
        IDENTIFIER,
        NUMBER,
        IF,
        THEN,
        ELSE,
        FOR,
        IN,
        RIGHT_PAREN,
        LEFT_PAREN,
        EQUAL,
        COMMA,
        STAR,
        MINUS,
        PLUS,
        LESS_THAN
    }

    public interface IToken
    {
        TokenType Type { get; }
    }

    public class Token : IToken
    {
        public TokenType Type { get; }
        public int Start { get; }
        public int Length { get; }
        public int Line { get; }

        public Token(TokenType type, int start, int length, int line)
        {
            Type = type;
            Start = start;
            Length = length;
            Line = line;
        }
    }

    public class NumberToken : Token
    {
        public double Value { get; }

        public NumberToken(double value, int start, int length, int line) 
            : base(TokenType.NUMBER, start, length, line)
        {
            Value = value;
        }
    }

    public class IdentifierToken : Token
    {
        public string Value { get; }

        public IdentifierToken(string value, int start, int length, int line)
             : base(TokenType.IDENTIFIER, start, length, line)
        {
            Value = value;
        }
    }
}