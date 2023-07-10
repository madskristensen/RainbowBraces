using System;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using RainbowBraces.Tagger;

namespace RainbowBraces;

public class CPlusPlusTemplateTagPairBuilder : PairBuilder
{
    private readonly CPlusPlusAllowanceResolver _allowanceResolver;

    public CPlusPlusTemplateTagPairBuilder(BracePairBuilderCollection collection, CPlusPlusAllowanceResolver allowanceResolver) : base(collection)
    {
        _allowanceResolver = allowanceResolver;
    }

    public override bool TryAdd(string match, Span braceSpan, MatchingContext context, (string Line, int Offset) line)
    {
        if (match is "<")
        {
            if (!BaseCheck(out MatchingContext.OrderedAllowanceSpan span)) return false;

            // There cannot be 2 opening braces next to another
            if (context.MatchingSpans[0].Span.Length != 1) return false;

            if (!TryGetPrevious(span, out string previous)) return false;
            if (!CPlusPlusAllowanceResolver.IsValidPreviousOpen(previous)) return false;

            if (!TryGetNext(span, out string next)) return false;
            if (!CPlusPlusAllowanceResolver.IsValidNextOpen(next)) return false;
            
            BracePair pair = new()
            {
                Level = NextLevel,
                Open = braceSpan
            };
            Pairs.Add(pair);
            OpenPairs.Push(pair);

            return true;
        }

        if (match is ">")
        {
            if (!BaseCheck(out MatchingContext.OrderedAllowanceSpan span)) return false;

            if (!TryGetPrevious(span, out string previous)) return false;
            if (!CPlusPlusAllowanceResolver.IsValidPreviousClose(previous)) return false;

            // We don't want to color errors in this case. (can be usual operator)
            if (OpenPairs.Count == 0)
            {
                return false;
            }

            BracePair pair = OpenPairs.Pop();
            pair.Close = braceSpan;
            return true;
        }
        
        return false;

        bool BaseCheck(out MatchingContext.OrderedAllowanceSpan span)
        {
            span = null;
            if (context.MatchingSpans.Count != 1) return false;

            span = context.MatchingSpans[0];
            return true;
        }

        bool TryGetPrevious(MatchingContext.OrderedAllowanceSpan span, out string previousClassification)
        {
            int previousIndex = span.FromStartOrderIndex - 1;
            if (previousIndex < 0 || previousIndex >= context.FromStart.Count)
            {
                previousClassification = null;
                return false;
            }

            MatchingContext.OrderedAllowanceSpan previous = context.FromStart[previousIndex];
            previousClassification = _allowanceResolver.GetClassification(previous.Span.Start, previous.Span.End);
            return true;
        }

        bool TryGetNext(MatchingContext.OrderedAllowanceSpan span, out string nextClassification)
        {
            int nextIndex = span.FromEndOrderIndex + 1;
            if (nextIndex < 0 || nextIndex >= context.FromEnd.Count)
            {
                nextClassification = null;
                return false;
            }

            MatchingContext.OrderedAllowanceSpan next = context.FromEnd[nextIndex];
            nextClassification = _allowanceResolver.GetClassification(next.Span.Start, next.Span.End);
            return true;
        }
    }
}