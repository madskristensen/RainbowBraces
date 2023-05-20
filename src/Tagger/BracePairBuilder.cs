using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace RainbowBraces
{
    public class BracePairBuilder : PairBuilder
    {
        private readonly TagAllowance[] _allowedTags;

        public BracePairBuilder(char open, char close, BracePairBuilderCollection collection, TagAllowance[] allowedTags) : base(collection)
        {
            Open = open;
            Close = close;
            _allowedTags = allowedTags;
        }

        public char Open { get; }

        public char Close { get; }

        public override bool TryAdd(string match, Span braceSpan, IReadOnlyList<(Span Span, TagAllowance Allowance)> matchingSpans, (string Line, int Offset) line)
        {
            if (match.Length != 1) return false;
            char c = match[0];

            if (_allowedTags != null)
            {
                // All matching tags must match allowed tags
                if (matchingSpans.Any(t => !_allowedTags.Contains(t.Allowance))) return false;
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
