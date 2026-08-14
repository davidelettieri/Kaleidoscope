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
        var driver = new KaleidoscopeDriver(generator, jit);
        if (args.Length == 1)
        {
            RunFile(driver, args[0]);
        }
        else
        {
            RunRepl(driver);
        }
    }

    static void RunFile(KaleidoscopeDriver driver, string path)
    {
        var source = File.ReadAllText(path);
        Run(driver, source);
    }

    static void RunRepl(KaleidoscopeDriver driver)
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

            Run(driver, source);
        }
    }

    static void Run(KaleidoscopeDriver driver, string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();
        var ast = Parser.Parse(tokens);
        if (ast is not null)
        {
            driver.Run(ast);
        }
    }
}
