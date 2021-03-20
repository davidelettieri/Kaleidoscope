using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaleidoscope
{
    class Program
    {
        static void Main(string[] args)
        {
            var interpreter = new Interpreter();
            var parser = new Parser();

            while (true)
            {
                Console.Write("> ");
                var source = Console.ReadLine();
                var scanner = new Scanner(source);
                var tokens = scanner.ScanTokens();
                var ast = parser.Parse(tokens);

                if (ast is not null)
                {
                    interpreter.Run(ast);
                }
            }
        }
    }
}
