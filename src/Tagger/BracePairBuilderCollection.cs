using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RainbowBraces
{
    public class BracePairBuilderCollection : IEnumerable<PairBuilder>
    {
        private readonly List<PairBuilder> _builders = new();
        
        public int Level => _builders
            .Where(builder => builder.UseGlobalStack)
            .Sum(builder => builder.OpenPairs.Count);

        public void AddBuilder(char open, char close, bool useGlobalStack, TagAllowance[] allowedTags = null, TagAllowance[] ignoredTags = null)
        {
            BracePairBuilder builder = new(open, close, useGlobalStack, this, allowedTags, ignoredTags);
            _builders.Add(builder);
        }

        public void AddXmlTagBuilder(bool allowHtmlVoidTag, bool useGlobalStack)
        {
            XmlTagPairBuilder builder = new(useGlobalStack, this, allowHtmlVoidTag);
            _builders.Add(builder);
        }

        public void AddBuilder<TBuilder>(Func<BracePairBuilderCollection, TBuilder> ctor)
            where TBuilder : PairBuilder
        {
            TBuilder builder = ctor(this);
            _builders.Add(builder);
        }

        public void LoadFromCache(BracePairCache cache, int changeStart)
        {
            foreach (PairBuilder builder in _builders)
            {
                if (cache.TryGet(builder, out List<BracePair> pairs))
                {
                    builder.LoadFromCache(pairs, changeStart);
                }
            }
        }

        public void SaveToCache(BracePairCache cache)
        {
            foreach (PairBuilder builder in _builders)
            {
                cache.Set(builder, builder.Pairs);
            }
        }

        /// <inheritdoc />
        public IEnumerator<PairBuilder> GetEnumerator() => _builders.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
