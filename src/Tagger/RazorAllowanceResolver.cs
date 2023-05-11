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
}