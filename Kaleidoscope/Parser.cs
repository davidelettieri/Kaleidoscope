using System;
using System.Collections.Generic;
using Kaleidoscope.AST;
using static Kaleidoscope.TokenType;

namespace Kaleidoscope
{
    class ParseError : Exception
    {
        public ParseError(string message) : base(message)
        {
        }
    }


    public sealed class Parser
    {
        private List<Token> _tokens;
        private readonly Dictionary<TokenType, double> _binaryOperatorPrecedence =
            new Dictionary<TokenType, double>()
            {
                { LESS_THAN, 10 },
                { PLUS, 20 },
                { MINUS, 20 },
                { STAR, 40 },
            };

        private readonly Dictionary<string, double> _customOperatorsPrecedence = new Dictionary<string, double>();
        private readonly HashSet<string> _unaryOperators = new HashSet<string>(StringComparer.Ordinal);


        private double GetPrecedence(Token token)
        {
            if (_binaryOperatorPrecedence.TryGetValue(token.Type, out var p))
            {
                return p;
            }

            if (token.Type == TokenType.IDENTIFIER && _customOperatorsPrecedence.TryGetValue(token.Value.ToString(), out var cp))
            {
                return cp;
            }

            return 0;
        }

        private int _current = 0;

        public List<Expression> Parse(List<Token> tokens)
        {
            _current = 0;
            _tokens = tokens;

            try
            {
                var result = new List<Expression>();

                while (!IsAtEnd())
                {
                    var e = TopLevel();
                    result.Add(e);
                    Consume(SEMICOLON, "Expected semi-colon.");
                }

                return result;
            }
            catch (ParseError error)
            {
                Console.WriteLine(error.Message);
                Syncronize();
                return null;
            }
        }

        private Expression TopLevel()
        {
            if (Match(DEF)) return Definition();
            if (Match(EXTERN)) return Extern();

            var expr = Expression();

            return new FunctionExpression(new PrototypeExpression("", new List<string>()), expr);

        }

        private Expression Expression(double precedence = 0)
        {
            var lhs = Unary();

            while (precedence < GetPrecedence())
            {
                lhs = ParseBinary(lhs);
            }

            return lhs;
        }

        private Expression ParseBinary(Expression lhs)
        {
            var token = Advance();
            var precedence = GetPrecedence(token);
            var rhs = Expression(precedence);
            return new BinaryExpression(token, lhs, rhs);
        }

        private double GetPrecedence()
        {
            var next = Peek();

            return GetPrecedence(next);
        }

        private Expression Unary()
        {
            var next = Peek();
            if (_unaryOperators.Contains(next.Value?.ToString()))
            {
                var @operator = Advance();
                var operand = Unary();
                return new UnaryExpression(@operator, operand);
            }

            return Primary();
        }

        private Expression Primary()
        {
            if (Match(LEFT_PAREN))
            {
                var expr = Expression();
                Consume(RIGHT_PAREN, "Expect ')' after expression");
                return expr;
            }

            if (Match(IDENTIFIER))
            {
                var name = Previous().Value as string;
                if (Match(LEFT_PAREN))
                    return Call(name);
                return new VariableExpression(name);
            }

            if (Match(NUMBER))
            {
                return new NumberExpression((double)Previous().Value);
            }

            if (Match(IF))
            {
                return If();
            }

            if (Match(FOR))
            {
                return For();
            }

            throw Error(Peek(), "Expect expression.");
        }

        private Expression For()
        {
            var id = Identifier();
            Consume(EQUAL, "Expect '=' after identifier");
            var start = Expression();
            Consume(COMMA, "Expect ',' after initial value");
            var end = Expression();
            Expression step = null;
            if (Check(COMMA))
            {
                Consume(COMMA, "");
                step = Expression();
            }

            Consume(IN, "Expect 'in' after for definition");
            var body = Expression();

            return new ForExpression(id, start, end, step, body);
        }

        private Expression If()
        {
            var cond = Expression();
            Consume(THEN, "Expected 'then'");
            var @then = Expression();
            Consume(ELSE, "Expected 'else'");
            var @else = Expression();

            return new IfExpression(cond, @then, @else);
        }

        private Expression Call(string name)
        {
            var args = new List<Expression>();

            if (!Check(RIGHT_PAREN))
            {
                do
                {
                    var arg = Expression();
                    args.Add(arg);
                }
                while (Match(COMMA));
            }

            Consume(RIGHT_PAREN, "Expected ')'");

            return new CallExpression(name, args);
        }

        private Expression Extern() => new ExternExpression(Prototype());

        private Expression Definition()
        {
            var prototype = Prototype();
            var body = Expression();

            return new FunctionExpression(prototype, body);
        }

        public enum PrototypeType
        {
            Function,
            Unary,
            Binary
        }

        private PrototypeExpression Prototype()
        {
            var prototypeType = PrototypeType.Function;
            var precedence = 0.0;

            if (Match(UNARY))
            {
                prototypeType = PrototypeType.Unary;
            }
            else if (Match(BINARY))
            {
                prototypeType = PrototypeType.Binary;
            }

            var name = Identifier();

            if (prototypeType == PrototypeType.Binary)
            {
                Consume(NUMBER, "Expected number after operator identifier.");
                precedence = (double)Previous().Value;
            }

            Consume(LEFT_PAREN, "Expected '('.");
            var args = new List<string>();

            if (!Check(RIGHT_PAREN))
            {
                do
                {
                    var arg = Identifier();
                    args.Add(arg);
                }
                while (Match(COMMA));
            }

            Consume(RIGHT_PAREN, "Expected ')'.");

            if (prototypeType == PrototypeType.Unary)
            {
                if (args.Count != 1)
                {
                    throw new ParseError("Unary operators accept exactly one parameter");
                }

                _unaryOperators.Add(name);

                return new UnaryOperatorExpression(name, args);
            }
            else if (prototypeType == PrototypeType.Binary)
            {
                if (args.Count != 2)
                {
                    throw new ParseError("Binary operators accept exactly two parameter");
                }

                _customOperatorsPrecedence[name] = precedence;

                return new BinaryOperatorExpression(name, precedence, args);
            }

            return new PrototypeExpression(name, args);
        }

        private string Identifier()
        {
            var token = Consume(IDENTIFIER, "Expect identifier.");
            return token.Value as string;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();

            throw Error(Peek(), message);
        }

        private Exception Error(Token token, string message)
            => new ParseError(message);

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;

            return Peek().Type == type;
        }

        private bool CheckNext(TokenType tokenType)
        {
            if (IsAtEnd()) return false;
            if (_tokens[_current + 1].Type == EOF) return false;
            return _tokens[_current + 1].Type == tokenType;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;

            return Previous();
        }

        private bool IsAtEnd() => Peek().Type == EOF;
        private Token Peek() => _tokens[_current];
        private Token Previous() => _tokens[_current - 1];
        private double PeekPrecedence()
        {
            var next = Peek();
            var tokenType = next.Type;
            return _binaryOperatorPrecedence.TryGetValue(tokenType, out var value) ? value : -1;
        }

        private void Syncronize()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (Previous().Type == SEMICOLON) return;

                switch (Peek().Type)
                {
                    case FOR:
                    case IF:
                        return;
                }

                Advance();
            }
        }
    }
}