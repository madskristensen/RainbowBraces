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
        if (General.Instance.XmlTags && tagType.IsOfType("HTML Tag Delimiter")) return TagAllowance.XmlTag;

        return base.IsAllowed(tagType);
    }

    /// <inheritdoc />
    protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
    {
        if (General.Instance.XmlTags && layeredType.Classification is "HTML Tag Delimiter") return TagAllowance.XmlTag;

        return base.IsAllowed(layeredType);
    }
}