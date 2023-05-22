using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace RainbowBraces
{
    public abstract class PairBuilder
    {
        private readonly BracePairBuilderCollection _collection;

        protected PairBuilder(BracePairBuilderCollection collection)
        {
            _collection = collection;
        }

        public List<BracePair> Pairs { get; } = new();

        public Stack<BracePair> OpenPairs { get; } = new();

        // Use global level for all brace types
        protected int NextLevel => _collection.Level + 1;

        protected static Span Empty { get; } = new(0, 0);

        public void LoadFromCache(IList<BracePair> pairs, int changeStart)
        {
            // Use the cache for all brackets defined above the position of the change
            Pairs.AddRange(pairs.Where(IsAboveChange));

            // Discard all cached closing braces after first change because it could not match anymore
            // We are ordering them by level so we can add them to OpenPairs stack in correct order for future processing
            foreach (BracePair openPair in Pairs
                         .Where(pair => pair.Close.End >= changeStart || pair.Close == Empty)
                         .OrderBy(pair => pair.Level))
            {
                openPair.Close = Empty;
                OpenPairs.Push(openPair);
            }

            bool IsAboveChange(BracePair p)
            {
                // Dummy span is expected to be empty
                if (p is DummyBracePair)
                {
                    if (p.Open.End <= changeStart) return true;
                    if (p.Close.End <= changeStart) return true;
                    return false;
                }

                // Empty spans can be ignored especially the [0..0) that would be always above change
                if (!p.Open.IsEmpty && p.Open.End <= changeStart) return true;
                if (!p.Close.IsEmpty && p.Close.End <= changeStart) return true;
                return false;
            }
        }

        public abstract bool TryAdd(string match, Span braceSpan, IReadOnlyList<(Span Span, TagAllowance Allowance)> matchingSpans, (string Line, int Offset) line);

        public virtual object GetCacheKey() => GetType();

        public virtual IEnumerable<BracePair> GetPairs() => Pairs;
    }
}