using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger
{
    /// <summary>
    /// TODO WIP
    /// </summary>
    public class CPlusPlusAllowanceResolver : DefaultAllowanceResolver
    {
        /// <inheritdoc />
        /// <remarks>The C++ tags are generated in second phase.</remarks>
        public override bool CanChangeTags => true;

        /// <inheritdoc />
        protected override TagAllowance IsAllowed(IClassificationType tagType)
        {
            // TODO is part of "string", but string cannot be easily ignored.
            //if (tagType.IsOfType("cppStringDelimiterCharacter")) return TagAllowance.Operator;
            
            return base.IsAllowed(tagType);
        }

        /// <inheritdoc />
        protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
        {
            string classification = layeredType.Classification;
            
            // TODO is part of "string", but string cannot be easily ignored.
            //if (classification == "cppStringDelimiterCharacter") return TagAllowance.Operator;

            return base.IsAllowed(layeredType);
        }

        /// <inheritdoc />
        protected override TagAllowance GetAllowance(IClassificationType tagType, IMappingSpan span)
        {
            if (tagType.IsOfType(PredefinedClassificationTypeNames.Operator) && CanBeGenericParameterOperator(span))
            {
                // Punctuation is used for generic arguments in C#, we'll use the same here.
                return TagAllowance.Punctuation;
            }

            return base.GetAllowance(tagType, span);
        }

        private static bool CanBeGenericParameterOperator(IMappingSpan span)
        {
            // Check id experimental flag is set.
            if (!General.Instance.ExperimentalCPlusPlusGenerics) return false;

            ITextBuffer buffer = span.AnchorBuffer;
            NormalizedSnapshotSpanCollection textSpans = span.GetSpans(buffer);
            if (textSpans.Count != 1) return false;

            SnapshotSpan textSpan = textSpans[0];
            if (textSpan.Length != 1) return false;

            string text = textSpan.GetText();
            return text switch
            {
                "<" => true,
                ">" => true,
                _ => false
            };
        }
    }
}