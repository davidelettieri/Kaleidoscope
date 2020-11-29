using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using static Kaleidoscope.TokenType;

namespace Kaleidoscope
{
    public sealed class Scanner
    {
        private readonly string _source;
        private int _start;
        private int _current;
        private int _line = 1;
        private readonly List<IToken> _tokens = new List<IToken>();
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>()
        {
            { "def", DEF },
            { "extern", EXTERN },
            { "if", IF },
            { "then", THEN },
            { "else", ELSE },
            { "for", FOR },
            { "in", IN },
        };



        public Scanner(string source)
        {
            _source = source;
        }

        public List<IToken> ScanTokens()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                ScanToken();
            }

            AddToken(EOF);

            return _tokens;
        }

        private void ScanToken()
        {
            SkipWhitespace();
            _start = _current;

            char c = Advance();

            switch (c)
            {
                case '=':
                    AddToken(EQUAL);
                    break;
                case '*':
                    AddToken(STAR);
                    break;
                case '-':
                    AddToken(MINUS);
                    break;
                case '+':
                    AddToken(PLUS);
                    break;
                case '<':
                    AddToken(LESS_THAN);
                    break;
                case '(':
                    AddToken(LEFT_PAREN);
                    break;
                case ')':
                    AddToken(RIGHT_PAREN);
                    break;
                case '#':
                    while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
                    {
                        Advance();
                    }
                    break;
                default:
                    if (char.IsLetter(c))
                    {
                        Identifier();
                    }
                    if (char.IsDigit(c))
                    {
                        Number();
                    }
                    break;
            }
        }

        private void Number()
        {
            while (IsDigit(Peek())) Advance();

            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                Advance();

                while (IsDigit(Peek())) Advance();
            }

            AddNumberToken(double.Parse(_source.Substring(_start, _current - _start), CultureInfo.InvariantCulture));
        }

        private void Identifier()
        {
            while (char.IsLetterOrDigit(Peek())) Advance();
            var identifier = _source.Substring(_start, _current - _start);

            if (_keywords.TryGetValue(identifier, out var value))
            {
                AddToken(value);
            }
            else
            {
                AddIdentifierToken(identifier);
            }
        }

        private char Advance()
        {
            _current++;
            return _source[_current - 1];
        }

        private char Peek()
        {
            if (IsAtEnd())
                return '\0';

            return _source[_current];
        }

        private char PeekNext()
        {
            if (_current + 1 >= _source.Length) return '\0';

            return _source[_current + 1];
        }

        void SkipWhitespace()
        {
            while (true)
            {
                var c = Peek();

                switch (c)
                {
                    case ' ':
                    case '\r':
                    case '\t':
                        Advance();
                        break;
                    case '\n':
                        _line++;
                        Advance();
                        break;
                    default:
                        return;
                }
            }
        }

        private bool IsDigit(char c) => char.IsDigit(c);

        void AddToken(TokenType type) => _tokens.Add(new Token(type, _start, _current - _start, _line));
        void AddIdentifierToken(string value) => _tokens.Add(new IdentifierToken(value, _start, _current - _start, _line));
        void AddNumberToken(double value) => _tokens.Add(new NumberToken(value, _start, _current - _start, _line));
        private bool IsAtEnd() => _current >= _source.Length;
    }
}