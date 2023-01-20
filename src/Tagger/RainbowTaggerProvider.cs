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
    [ContentType(ContentTypes.VisualBasic)]
    [ContentType(ContentTypes.FSharp)]
    [ContentType(ContentTypes.Css)]
    [ContentType(ContentTypes.Less)]
    [ContentType(ContentTypes.Scss)]
    [ContentType(ContentTypes.Json)]
    [ContentType(ContentTypes.Xaml)]
    [ContentType("TypeScript")]
    [ContentType("SQL")]
    [ContentType("SQL Server Tools")]
    [ContentType("php")]
    [ContentType("phalanger")]
    [ContentType("Code++")]
    [ContentType("XSharp")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TagType(typeof(IClassificationTag))]
    public class CreationListener : IViewTaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService _registry = null;

        [Import]
        internal IViewTagAggregatorFactoryService _aggregator = null;

        [Import]
        internal IClassificationFormatMapService _formatMap = null;

        private bool _isProcessing;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Calling CreateTagAggregator creates a recursive situation, so _isProcessing ensures it only runs once per textview.
            if (textView.TextBuffer != buffer || _isProcessing)
            {
                return null;
            }

            _isProcessing = true;

            try
            {
                ITagger<T> result = buffer.Properties.GetOrCreateSingletonProperty(() =>
                {
                    ITagAggregator<IClassificationTag> aggregator = _aggregator.CreateTagAggregator<IClassificationTag>(textView);
                    return new RainbowTagger(textView, buffer, _registry, aggregator, _formatMap);
                }) as ITagger<T>;
                (result as RainbowTagger)?.AddView(textView);
                return result;
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}
