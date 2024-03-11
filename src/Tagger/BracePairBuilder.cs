using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using RainbowBraces.Tagger;

namespace RainbowBraces
{
    public class BracePairBuilder : PairBuilder
    {
        private readonly TagAllowance[] _allowedTags;
        private readonly TagAllowance[] _ignoredTags;

        public BracePairBuilder(char open, char close, bool useGlobalStack, BracePairBuilderCollection collection, TagAllowance[] allowedTags, TagAllowance[] ignoredTags) : base(collection, useGlobalStack)
        {
            Open = open;
            Close = close;
            _allowedTags = allowedTags;
            _ignoredTags = ignoredTags;
        }

        public char Open { get; }

        public char Close { get; }

        public override bool TryAdd(string match, Span braceSpan, MatchingContext context, (string Line, int Offset) line)
        {
            if (match.Length != 1) return false;
            char c = match[0];

            if (_allowedTags != null)
            {
                IEnumerable<MatchingContext.OrderedAllowanceSpan> possibleSpans = context.MatchingSpans;

                // Remove ignored tags
                if (_ignoredTags is { Length: > 0 })
                {
                    possibleSpans = possibleSpans.Where(t => !_ignoredTags.Contains(t.Allowance));
                }

                bool anyAllowed = false;

                // All matching tags must match allowed tags
                foreach (MatchingContext.OrderedAllowanceSpan t in possibleSpans)
                {
                    if (!_allowedTags.Contains(t.Allowance)) return false;
                    anyAllowed = true;
                }

                // All matched tags are ignored
                if (!anyAllowed) return false;
            }

            if (c == Open)
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
            else if (c == Close)
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

        /// <inheritdoc />
        public override object GetCacheKey() => (Open, Close);
    }
}
