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
        private List<BracePair> _braces = new();
        private bool _isEnabled;

        public RainbowTagger(ITextView view, ITextBuffer buffer, IClassificationTypeRegistryService registry, ITagAggregator<IClassificationTag> aggregator)
        {
            _buffer = (ITextBuffer2)buffer;
            _view = view;
            _registry = registry;
            _aggregator = aggregator;
            _isEnabled = General.Instance.Enabled;
            _debouncer = new(General.Instance.Timeout);

            _buffer.Changed += OnBufferChanged;
            view.Closed += OnViewClosed;
            General.Saved += OnSettingsSaved;

            if (_isEnabled)
            {
                _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(async () =>
                {
                    await ParseAsync();
                }, VsTaskRunContext.UIThreadIdlePriority);
            }
        }

        private void OnSettingsSaved(General settings)
        {
            if (settings.Enabled)
            {
                _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(async () =>
                {
                    await ParseAsync();
                }, VsTaskRunContext.UIThreadIdlePriority);
            }
            else
            {
                _braces.Clear();
                _tags.Clear();
                int visibleStart = _view.TextViewLines.First().Start.Position;
                int visibleEnd = _view.TextViewLines.Last().End.Position;
                TagsChanged?.Invoke(this, new(new(_buffer.CurrentSnapshot, visibleStart, visibleEnd - visibleStart)));
            }

            _isEnabled = settings.Enabled;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            ITextView view = (ITextView)sender;
            view.TextBuffer.Changed -= OnBufferChanged;
            view.Closed -= OnViewClosed;
            General.Saved -= OnSettingsSaved;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (_isEnabled && e.Changes.Count > 0)
            {
                int startPosition = e.Changes.Min(change => change.OldPosition);
                _debouncer.Debouce(() => { _ = ParseAsync(startPosition); });
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
        private static readonly Span _empty = new(0, 0);

        public async Task ParseAsync(int changedPosition = 0)
        {
            General options = await General.GetLiveInstanceAsync();
            
            if (_buffer.CurrentSnapshot.LineCount > options.MaxBufferLines)
            {
                await VS.StatusBar.ShowMessageAsync($"No rainbow braces. File too big ({_buffer.CurrentSnapshot.LineCount} lines).");
                return;
            }
            
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ITextSnapshotLine changedLine = _buffer.CurrentSnapshot.GetLineFromPosition(changedPosition);
            List<BracePair> pairs = new();

            if (changedLine.Start > 0)
            {
                // Use the cache for all brackets defined above the position of the change
                pairs.AddRange(_braces.Where(p => p.Open.End <= changedLine.Start || p.Close.End <= changedLine.Start));
                pairs.ForEach(p => p.Close = p.Close.End > changedLine.Start ? _empty : p.Close);
            }
            
            foreach (ITextSnapshotLine line in _buffer.CurrentSnapshot.Lines)
            {
                if (line.End < changedLine.Start || line.Extent.IsEmpty)
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

            _braces = pairs;
            _tags = GenerateTagSpans(pairs);

            int visibleStart = Math.Max(_view.TextViewLines.First().Start.Position, changedLine.Start);
            int visibleEnd = _view.TextViewLines.Last().End.Position;
            TagsChanged?.Invoke(this, new(new(_buffer.CurrentSnapshot, visibleStart, visibleEnd - visibleStart)));

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
