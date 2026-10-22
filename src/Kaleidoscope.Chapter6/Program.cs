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
        try
        {
            var source = File.ReadAllText(path);
            Run(driver, source, interactive: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
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

            Run(driver, source, interactive: true);
        }
    }

    static void Run(KaleidoscopeDriver driver, string source, bool interactive)
    {
        try
        {
            var scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();
            var ast = Parser.Parse(tokens);
            if (ast is null)
            {
                if (!interactive)
                    Environment.ExitCode = 1;
                return;
            }

            driver.Run(ast);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            if (!interactive)
                Environment.ExitCode = 1;
        }
    }
}
