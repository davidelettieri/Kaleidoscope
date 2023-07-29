using System;
using System.IO;

namespace Kaleidoscope
{
    class Program
    {
        private static readonly Interpreter Interpreter = new();
        private static readonly Parser Parser = new();
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunRepl();
            }

        }

        static void RunFile(string path)
        {
            var source = File.ReadAllText(path);
            Run(source);
        }

        static void RunRepl()
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
            var ast = Parser.Parse(tokens);

            if (ast is not null)
            {
                Interpreter.Run(ast);
            }
        }
    }
}
