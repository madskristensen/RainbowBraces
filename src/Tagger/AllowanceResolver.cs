using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace RainbowBraces.Tagger
{
    public abstract class AllowanceResolver
    {
        /// <summary>
        /// Returns allowance category for given tag.
        /// </summary>
        public TagAllowance GetAllowance(IMappingTagSpan<IClassificationTag> tagSpan)
        {
            IClassificationType tagType = tagSpan.Tag.ClassificationType;

            TagAllowance allowance;
            if (tagType is ILayeredClassificationType layeredType)
            {
                allowance = GetAllowance(layeredType);
                if (allowance != TagAllowance.Disallowed) return allowance;
                foreach (IClassificationType baseType in layeredType.BaseTypes)
                {
                    allowance = GetAllowance(baseType);
                    if (allowance != TagAllowance.Disallowed) return allowance;
                }
            }
            else
            {
                allowance = GetAllowance(tagType);
                if (allowance != TagAllowance.Disallowed) return allowance;
            }

            return TagAllowance.Disallowed;
        }

        /// <summary>
        /// If this property is <see langword="true"/> brace should be considered pair if is not in another tag.
        /// Otherwise brace will be treated as unrelated text.
        /// </summary>
        public virtual bool DefaultAllowed => true;

        /// <summary>
        /// If this property is <see langword="true"/> we expect tags from other sources to change dynamically
        /// and so we'll have to listen to these changes a parse tags more often without user changes.
        /// </summary>
        public virtual bool CanChangeTags => false;

        private TagAllowance GetAllowance(IClassificationType tagType)
        {
            if (tagType is ILayeredClassificationType layeredType) return IsAllowed(layeredType);
            else return IsAllowed(tagType);
        }

        /// <summary>
        /// Implementation for allowance resolution for <see cref="IClassificationType"/>.
        /// </summary>
        protected abstract TagAllowance IsAllowed(IClassificationType tagType);

        /// <summary>
        /// Implementation for allowance resolution for <see cref="ILayeredClassificationType"/>.
        /// </summary>
        protected abstract TagAllowance IsAllowed(ILayeredClassificationType layeredType);
    }
}