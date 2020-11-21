namespace Kaleidoscope
{
    using System;
    using System.Collections.Generic;
    using AST;

    public sealed class Parser
    {
        private readonly Lexer scanner;

        private readonly BaseParserListener baseListener;

        public Parser(Lexer scanner, IParserListener listener)
        {
            this.scanner = scanner;
            baseListener = new BaseParserListener(listener);
        }

        public void HandleDefinition()
        {
            baseListener.EnterRule("HandleDefinition");

            var functionAST = ParseDefinition();

            baseListener.ExitRule(functionAST);

            if (functionAST != null)
            {
                baseListener.Listen();
            }
            else
            {
                // Skip token for error recovery.
                scanner.GetNextToken();
            }
        }

        public void HandleExtern()
        {
            baseListener.EnterRule("HandleExtern");

            var prototypeAST = ParseExtern();

            baseListener.ExitRule(prototypeAST);

            if (prototypeAST != null)
            {
                baseListener.Listen();
            }
            else
            {
                // Skip token for error recovery.
                scanner.GetNextToken();
            }
        }

        public void HandleTopLevelExpression()
        {
            // Evaluate a top-level expression into an anonymous function.
            baseListener.EnterRule("HandleTopLevelExpression");

            var functionAST = ParseTopLevelExpr();

            baseListener.ExitRule(functionAST);

            if (functionAST != null)
            {
                baseListener.Listen();
            }
            else
            {
                // Skip token for error recovery.
                scanner.GetNextToken();
            }
        }

        // identifierexpr
        //   ::= identifier
        //   ::= identifier '(' expression* ')'
        private ExprAST ParseIdentifierExpr()
        {
            string idName = scanner.GetLastIdentifier();
            
            scanner.GetNextToken();  // eat identifier.

            if (scanner.CurrentToken != '(') // Simple variable ref.
            {
                return new VariableExprAST(idName);
            }

            // Call.
            scanner.GetNextToken();  // eat (
            List<ExprAST> args = new List<ExprAST>();

            if (scanner.CurrentToken != ')')
            {
                while (true)
                {
                    ExprAST arg = ParseExpression();
                    if (arg == null)
                    {
                        return null;
                    }

                    args.Add(arg);

                    if (scanner.CurrentToken == ')')
                    {
                        break;
                    }

                    if (scanner.CurrentToken != ',')
                    {
                        Console.WriteLine("Expected ')' or ',' in argument list");
                        return null;
                    }
                    
                    scanner.GetNextToken();
                }
            }
            
            // Eat the ')'.
            scanner.GetNextToken();

            return new CallExprAST(idName, args);
        }

        // numberexpr ::= number
        private ExprAST ParseNumberExpr()
        {
            ExprAST result = new NumberExprAST(scanner.GetLastNumber());
            scanner.GetNextToken();
            return result;
        }

        // parenexpr ::= '(' expression ')'
        private ExprAST ParseParenExpr()
        {
            scanner.GetNextToken();  // eat (.
            ExprAST v = ParseExpression();
            if (v == null)
            {
                return null;
            }

            if (scanner.CurrentToken != ')')
            {
                Console.WriteLine("expected ')'");
                return null;
            }

            scanner.GetNextToken(); // eat ).

            return v;
        }

        // ifexpr ::= 'if' expression 'then' expression 'else' expression
        public ExprAST ParseIfExpr()
        {
            scanner.GetNextToken(); // eat the if.

            // condition
            ExprAST cond = ParseExpression();
            if (cond == null)
            {
                return null;
            }

            if (scanner.CurrentToken != Token.THEN)
            {
                Console.WriteLine("expected then");
            }

            scanner.GetNextToken(); // eat the then

            ExprAST then = ParseExpression();
            if (then == null)
            {
                return null;
            }

            if (scanner.CurrentToken != Token.ELSE)
            {
                Console.WriteLine("expected else");
                return null;
            }

            scanner.GetNextToken();

            ExprAST @else = ParseExpression();
            if (@else == null)
            {
                return null;
            }

            return new IfExpAST(cond, then, @else);
        }

        // forexpr ::= 'for' identifier '=' expr ',' expr (',' expr)? 'in' expression
        public ExprAST ParseForExpr()
        {
            scanner.GetNextToken(); // eat the for.

            if (scanner.CurrentToken != Token.IDENTIFIER)
            {
                Console.WriteLine("expected identifier after for");
                return null;
            }

            string idName = scanner.GetLastIdentifier();
            scanner.GetNextToken(); // eat identifier.

            if (scanner.CurrentToken != '=')
            {
                Console.WriteLine("expected '=' after for");
                return null;
            }

            scanner.GetNextToken(); // eat '='.

            ExprAST start = ParseExpression();
            if (start == null)
            {
                return null;
            }

            if (scanner.CurrentToken != ',')
            {
                Console.WriteLine("expected ',' after for start value");
                return null;
            }

            scanner.GetNextToken();

            ExprAST end = ParseExpression();
            if (end == null)
            {
                return null;
            }

            // The step value is optional;
            ExprAST step = null;
            if (scanner.CurrentToken == ',')
            {
                scanner.GetNextToken();
                step = ParseExpression();
                if (step == null)
                {
                    return null;
                }
            }

            if (scanner.CurrentToken != Token.IN)
            {
                Console.WriteLine("expected 'in' after for");
                return null;
            }

            scanner.GetNextToken();
            ExprAST body = ParseExpression();
            if (body == null)
            {
                return null;
            }

            return new ForExprAST(idName, start, end, step, body);
        }

        // primary
        //   ::= identifierexpr
        //   ::= numberexpr
        //   ::= parenexpr
        private ExprAST ParsePrimary()
        {
            switch (scanner.CurrentToken)
            {
                case Token.IDENTIFIER:
                    return ParseIdentifierExpr();
                case Token.NUMBER:
                    return ParseNumberExpr();
                case '(':
                    return ParseParenExpr();
                case Token.IF:
                    return ParseIfExpr();
                case Token.FOR:
                    return ParseForExpr();
                default:
                    Console.WriteLine("unknown token when expecting an expression");
                    return null;
            }
        }

        // binoprhs
        //   ::= ('+' primary)*
        private ExprAST ParseBinOpRHS(int exprPrec, ExprAST lhs)
        {
            // If this is a binop, find its precedence.
            while (true)
            {
                int tokPrec = scanner.GetTokPrecedence();

                // If this is a binop that binds at least as tightly as the current binop,
                // consume it, otherwise we are done.
                if (tokPrec < exprPrec)
                {
                    return lhs;
                }

                // Okay, we know this is a binop.
                int binOp = scanner.CurrentToken;
                scanner.GetNextToken();  // eat binop

                // Parse the primary expression after the binary operator.
                ExprAST rhs = ParsePrimary();
                if (rhs == null)
                {
                    return null;
                }

                // If BinOp binds less tightly with RHS than the operator after RHS, let
                // the pending operator take RHS as its LHS.
                int nextPrec = scanner.GetTokPrecedence();
                if (tokPrec < nextPrec)
                {
                    rhs = ParseBinOpRHS(tokPrec + 1, rhs);
                    if (rhs == null)
                    {
                        return null;
                    }
                }

                // Merge LHS/RHS.
                lhs = new BinaryExprAST((char)binOp, lhs, rhs);
            }
        }

        // expression
        //   ::= primary binoprhs
        //
        private ExprAST ParseExpression()
        {
            ExprAST lhs = ParsePrimary();
            if (lhs == null)
            {
                return null;
            }

            return ParseBinOpRHS(0, lhs);
        }

        // prototype
        //   ::= id '(' id* ')'
        private PrototypeAST ParsePrototype()
        {
            if (scanner.CurrentToken != Token.IDENTIFIER)
            {
                Console.WriteLine("Expected function name in prototype");
                return null;
            }

            string fnName = scanner.GetLastIdentifier();

            scanner.GetNextToken();

            if (scanner.CurrentToken != '(')
            {
                Console.WriteLine("Expected '(' in prototype");
                return null;
            }

            List<string> argNames = new List<string>();
            while (scanner.GetNextToken() == Token.IDENTIFIER)
            {
                argNames.Add(scanner.GetLastIdentifier());
            }

            if (scanner.CurrentToken != ')')
            {
                Console.WriteLine("Expected ')' in prototype");
                return null;
            }

            scanner.GetNextToken(); // eat ')'.

            return new PrototypeAST(fnName, argNames);
        }

        // definition ::= 'def' prototype expression
        private FunctionAST ParseDefinition()
        {
            scanner.GetNextToken(); // eat def.
            PrototypeAST proto = ParsePrototype();

            if (proto == null)
            {
                return null;
            }

            ExprAST body = ParseExpression();
            if (body == null)
            {
                return null;
            }

            return new FunctionAST(proto, body);
        }

        /// toplevelexpr ::= expression
        private FunctionAST ParseTopLevelExpr()
        {
            ExprAST e = ParseExpression();
            if (e == null)
            {
                return null;
            }

            // Make an anonymous proto.
            PrototypeAST proto = new PrototypeAST(string.Empty, new List<string>());
            return new FunctionAST(proto, e);
        }

        /// external ::= 'extern' prototype
        private PrototypeAST ParseExtern()
        {
            scanner.GetNextToken();  // eat extern.
            return ParsePrototype();
        }
    }
}