namespace Kaleidoscope
{
    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public int Line { get; }
        public object Value { get; }

        public Token(TokenType type, string lexeme, int line, object value = null)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Value = value;
        }
    }
}