using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace RainbowBraces.Tagger
{
    public class MatchingContext
    {
        public MatchingContext(IEnumerable<(SnapshotSpan Span, TagAllowance Allowance)> all)
        {
            All = all
                .Select((tuple, i) => new AllowanceSpan(tuple.Span.Span, tuple.Allowance, i))
                .ToArray();
            
            OrderedAllowanceSpan[] allOrdered = All
                .Select(s => new OrderedAllowanceSpan(s))
                .ToArray();

            FromStart = allOrdered
                .OrderBy(s => s.Span.Start)
                .Select((s, i) =>
                {
                    s.FromStartOrderIndex = i;
                    return s;
                })
                .ToArray();

            FromEnd = allOrdered
                .OrderBy(d => d.Span.End)
                .Select((s, i) =>
                {
                    s.FromEndOrderIndex = i;
                    return s;
                })
                .ToArray();
        }

        private IReadOnlyList<AllowanceSpan> All { get; }

        public IReadOnlyList<OrderedAllowanceSpan> FromStart { get; }

        public IReadOnlyList<OrderedAllowanceSpan> FromEnd { get; }

        private int FromStartAdd { get; set; }

        private int FromEndRemove { get; set; }

        private Dictionary<int, OrderedAllowanceSpan> PossibleMatchingSpans { get; } = new();

        public List<OrderedAllowanceSpan> MatchingSpans { get; } = new();

        public void ProceedTo(int lineStart, int lineEnd)
        {
            // Remove all disallowed tags with end before this line
            while (true)
            {
                if (FromEndRemove >= FromEnd.Count) break;
                OrderedAllowanceSpan indexedSpan = FromEnd[FromEndRemove];
                if (indexedSpan.Span.End >= lineStart) break;
                PossibleMatchingSpans.Remove(indexedSpan.Index);
                FromEndRemove++;
            }

            // Add all disallowed tags with start on this line
            while (true)
            {
                if (FromStartAdd >= FromStart.Count) break;
                OrderedAllowanceSpan indexedSpan = FromStart[FromStartAdd];
                if (indexedSpan.Span.Start > lineEnd) break;
                PossibleMatchingSpans.Add(indexedSpan.Index, indexedSpan);
                FromStartAdd++;
            }
        }

        public IReadOnlyList<OrderedAllowanceSpan> GetMatch(Match match, int position, int positionEnd)
        {
            // Enumeration of spans matching tags.
            MatchingSpans.Clear();
            MatchingSpans.AddRange(PossibleMatchingSpans.Values.Where(s => s.Span.Start <= position && s.Span.End > positionEnd));

            // If match is more than 1 character, include possible smaller spans inside.
            if (match.Length > 1)
            {
                MatchingSpans.AddRange(PossibleMatchingSpans.Values.Where(s => s.Span.Start >= position && s.Span.End <= positionEnd));
            }

            return MatchingSpans;
        }

        public record AllowanceSpan(Span Span, TagAllowance Allowance, int Index);

        public class OrderedAllowanceSpan
        {
            public OrderedAllowanceSpan(AllowanceSpan allowanceSpan)
            {
                AllowanceSpan = allowanceSpan;
            }

            public AllowanceSpan AllowanceSpan { get; }

            public int FromStartOrderIndex { get; set; }

            public int FromEndOrderIndex { get; set; }

            public Span Span => AllowanceSpan.Span;

            public TagAllowance Allowance => AllowanceSpan.Allowance;

            public int Index => AllowanceSpan.Index;
        }
    }
}
