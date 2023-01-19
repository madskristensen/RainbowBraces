using System.Collections.Generic;

namespace RainbowBraces
{
    public class BracePairCache
    {
        private readonly Dictionary<(char Open, char Close), List<BracePair>> _cachedPairs = new();
        
        public bool TryGet(char open, char close, out List<BracePair> cache) => _cachedPairs.TryGetValue((open, close), out cache);

        public void Set(char open, char close, List<BracePair> cache) => _cachedPairs[(open, close)] = cache;

        public void Clear() => _cachedPairs.Clear();
    }
}