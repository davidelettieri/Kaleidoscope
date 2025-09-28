using LLVMSharp.Interop;
using System.Collections.Immutable;

namespace Kaleidoscope.Shared;

public sealed class Context
{
    private readonly ImmutableDictionary<string, LLVMValueRef> _source;

    public Context() => _source = ImmutableDictionary<string, LLVMValueRef>.Empty;

    private Context(ImmutableDictionary<string, LLVMValueRef> source) => _source = source;

    public Context Add(string key, LLVMValueRef value)
        => new Context(_source.SetItem(key, value));

    /// <summary>
    /// Used only in Chapter 6
    /// </summary>
    public Context AddArguments(LLVMValueRef function, List<string> arguments)
    {
        if (arguments.Count == 0)
            return this;

        var s = _source;

        for (int i = 0; i < arguments.Count; i++)
        {
            var name = arguments[i];
            var param = function.GetParam((uint)i);
            param.Name = name;
            s = s.SetItem(name, param);
        }

        return new Context(s);
    }

    public LLVMValueRef? Get(string key)
    {
        if (_source.TryGetValue(key, out var value))
            return value;

        return null;
    }
}
