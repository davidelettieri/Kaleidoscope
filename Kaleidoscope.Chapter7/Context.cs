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

        public Context Add(string key,LLVMBuilderRef builder)
        {
            var alloca = builder.BuildAlloca(LLVMTypeRef.Double,key);

            return new Context(_source.SetItem(key, alloca));
        }

        public LLVMValueRef? Get(string key)
        {
            if (_source.TryGetValue(key, out var value))
                return value;

            return null;
        }
    }
}
