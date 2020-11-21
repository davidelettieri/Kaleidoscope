using Kaleidoscope.AST;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Kaleidoscope
{
    public interface IToken { }

    public static class TOKEN
    {
        public static readonly EOF EOF = new EOF();
        public static readonly DEF DEF = new DEF();
        public static readonly EXTERN EXTERN = new EXTERN();
        public static IDENTIFIER IDENTIFIER(string value) => new IDENTIFIER(value);
        public static NUMBER NUMBER(double value) => new NUMBER(value);
        public static OPERATOR OPERATOR(char value) => new OPERATOR(value);
    }

    public class EOF : IToken
    {

    }
    public class DEF : IToken
    {

    }

    public class EXTERN : IToken
    {
    }

    public class IDENTIFIER : IToken
    {
        public string Value { get; }
        public IDENTIFIER(string value)
        {
            Value = value;
        }
    }

    public class NUMBER : IToken
    {
        public double Value { get; }
        public NUMBER(double value)
        {
            Value = value;
        }
    }

    public class OPERATOR : IToken
    {
        public char Value { get; }
        public OPERATOR(char value)
        {
            Value = value;
        }
    }

    public class Scanner
    {
        private readonly TextReader _reader;
        private readonly StringBuilder _builder = new StringBuilder();

        public Scanner(TextReader reader)
        {
            _reader = reader;
        }

        public IToken ScanToken()
        {
            var c = Read();

            switch (c)
            {
                case '\n':
                case '\r':
                case '\t':
                case ' ':
                    return ScanToken();
                case '#':
                    do
                    {
                        c = Read();
                    } while (c != -1 && c != '\n' && c != '\r');

                    if (c != -1)
                        return ScanToken();
                    return TOKEN.EOF;
                default:
                    if (char.IsLetter(c))
                    {
                        _builder.Append(c);
                        c = Read();
                        while (char.IsLetterOrDigit(c))
                        {
                            _builder.Append(c);
                            c = Read();
                        }

                        var identifier = _builder.ToString();
                        _builder.Clear();

                        if (string.Equals(identifier, "def", StringComparison.Ordinal))
                        {
                            return TOKEN.DEF;
                        }
                        if (string.Equals(identifier, "extern", StringComparison.Ordinal))
                        {
                            return TOKEN.EXTERN;
                        }

                        return TOKEN.IDENTIFIER(identifier);
                    }

                    if (char.IsDigit(c))
                    {
                        do
                        {
                            _builder.Append(c);
                            c = Read();
                        } while (char.IsDigit(c));

                        if (Peek() == '.')
                        {
                            c = Read();
                            _builder.Append(c);

                            while (char.IsDigit(c = Read()))
                            {
                                _builder.Append(c);
                            }

                            var num = double.Parse(_builder.ToString(), CultureInfo.InvariantCulture);
                            _builder.Clear();
                            return TOKEN.NUMBER(num);
                        }
                    }

                    return TOKEN.OPERATOR(c);
            }
        }

        private char Peek() => (char)_reader.Peek();
        private char Read() => (char)_reader.Read();
    }

    public class Parser
    {
        private readonly Scanner _scanner;
        private IToken _current;

        public Parser(Scanner scanner)
        {
            _scanner = scanner;
        }

        private FunctionAST Parse()
        {

        }


        private ExprAST ParseNumberExpr(NUMBER n) => new NumberExprAST(n.Value);
    }
}
