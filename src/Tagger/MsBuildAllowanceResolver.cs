using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger
{
    public class MsBuildAllowanceResolver : DefaultAllowanceResolver
    {
        /// <inheritdoc />
        public override bool AllowXmlTags => true;

        protected override TagAllowance IsAllowed(IClassificationType tagType)
        {
            // Allow for conditions in attributes or other transformations
            if (tagType.IsOfType("XML Attribute Value")) return TagAllowance.Allowed;

            // Allow for property transformations as values
            if (tagType.IsOfType("XML Text")) return TagAllowance.Allowed;

            // Allow <,</,>,/> XML delimiters
            if (General.Instance.XmlTags && tagType.IsOfType("XML Delimiter")) return TagAllowance.XmlTag;

            return TagAllowance.Disallowed;
        }

        protected override TagAllowance IsAllowed(ILayeredClassificationType layeredType)
        {
            string classification = layeredType.Classification;

            // Allow for conditions in attributes or other transformations
            if (classification == "XML Attribute Value") return TagAllowance.Allowed;

            // Allow for property transformations as values
            if (classification == "XML Text") return TagAllowance.Allowed;

            // Allow <,</,>,/> XML delimiters
            if (General.Instance.XmlTags && classification == "XML Delimiter") return TagAllowance.XmlTag;

            return TagAllowance.Disallowed;
        }
    }
}
