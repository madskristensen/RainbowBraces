using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger
{
    public class CssAllowanceResolver : AllowanceResolver
    {
        protected override TagAllowance IsAllowed(IClassificationType tagType)
        {
            // Allow for CSS Property Value tag
            if (tagType.IsOfType("CSS Property Value")) return TagAllowance.Punctuation;

            // Allow for CSS Selector
            if (tagType.IsOfType("CSS Selector")) return TagAllowance.Operator;
            return TagAllowance.Disallowed;
        }

        protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
        {
            string classification = layeredType.Classification;

            // Allow for CSS Property Value tag
            if (classification == "CSS Property Value") return TagAllowance.Punctuation;

            // Allow for CSS Selector
            if (classification == "CSS Selector") return TagAllowance.Operator;
            return TagAllowance.Disallowed;
        }
    }
}
