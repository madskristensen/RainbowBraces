using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace RainbowBraces
{
    public class XmlTagPairBuilder : PairBuilder
    {
        private readonly bool _allowHtmlVoidTag;
        private bool _inCloseTag;
        private bool _inVoidTag;

        /// <inheritdoc />
        public XmlTagPairBuilder(BracePairBuilderCollection collection, bool allowHtmlVoidTag) : base(collection)
        {
            _allowHtmlVoidTag = allowHtmlVoidTag;
        }

        /// <inheritdoc />
        public override bool TryAdd(string match, Span braceSpan, IReadOnlyList<(Span Span, TagAllowance Allowance)> matchingSpans, (string Line, int Offset) line)
        {
            if (matchingSpans.Count == 0) return false;
            bool isOpenBracket = match is "<" or "</";
            bool isCloseBracket = match is ">" or "/>";
            foreach ((Span Span, TagAllowance Allowance) matchingSpan in matchingSpans)
            {
                // Only XML tags are allowed to intersect
                if (matchingSpan.Allowance != TagAllowance.XmlTag) return false;
                if (matchingSpan.Span.Length <= match.Length) continue;

                // XML tagger is using tags that can span up to 2 brackets (1 close and 1 open) and whitespaces around them.
                int spanStart = matchingSpan.Span.Start;
                int spanEnd = matchingSpan.Span.End;

                // Trim whitespaces from start.
                for (; spanStart < spanEnd; spanStart++)
                {
                    if (!char.IsWhiteSpace(GetChar(line, spanStart))) break;
                }

                // Trim whitespaces from end.
                for (; spanEnd > spanStart; spanEnd--)
                {
                    if (!char.IsWhiteSpace(GetChar(line, spanEnd - 1))) break;
                }

                if (isOpenBracket && spanStart != braceSpan.Start)
                {
                    // If is open bracket then in matching span can be previous close bracket.
                    if (IsLineText(line, spanStart, "/>")) spanStart += 2;
                    else if (IsLineText(line, spanStart, ">")) spanStart++;
                    
                    // If so, again trim whitespaces from start.
                    for (; spanStart < spanEnd; spanStart++)
                    {
                        if (!char.IsWhiteSpace(GetChar(line, spanStart))) break;
                    }
                }

                if (isCloseBracket && spanEnd != braceSpan.End)
                {
                    // If is close bracket in matching span can be next open bracket.
                    if (IsLineText(line, spanEnd - 2, "</")) spanEnd -= 2;
                    else if (IsLineText(line, spanEnd - 1, "<")) spanEnd--;
                    
                    // If so, again trim whitespaces from end.
                    for (; spanEnd > spanStart; spanEnd--)
                    {
                        if (!char.IsWhiteSpace(GetChar(line, spanEnd - 1))) break;
                    }
                }

                // If trimmed matching span still not exactly match bracket span the tag is not allowed (eg. is comment or preprocessor).
                if (spanEnd != braceSpan.End) return false;
                if (spanStart != braceSpan.Start) return false;
            }

            if (isOpenBracket)
            {
                if (match is "</")
                {
                    // If is open bracket of closing tag close the dummy pair for XML element content.
                    if (OpenPairs.Count != 0)
                    {
                        BracePair dummy = OpenPairs.Pop();
                        dummy.Close = new Span(braceSpan.Start, 0);
                    }

                    // Mark the tag as closing.
                    _inCloseTag = true;
                }
                else if (_allowHtmlVoidTag)
                {
                    // If current tag is of HTML void element we'll mark it as it will be self-closing one.
                    _inVoidTag = IsHtmlVoidTag(braceSpan, line);
                }

                // Create new bracket pair for tag.
                BracePair pair = new()
                {
                    Level = NextLevel,
                    Open = braceSpan
                };
                Pairs.Add(pair);
                OpenPairs.Push(pair);
            }
            else if (isCloseBracket)
            {
                // Closing bracket before opening, document is malformed.
                if (OpenPairs.Count == 0)
                {
                    // Set default color, could be some error color specified.
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

                // If the tag is not of self-closing element or HTML void element we'll emit dummy pair for XML element content.
                if (match is ">" && !_inCloseTag && !_inVoidTag)
                {
                    DummyBracePair dummy = new() { Level = NextLevel, Open = new Span(braceSpan.End, 0) };
                    OpenPairs.Push(dummy);
                    Pairs.Add(dummy);
                }

                // Reset intag flags.
                _inCloseTag = false;
                _inVoidTag = false;
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Will return only pairs for tag brackets without dummy.
        /// </remarks>
        public override IEnumerable<BracePair> GetPairs() => Pairs.Where(p => p is not DummyBracePair);

        private static bool IsHtmlVoidTag(Span braceSpan, (string Line, int Offset) line)
        {
            // List of HTML void elements - https://developer.mozilla.org/en-US/docs/Glossary/Void_element
            if (IsTag("area")) return true;
            if (IsTag("base")) return true;
            if (IsTag("br")) return true;
            if (IsTag("col")) return true;
            if (IsTag("embed")) return true;
            if (IsTag("hr")) return true;
            if (IsTag("img")) return true;
            if (IsTag("input")) return true;
            if (IsTag("keygen")) return true;
            if (IsTag("link")) return true;
            if (IsTag("meta")) return true;
            if (IsTag("param")) return true;
            if (IsTag("source")) return true;
            if (IsTag("track")) return true;
            if (IsTag("wbr")) return true;

            return false;

            bool IsTag(string tag)
            {
                return IsLineText(line, braceSpan.End, tag, true);
            }
        }

        private static bool IsLineText((string Line, int Offset) line, int absoluteOffset, string text, bool toLower = false)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = GetChar(line, absoluteOffset + i);
                if (toLower) c = char.ToLower(c);
                if (c != text[i]) return false;
            }

            return true;
        }

        private static char GetChar((string Line, int Offset) line, int absoluteOffset)
        {
            int index = absoluteOffset - line.Offset;
            if (index < 0 || index >= line.Line.Length) return '\0';
            return line.Line[index];
        }
    }

}