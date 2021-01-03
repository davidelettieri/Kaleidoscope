using System;
using System.Collections.Generic;
using Kaleidoscope.AST;
using static Kaleidoscope.TokenType;

namespace Kaleidoscope
{
    class ParseError : Exception
    {

    }


    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private readonly Dictionary<TokenType, int> _binaryOperatorPrecedence =
            new Dictionary<TokenType, int>()
            {
                { LESS_THAN, 10 },
                { PLUS, 20 },
                { MINUS, 20 },
                { STAR, 40 },
            };

        private int _current = 0;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public List<ExprAST> Parse()
        {
            var result = new List<ExprAST>();

            while (!IsAtEnd())
            {
                var e = TopLevel();
                result.Add(e);
                Consume(SEMICOLON, "Expected semi-colon.");
            }

            return result;
        }

        private ExprAST TopLevel()
        {
            try
            {
                if (Match(DEF)) return Definition();
                if (Match(EXTERN)) return Extern();

                var expr = Expression();

                return new FunctionAST(new PrototypeAST("", new List<string>()), expr);
            }
            catch (ParseError error)
            {
                Syncronize();
                return null;
            }
        }

        private ExprAST Expression(int precedence = 0)
        {
            var lhs = Primary();

            while (precedence < GetPrecedence())
            {
                lhs = ParseBinary(lhs);
            }

            return lhs;
        }

        private ExprAST ParseBinary(ExprAST lhs)
        {
            var token = Advance();
            var precedence = _binaryOperatorPrecedence[token.Type];
            var rhs = Expression(precedence);
            return new BinaryExprAST(token.Type, lhs, rhs);
        }

        private int GetPrecedence()
        {
            var next = Peek();

            return _binaryOperatorPrecedence.TryGetValue(next.Type, out var value) ? value : 0;
        }

        private ExprAST Primary()
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

                return new VariableExprAST(name);
            }

            if (Match(NUMBER))
            {
                return new NumberExprAST((double)Previous().Value);
            }

            throw Error(Peek(), "Expect expression.");
        }

        private ExprAST Call(string name)
        {
            var args = new List<ExprAST>();

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

            return new CallExprAST(name, args);
        }

        private ExprAST Extern() => Prototype();

        private ExprAST Definition()
        {
            var prototype = Prototype();
            var body = Expression();

            return new FunctionAST(prototype, body);
        }

        private PrototypeAST Prototype()
        {
            var name = Identifier();
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

            Consume(RIGHT_PAREN, "Expected ')'");

            return new PrototypeAST(name, args);
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
        {
            throw new NotImplementedException();
        }

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
        private int PeekPrecedence()
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