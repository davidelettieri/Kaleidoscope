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
            var emitter = new IREmitter();

            while (true)
            {
                Console.Write("> ");
                var source = Console.ReadLine();
                var scanner = new Scanner(source);
                var tokens = scanner.ScanTokens();
                var parser = new Parser(tokens);
                var ast = parser.Parse();
                emitter.Intepret(ast);
            }
        }
    }
}
