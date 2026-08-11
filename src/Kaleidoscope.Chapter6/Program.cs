using Kaleidoscope.Shared;
using System;
using System.IO;

namespace Kaleidoscope;

class Program
{
    private static readonly Parser Parser = new();

    static void Main(string[] args)
    {
        using var jit = new OrcJitEngine();
        using var generator = new IrEmitter(jit.Context);
        if (args.Length == 1)
        {
            RunFile(generator, jit, args[0]);
        }
        else
        {
            RunRepl(generator, jit);
        }
    }

    static void RunFile(IrEmitter generator, OrcJitEngine jit, string path)
    {
        var source = File.ReadAllText(path);
        Run(generator, jit, source);
    }

    static void RunRepl(IrEmitter generator, OrcJitEngine jit)
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

            Run(generator, jit, source);
        }
    }

    static void Run(IrEmitter generator, OrcJitEngine jit, string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();
        var ast = Parser.Parse(tokens);
        var driver = new KaleidoscopeDriver(generator, jit);
        if (ast is not null)
        {
            driver.Run(ast);
        }
    }
}
