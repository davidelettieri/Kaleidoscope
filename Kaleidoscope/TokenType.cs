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
        LESS_THAN,
        SEMICOLON
    }

    public class Token
    {
        public TokenType Type { get; }
        public int Start { get; }
        public int Length { get; }
        public int Line { get; }
        public object Value { get; }

        public Token(TokenType type, int start, int length, int line, object value = null)
        {
            Type = type;
            Start = start;
            Length = length;
            Line = line;
            Value = value;
        }
    }
}