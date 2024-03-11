using Microsoft.VisualStudio.Text;
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
            IMappingSpan span = tagSpan.Span;

            TagAllowance allowance;
            if (tagType is ILayeredClassificationType layeredType)
            {
                allowance = GetAllowance(layeredType, span);
                if (allowance != TagAllowance.Disallowed) return allowance;
                foreach (IClassificationType baseType in layeredType.BaseTypes)
                {
                    allowance = GetAllowance(baseType, span);
                    if (allowance != TagAllowance.Disallowed) return allowance;
                }
            }
            else
            {
                allowance = GetAllowance(tagType, span);
                if (allowance != TagAllowance.Disallowed) return allowance;
            }

            return TagAllowance.Disallowed;
        }

        /// <summary>
        /// Creates builder collection specific for current resolver with respect to <paramref name="options"/>.
        /// </summary>
        public virtual BracePairBuilderCollection CreateBuilders(General options)
        {
            // Create builders for each brace type
            BracePairBuilderCollection builders = new();

            if (options.Parentheses) builders.AddBuilder('(', ')', options.ParenthesesUseGlobalStack);

            if (options.CurlyBrackets) builders.AddBuilder('{', '}', options.CurlyBracketsUseGlobalStack);

            if (options.SquareBrackets) builders.AddBuilder('[', ']', options.SquareBracketsUseGlobalStack);

            // Allow only punctuation.
            // Ignore XML tags, in rare circumstances, Razor tagger will mark regular generics operator as XML tag, so we'll ignore it.
            if (options.AngleBrackets) builders.AddBuilder('<', '>', options.AngleBracketsUseGlobalStack, Singleton.Punctuation, Singleton.XmlTag);

            if (options.XmlTags && AllowXmlTags) builders.AddXmlTagBuilder(AllowHtmlVoidElement, options.AngleBracketsUseGlobalStack);

            return builders;
        }

        /// <summary>
        /// Method invoked before processing tags. Can be used to initialize resources,
        /// </summary>
        public virtual void Prepare()
        {

        }

        /// <summary>
        /// Method invoked after processed tags. Can be used to cleanup resources.
        /// </summary>
        public virtual void Cleanup()
        {

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

        /// <summary>
        /// If this property is <see langword="true"/> resolver can return <see cref="TagAllowance.XmlTag"/>.
        /// This property is for performance optimization.
        /// </summary>
        public virtual bool AllowXmlTags => false;

        /// <summary>
        /// If this property is <see langword="true"/> resolver will treat HTML void elements as self-closed 
        /// and <see cref="AllowXmlTags"/> is also expected to return <see langword="true"/>.
        /// Otherwise HTML void element are treated as malformed XML document.
        /// https://developer.mozilla.org/en-US/docs/Glossary/Void_element
        /// </summary>
        public virtual bool AllowHtmlVoidElement => false;

        protected virtual TagAllowance GetAllowance(IClassificationType tagType, IMappingSpan span)
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

        private class Singleton
        {
            public static TagAllowance[] Punctuation { get; } = { TagAllowance.Punctuation };

            public static TagAllowance[] XmlTag { get; } = { TagAllowance.XmlTag };
        }
    }
}