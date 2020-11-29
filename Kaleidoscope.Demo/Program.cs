using System;
using System.Collections.Generic;

namespace Kaleidoscope.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("> ");
                var source = Console.ReadLine();
                var scanner = new Scanner(source);
                var tokens = scanner.ScanTokens();
            }
        }
    }
}
