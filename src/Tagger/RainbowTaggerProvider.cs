using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace RainbowBraces
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypes.CPlusPlus)]
    [ContentType(ContentTypes.CSharp)]
    [ContentType(ContentTypes.Css)]
    [ContentType(ContentTypes.Less)]
    [ContentType(ContentTypes.Scss)]
    [ContentType(ContentTypes.Json)]
    [ContentType(ContentTypes.Xaml)]
    [ContentType("TypeScript")]
    [ContentType("SQL")]
    [ContentType("SQL Server Tools")]
    [ContentType("php")]
    [ContentType("Code++")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TagType(typeof(IClassificationTag))]
    public class CreationListener : IViewTaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService _registry = null;

        [Import]
        internal IViewTagAggregatorFactoryService _aggregator = null;

        private bool _isProcessing;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Calling CreateTagAggregator creates a recursive situation, so _isProcessing ensures it only runs once per textview.
            if (_isProcessing)
            {
                return null;
            }

            _isProcessing = true;

            try
            {
                ITagAggregator<IClassificationTag> aggregator = _aggregator.CreateTagAggregator<IClassificationTag>(textView);
                return buffer.Properties.GetOrCreateSingletonProperty(() => new RainbowTagger(textView, buffer, _registry, aggregator)) as ITagger<T>;
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}
