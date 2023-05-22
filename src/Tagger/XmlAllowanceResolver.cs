using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger
{
    public class XmlAllowanceResolver : DefaultAllowanceResolver
    {
        private readonly string _allowedDelimiter;

        public XmlAllowanceResolver(string allowedDelimiter, bool allowHtmlVoidElement = false)
        {
            _allowedDelimiter = allowedDelimiter;
            AllowHtmlVoidElement = allowHtmlVoidElement;
        }

        /// <inheritdoc />
        public override bool AllowXmlTags => true;

        /// <inheritdoc />
        public override bool AllowHtmlVoidElement { get; }

        /// <inheritdoc />
        protected override TagAllowance IsAllowed(IClassificationType tagType)
        {
            if (General.Instance.XmlTags)
            {
                // Allow <,</,>,/> XML delimiters
                if (tagType.IsOfType(_allowedDelimiter)) return TagAllowance.XmlTag;
            }

            return base.IsAllowed(tagType);
        }

        /// <inheritdoc />
        protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
        {
            string classification = layeredType.Classification;

            if (General.Instance.XmlTags)
            {
                // Allow <,</,>,/> XML delimiters
                if (classification == _allowedDelimiter) return TagAllowance.XmlTag;
            }

            return base.IsAllowed(layeredType);
        }
    }
}