using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace RainbowBraces.Tagger
{
    public abstract class AllowanceResolver
    {
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

        private TagAllowance GetAllowance(IClassificationType tagType)
        {
            if (tagType is ILayeredClassificationType layeredType) return IsAllowed(layeredType);
            else return IsAllowed(tagType);
        }

        protected abstract TagAllowance IsAllowed(IClassificationType tagType);

        protected abstract TagAllowance IsAllowed(ILayeredClassificationType layeredType);
    }
}
