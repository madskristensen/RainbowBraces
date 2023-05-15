using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger;

public class RazorAllowanceResolver : DefaultAllowanceResolver
{
    /// <inheritdoc />
    /// <remarks>
    /// Braces in HTML text should not be treated as pairs.
    /// </remarks>
    public override bool DefaultAllowed => false;

    /// <inheritdoc />
    /// <remarks>
    /// Tags are changed multiple times in razor templates, we need to listen to these changes.
    /// </remarks>
    public override bool CanChangeTags => true;

    /// <inheritdoc />
    protected override TagAllowance IsAllowed(IClassificationType tagType)
    {
        // string (eg. interpolated or parameter filling) tag can contain punctuation or inlined C# code and braces without tags are ignored by default (DefaultAllowed = false)
        if (tagType.IsOfType(PredefinedClassificationTypeNames.String)) return TagAllowance.Ignore; 

        return base.IsAllowed(tagType);
    }

    /// <inheritdoc />
    protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
    {
        string classification = layeredType.Classification;

        // string (eg. interpolated or parameter filling) tag can contain punctuation or inlined C# code and braces without tags are ignored by default (DefaultAllowed = false)
        if (classification == PredefinedClassificationTypeNames.String) return TagAllowance.Ignore;

        return base.IsAllowed(layeredType);
    }
}