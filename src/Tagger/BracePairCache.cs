using System.Collections.Generic;

namespace RainbowBraces
{
    public class BracePairCache
    {
        private readonly Dictionary<object, List<BracePair>> _cachedPairs = new();
        
        public bool TryGet(PairBuilder builder, out List<BracePair> cache) => _cachedPairs.TryGetValue(builder.GetCacheKey(), out cache);

        public void Set(PairBuilder builder, List<BracePair> cache) => _cachedPairs[builder.GetCacheKey()] = cache;

        public void Clear() => _cachedPairs.Clear();
    }
}