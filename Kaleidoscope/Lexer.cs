namespace Kaleidoscope
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public sealed class Lexer
    {
        private const int EOF = -1;
        private readonly TextReader _reader;
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly Dictionary<char, int> _binopPrecedence;
        private readonly Dictionary<string, int> _keywords = new Dictionary<string, int>()
        {
            { "def", Token.DEF },
            { "extern", Token.EXTERN },
            { "if", Token.IF },
            { "then", Token.THEN },
            { "else", Token.ELSE },
            { "for", Token.FOR },
            { "in", Token.IN },
        };

        private int c = ' ';

        private string identifier;

        private double numVal;

        public Lexer(TextReader reader, Dictionary<char, int> binOpPrecedence)
        {
            _reader = reader;
            _binopPrecedence = binOpPrecedence;
        }

        public int CurrentToken { get; private set; }

        public string GetLastIdentifier()
        {
            return identifier;
        }

        public double GetLastNumber()
        {
            return numVal;
        }

        public int GetTokPrecedence()
        {
            // Make sure it's a declared binop.
            int tokPrec;
            if (_binopPrecedence.TryGetValue((char)CurrentToken, out tokPrec))
            {
                return tokPrec;
            }

            return -1;
        }

        public int GetNextToken()
        {
            // Skip any whitespace.
            while (char.IsWhiteSpace((char)c))
            {
                c = _reader.Read();
            }

            if (char.IsLetter((char)c)) // identifier: [a-zA-Z][a-zA-Z0-9]*
            {
                _builder.Append((char)c);
                while (char.IsLetterOrDigit((char)(c = _reader.Read())))
                {
                    _builder.Append((char)c);
                }

                identifier = _builder.ToString();
                _builder.Clear();

                if(_keywords.TryGetValue(identifier, out var value))
                {
                    CurrentToken = value;
                }
                else
                {
                    CurrentToken = Token.IDENTIFIER;
                }

                return CurrentToken;
            }

            // Number: [0-9.]+
            if (char.IsDigit((char)c) || c == '.')
            {
                do
                {
                    _builder.Append((char)c);
                    c = _reader.Read();
                } while (char.IsDigit((char)c) || c == '.');
                
                numVal = double.Parse(_builder.ToString());
                _builder.Clear();
                CurrentToken = Token.NUMBER;

                return CurrentToken;
            }

            if (c == '#')
            {
                // Comment until end of line.
                do
                {
                    c = _reader.Read();
                } while (c != EOF && c != '\n' && c != '\r');

                if (c != EOF)
                {
                    return GetNextToken();
                }
            }

            // Check for end of file.  Don't eat the EOF.
            if (c == EOF)
            {
                CurrentToken = c;
                return Token.EOF;
            }

            CurrentToken = c;
            c = _reader.Read();
            return c;
        }
    }
}