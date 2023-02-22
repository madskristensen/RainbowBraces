using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace RainbowBraces
{
    public class BracePairBuilder
    {
        private static readonly Span _empty = new(0, 0);
        private readonly BracePairBuilderCollection _collection;
        private readonly TagAllowance[] _allowedTags;

        public BracePairBuilder(char open, char close, BracePairBuilderCollection collection, TagAllowance[] allowedTags)
        {
            Open = open;
            Close = close;
            _collection = collection;
            _allowedTags = allowedTags;
        }

        public char Open { get; }

        public char Close { get; }

        public List<BracePair> Pairs { get; } = new();

        public Stack<BracePair> OpenPairs { get; } = new();

        // Use global level for all brace types
        private int NextLevel => _collection.Level + 1;

        public void LoadFromCache(IList<BracePair> pairs, int changeStart)
        {
            // Use the cache for all brackets defined above the position of the change
            Pairs.AddRange(pairs.Where(IsAboveChange));

            // Discard all cached closing braces after first change because it could not match anymore
            // We are ordering them by level so we can add them to OpenPairs stack in correct order for future processing
            foreach (BracePair openPair in Pairs
                         .Where(pair => pair.Close.End >= changeStart || pair.Close == _empty)
                         .OrderBy(pair => pair.Level))
            {
                openPair.Close = _empty;
                OpenPairs.Push(openPair);
            }

            bool IsAboveChange(BracePair p)
            {
                // Empty spans can be ignored especially the [0..0) that would be always above change
                if (!p.Open.IsEmpty && p.Open.End <= changeStart) return true;
                if (!p.Close.IsEmpty && p.Close.End <= changeStart) return true;
                return false;
            }
        }

        public bool TryAdd(char match, Span braceSpan, IEnumerable<(Span Span, TagAllowance Allowance)> matchingSpans)
        {
            if (_allowedTags != null)
            {
                // All matching tags must match allowed tags
                if (matchingSpans.Any(t => !_allowedTags.Contains(t.Allowance))) return false;
            }

            if (match == Open)
            {
                // Create new brace pair
                BracePair pair = new()
                {
                    Level = NextLevel,
                    Open = braceSpan
                };
                Pairs.Add(pair);
                OpenPairs.Push(pair);
            }
            else if (match == Close)
            {
                // Closing before opening, document is malformed
                if (OpenPairs.Count == 0)
                {
                    // Set default color, could be some error color specified
                    Pairs.Add(new BracePair()
                    {
                        Level = 1,
                        Close = braceSpan
                    });
                }
                else
                {
                    BracePair pair = OpenPairs.Pop();
                    pair.Close = braceSpan;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}
