using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace RainbowBraces
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TagType(typeof(IClassificationTag))]
    public class CreationListener : IViewTaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService _registry = null;

        [Import]
        internal IViewTagAggregatorFactoryService _aggregator = null;

        public bool _isProcessing { get; set; }
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (!_isProcessing)
            {
                _isProcessing = true;
                ITagAggregator<IClassificationTag> aggregator = _aggregator.CreateTagAggregator<IClassificationTag>(textView);
                _isProcessing = false;

                return buffer.Properties.GetOrCreateSingletonProperty(() => new RainbowTagger(buffer, _registry, aggregator)) as ITagger<T>;
            }

            return null;
        }
    }
}
