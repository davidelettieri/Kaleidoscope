using System.Collections.Generic;
using System.Collections.Immutable;
using LLVMSharp.Interop;

namespace Kaleidoscope
{
    public class Context
    {
        private readonly ImmutableDictionary<string, LLVMValueRef> _source;

        public Context()
        {
            _source = ImmutableDictionary<string, LLVMValueRef>.Empty;
        }

        private Context(ImmutableDictionary<string, LLVMValueRef> source)
        {
            _source = source;
        }

        public Context Add(string key, LLVMValueRef value)
            => new Context(_source.Remove(key).Add(key, value));

        public Context AddArguments(LLVMValueRef function, List<string> arguments)
        {
            var s = _source;

            for (int i = 0; i < arguments.Count; i++)
            {
                var name = arguments[i];
                var param = function.GetParam((uint)i);
                param.Name = name;
                s = s.Add(name, param);
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
}
