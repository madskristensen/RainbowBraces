using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace RainbowBraces
{
    public class RainbowTagger : ITagger<IClassificationTag>
    {
        private static bool _ratingCounted;
        private readonly ITextBuffer2 _buffer;
        private readonly ITextView _view;
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITagAggregator<IClassificationTag> _aggregator;
        private readonly Debouncer _debouncer;
        private List<ITagSpan<IClassificationTag>> _tags = new();
        private bool _isEnabled;

        public RainbowTagger(ITextView view, IClassificationTypeRegistryService registry, ITagAggregator<IClassificationTag> aggregator)
        {
            _buffer = (ITextBuffer2)view.TextBuffer;
            _view = view;
            _registry = registry;
            _aggregator = aggregator;
            _isEnabled = General.Instance.Enabled;
            _debouncer = new(General.Instance.Timeout);

            _buffer.PostChanged += OnBufferChanged;
            view.Closed += OnViewClosed;
            General.Saved += OnSettingsSaved;

            if (_isEnabled)
            {
                ParseAsync().FireAndForget();
            }
        }

        private void OnSettingsSaved(General settings)
        {

            if (settings.Enabled)
            {
                ParseAsync().FireAndForget();
            }
            else
            {
                _tags.Clear();
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
            }

            _isEnabled = settings.Enabled;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            ITextView view = (ITextView)sender;
            view.TextBuffer.PostChanged -= OnBufferChanged;
            view.Closed -= OnViewClosed;
            General.Saved -= OnSettingsSaved;
        }

        private void OnBufferChanged(object sender, EventArgs e)
        {
            if (_isEnabled)
            {
                _debouncer.Debouce(() => { _ = ParseAsync(); });
            }
        }

        IEnumerable<ITagSpan<IClassificationTag>> ITagger<IClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_tags.Count == 0 || spans.Count == 0 || spans[0].IsEmpty)
            {
                return null;
            }

            return _tags.Where(p => spans[0].IntersectsWith(p.Span.Span));
        }

        private static readonly Regex _regex = new(@"[\{\}\(\)\[\]]", RegexOptions.Compiled);

        public async Task ParseAsync()
        {
            await Task.Yield();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            List<BracePair> pairs = new();

            General options = await General.GetLiveInstanceAsync();

            foreach (ITextSnapshotLine line in _buffer.CurrentSnapshot.Lines)
            {
                if (line.Extent.IsEmpty)
                {
                    continue;
                }

                string text = line.GetText();
                MatchCollection matches = _regex.Matches(text);

                if (matches.Count == 0)
                {
                    continue;
                }

                IEnumerable<SnapshotSpan> disallow = _aggregator.GetTags(line.Extent)
                                                        .Where(t => !t.Tag.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Punctuation) &&
                                                                    !t.Tag.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Operator) &&
                                                                    !t.Tag.ClassificationType.IsOfType("XAML Delimiter"))
                                                        .SelectMany(d => d.Span.GetSpans(_buffer)).ToArray();

                foreach (Match match in matches)
                {
                    char c = match.Value[0];
                    int position = line.Start + match.Index;

                    if (disallow.Any(s => s.Start <= position && s.End > position))
                    {
                        continue;
                    }

                    Span braceSpan = new(position, 1);

                    if (options.Parentheses && (c == '(' || c == ')'))
                    {
                        BuildPairs(pairs, c, braceSpan, '(', ')');
                    }
                    else if (options.CurlyBrackets && (c == '{' || c == '}'))
                    {
                        BuildPairs(pairs, c, braceSpan, '{', '}');
                    }
                    else if (options.SquareBrackets && (c == '[' || c == ']'))
                    {
                        BuildPairs(pairs, c, braceSpan, '[', ']');
                    }
                }
            }

            _tags = GenerateTagSpans(pairs);

            int start = _view.TextViewLines.First().Start.Position;
            int end = _view.TextViewLines.Last().End.Position;
            TagsChanged?.Invoke(this, new(new(_buffer.CurrentSnapshot, start, end - start)));

            HandleRatingPrompt(_tags.Count > 0);
        }

        private static void HandleRatingPrompt(bool hasTags)
        {
            if (hasTags && !_ratingCounted)
            {
                _ratingCounted = true;
                RatingPrompt prompt = new("MadsKristensen.RainbowBraces", Vsix.Name, General.Instance, 10);
                prompt.RegisterSuccessfulUsage();
            }
        }

        private List<ITagSpan<IClassificationTag>> GenerateTagSpans(IEnumerable<BracePair> pairs)
        {
            List<ITagSpan<IClassificationTag>> tags = new();

            foreach (BracePair pair in pairs)
            {
                IClassificationType classification = _registry.GetClassificationType(ClassificationTypes.GetName(pair.Level));
                ClassificationTag openTag = new(classification);
                SnapshotSpan openSpan = new(_buffer.CurrentSnapshot, pair.Open);
                tags.Add(new TagSpan<IClassificationTag>(openSpan, openTag));

                ClassificationTag closeTag = new(classification);
                SnapshotSpan closeSpan = new(_buffer.CurrentSnapshot, pair.Close);
                tags.Add(new TagSpan<IClassificationTag>(closeSpan, closeTag));
            }

            return tags;
        }

        private void BuildPairs(List<BracePair> pairs, char match, Span braceSpan, char open, char close)
        {
            int level = pairs.Count(p => p.Close.IsEmpty) + 1;
            BracePair pair = new() { Level = level };

            if (match == open)
            {
                pair.Open = braceSpan;
                pairs.Add(pair);
            }
            else if (match == close)
            {
                pair = pairs.Where(kvp => kvp.Close.IsEmpty).LastOrDefault();
                if (pair != null)
                {
                    pair.Close = braceSpan;
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
