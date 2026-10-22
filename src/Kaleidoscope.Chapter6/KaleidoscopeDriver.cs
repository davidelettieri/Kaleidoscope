using Kaleidoscope.Shared.AST;
using System;
using System.Collections.Generic;

namespace Kaleidoscope;

public class KaleidoscopeDriver(IrEmitter generator, OrcJitEngine jit)
{
    private readonly IrEmitter _generator = generator;
    private readonly OrcJitEngine _jit = jit;
    private int _anonCounter;

    public void Run(List<Expression> exprs)
    {
        var anonSymbols = new List<string>();
        var committed = false;
        _generator.BeginBatch();
        try
        {
            foreach (var expr in exprs)
            {
                if (expr is FunctionExpression { Proto.Name: "anon_expr" } function)
                {
                    var uniqueName = $"__anon_expr_{_anonCounter++}";
                    _generator.Emit(function with { Proto = function.Proto with { Name = uniqueName } });
                    anonSymbols.Add(uniqueName);
                }
                else
                {
                    _generator.Emit(expr);
                }
            }

            var module = _generator.ExtractModuleAndReset($"batch_module_{Guid.NewGuid():N}");
            _jit.AddModule(module);
            committed = true;
            _generator.CommitBatch();

            foreach (var symbolName in anonSymbols)
            {
                var function = _jit.GetFunctionDelegate(symbolName);
                Console.WriteLine("> {0}", function());
            }
        }
        catch
        {
            if (!committed)
                _generator.AbortBatch();

            throw;
        }
    }
}
