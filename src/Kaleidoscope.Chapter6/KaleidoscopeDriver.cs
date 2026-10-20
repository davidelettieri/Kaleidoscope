using Kaleidoscope.Shared.AST;
using System;
using System.Collections.Generic;

namespace Kaleidoscope;

public class KaleidoscopeDriver(IrEmitter generator, OrcJitEngine jit)
{
    private readonly IrEmitter _generator = generator;
    private readonly OrcJitEngine _jit = jit;
    private int _anonCounter = 0;

    public void Run(List<Expression> exprs)
    {
        var anonSymbolsToExecute = new List<string>();

        // 1. EMISSION PHASE: Emit AST nodes into the generator's active module
        foreach (var expr in exprs)
        {
            if (expr is FunctionExpression { Proto.Name: "anon_expr" } func)
            {
                // Assign a unique symbol name so multiple top-level expressions in the same batch don't collide
                string uniqueAnonName = $"__anon_expr_{_anonCounter++}";
                var functionWithCustomName = func with { Proto = func.Proto with { Name = uniqueAnonName } };
                _generator.Emit(functionWithCustomName);
                // Emit with the new unique name
                anonSymbolsToExecute.Add(uniqueAnonName);
            }
            else
            {
                // Emit standard function definition or prototype
                _generator.Emit(expr);
            }
        }

        // Extract the compiled module and reset the generator for the next REPL turn
        LLVMModuleRef module = _generator.ExtractModuleAndReset($"batch_module_{Guid.NewGuid():N}");

        // 2. OPTIMIZATION PHASE: Run IR transformations on the module before compilation
        // _optimizer.Optimize(module);

        // 3. JIT INGESTION PHASE: Hand off module ownership to the JIT engine
        _jit.AddModule(module);

        // 4. EXECUTION PHASE: Resolve and invoke top-level anonymous expressions
        foreach (var symbolName in anonSymbolsToExecute)
        {
            var funcDelegate = _jit.GetFunctionDelegate(symbolName);
            double result = funcDelegate();
            Console.WriteLine("> {0}", result);
        }
    }
}
