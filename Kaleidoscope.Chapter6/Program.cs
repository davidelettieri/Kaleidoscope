using LLVMSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Kaleidoscope
{
    class Program
    {
        private static readonly Interpreter _interpreter = new Interpreter();
        private static readonly Parser _parser = new Parser();
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunREPL();
            }

        }

        static void RunFile(string path)
        {
            var source = File.ReadAllText(path);
            Run(source);
        }

        static void RunREPL()
        {
            while (true)
            {
                Console.Write("> ");
                var source = Console.ReadLine();
                if (source is null)
                {
                    Console.WriteLine("See you soon!");
                    return;
                }
                Run(source);
            }
        }


        static void Run(string source)
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var ast = _parser.Parse(tokens);

            if (ast is not null)
            {
                _interpreter.Run(ast);
            }
        }
    }
}
