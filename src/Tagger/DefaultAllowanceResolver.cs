using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger
{
    public class DefaultAllowanceResolver : AllowanceResolver
    {
        /// <inheritdoc />
        protected override TagAllowance IsAllowed(IClassificationType tagType)
        {
            // Allow tags for braces
            if (tagType.IsOfType(PredefinedClassificationTypeNames.Punctuation)) return TagAllowance.Punctuation;
            if (tagType.IsOfType(PredefinedClassificationTypeNames.Operator)) return TagAllowance.Operator;
            if (tagType.IsOfType("XAML Delimiter")) return TagAllowance.Delimiter;
            if (tagType.IsOfType("SQL Operator")) return TagAllowance.Operator;
            if (tagType.IsOfType("unnecessary code")) return TagAllowance.Ignore;
            if (tagType.IsOfType("ReSharper Dead Code")) return TagAllowance.Ignore;
            return TagAllowance.Disallowed;
        }

        /// <inheritdoc />
        protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
        {
            string classification = layeredType.Classification;

            // Allow tags for braces
            TagAllowance allowance = classification switch
            {
                PredefinedClassificationTypeNames.Punctuation => TagAllowance.Punctuation,
                PredefinedClassificationTypeNames.Operator => TagAllowance.Operator,
                "XAML Delimiter" => TagAllowance.Delimiter,
                "SQL Operator" => TagAllowance.Operator,
                "unnecessary code" => TagAllowance.Ignore,
                "ReSharper Dead Code" => TagAllowance.Ignore,
                _ => TagAllowance.Disallowed,
            };
            if (allowance != TagAllowance.Disallowed) return allowance;

            // Ignore tags for breakpoints
            if (classification.Contains("Breakpoint")) return TagAllowance.Debug;

            return TagAllowance.Disallowed;
        }
    }
}
