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
    [ContentType(ContentTypes.Xml)]
    [ContentType(ContentTypes.WebForms)]
    [ContentType("TypeScript")]
    [ContentType("SQL")]
    [ContentType("SQL Server Tools")]
    [ContentType("php")]
    [ContentType("phalanger")]
    [ContentType("Code++")]
    [ContentType("XSharp")]
    [ContentType("Razor")]
    [ContentType("LegacyRazorVisualBasic")]
    [ContentType("WebForms")]
    [ContentType("html-delegation")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(CustomTextViewRoles.StickyScroll)]
    [TextViewRole(CustomTextViewRoles.Diff)]
    [TextViewRole(CustomTextViewRoles.Repl)]
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
            if (!IsSupportedBuffer(textView, buffer) || _isProcessing)
            {
                return null;
            }

            _isProcessing = true;

            try
            {
                ITagger<T> result = buffer.Properties.GetOrCreateSingletonProperty(() => new RainbowTagger(textView, buffer, _registry, _aggregator, _formatMap)) as ITagger<T>;
                (result as RainbowTagger)?.AddView(textView);
                return result;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static bool IsSupportedBuffer(ITextView textView, ITextBuffer buffer)
        {
            if (textView.TextBuffer == buffer) return true;

            // Sticky scroll container is allowed for colorization.
            if (textView.Roles.Contains(CustomTextViewRoles.StickyScroll)) return true;

            // Inline diff editor is allowed for colorization. (but before and after codes are mixed and can looks weird)
            if (textView.Roles.Contains(CustomTextViewRoles.InlineDiff)) return true;

            // Allow REPL text view for scripts.
            if (textView.Roles.Contains(CustomTextViewRoles.Repl)) return true;

            // HTML/WebForms textview don't use HTML buffer but only HTMLProjection or WebFormsProjection.
            if (IsSuportedHtmlTextBufferContentType(textView.TextBuffer.ContentType) && IsSupportedHtmlContentType(buffer.ContentType)) return true;
            return false;

            static bool IsSuportedHtmlTextBufferContentType(IContentType contentType)
            {
                if (contentType.IsOfType("HTMLProjection")) return true;
                if (contentType.IsOfType("WebFormsProjection")) return true;
                if (contentType.IsOfType("html-delegation")) return true;
                return false;
            }

            static bool IsSupportedHtmlContentType(IContentType contentType)
            {
                if (contentType.IsOfType("HTML")) return true;
                if (contentType.IsOfType("Basic")) return true;
                if (contentType.IsOfType("CSharp")) return true;
                if (contentType.IsOfType("WebForms")) return true;
                if (contentType.IsOfType("LegacyRazorCSharp")) return true;
                if (contentType.IsOfType("html-delegation")) return true;
                return false;
            }
        }
    }
}
