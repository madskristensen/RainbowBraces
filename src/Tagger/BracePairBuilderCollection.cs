using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RainbowBraces
{
    public class BracePairBuilderCollection : IEnumerable<BracePairBuilder>
    {
        private readonly List<BracePairBuilder> _builders = new();
        
        public int Level => _builders.Sum(builder => builder.OpenPairs.Count);

        public void AddBuilder(char open, char close)
        {
            BracePairBuilder builder = new(open, close, this);
            _builders.Add(builder);
        }

        public void LoadFromCache(BracePairCache cache, int changeStart)
        {
            foreach (BracePairBuilder builder in _builders)
            {
                if (cache.TryGet(builder.Open, builder.Close, out List<BracePair> pairs))
                {
                    builder.LoadFromCache(pairs, changeStart);
                }
            }
        }

        public void SaveToCache(BracePairCache cache)
        {
            foreach (BracePairBuilder builder in _builders)
            {
                cache.Set(builder.Open, builder.Close, builder.Pairs);
            }
        }

        /// <inheritdoc />
        public IEnumerator<BracePairBuilder> GetEnumerator() => _builders.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
