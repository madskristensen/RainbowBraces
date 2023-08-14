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
    public override bool AllowXmlTags => true;

    /// <inheritdoc />
    public override bool AllowHtmlVoidElement => true;

    /// <inheritdoc />
    protected override TagAllowance IsAllowed(IClassificationType tagType)
    {
        // string (eg. interpolated or parameter filling) tag can contain punctuation or inlined C# code and braces without tags are ignored by default (DefaultAllowed = false)
        if (tagType.IsOfType(PredefinedClassificationTypeNames.String)) return TagAllowance.Ignore;

        if (General.Instance.XmlTags)
        {
            if (tagType.IsOfType("HTML Tag Delimiter")) return TagAllowance.XmlTag;
            if (tagType.IsOfType("HTML Operator")) return TagAllowance.XmlTag;
        }

        return base.IsAllowed(tagType);
    }

    /// <inheritdoc />
    protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
    {
        string classification = layeredType.Classification;

        // string (eg. interpolated or parameter filling) tag can contain punctuation or inlined C# code and braces without tags are ignored by default (DefaultAllowed = false)
        if (classification == PredefinedClassificationTypeNames.String) return TagAllowance.Ignore;

        if (General.Instance.XmlTags)
        {
            if (layeredType.Classification is "HTML Tag Delimiter") return TagAllowance.XmlTag;
            if (layeredType.Classification is "HTML Operator") return TagAllowance.XmlTag;
        }

        return base.IsAllowed(layeredType);
    }
}