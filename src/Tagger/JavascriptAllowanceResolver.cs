namespace RainbowBraces.Tagger
{
    public class JavascriptAllowanceResolver : DefaultAllowanceResolver
    {
        /// <inheritdoc />
        /// <remarks>
        /// Javascript tags can be loaded lazily. Relational operators (&gt; or &lt;) can be not registered at start.
        /// </remarks>
        public override bool CanChangeTags => true;
    }
}