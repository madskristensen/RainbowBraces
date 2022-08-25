using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;

namespace RainbowBraces
{
    public class RainbowTagger : ITagger<IClassificationTag>
    {
        private readonly ITextBuffer2 _buffer;
        private readonly IClassificationTypeRegistryService _registry;
        private readonly ITagAggregator<IClassificationTag> _aggregator;
        private readonly Debouncer _debouncer = new(250);
        private List<ITagSpan<IClassificationTag>> _tags = new();

        public RainbowTagger(ITextBuffer buffer, IClassificationTypeRegistryService registry, ITagAggregator<IClassificationTag> aggregator)
        {
            _buffer = (ITextBuffer2)buffer;
            _registry = registry;
            _aggregator = aggregator;
            _buffer.Changed += OnChanged;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ParseAsync();
            }).FireAndForget();
        }


        private void OnChanged(object sender, TextContentChangedEventArgs e)
        {
            _debouncer.Debouce(() => ParseAsync().FireAndForget());
        }

        IEnumerable<ITagSpan<IClassificationTag>> ITagger<IClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans[0].IsEmpty)
            {
                return null;
            }

            return _tags.Where(p => spans[0].IntersectsWith(p.Span.Span)).ToArray();
        }

        public async Task ParseAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IEnumerable<IMappingTagSpan<IClassificationTag>> spans = _aggregator.GetTags(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)).ToArray();
            await TaskScheduler.Default;

            List<BracePair> parentheses = new();
            List<BracePair> curlies = new();
            List<BracePair> squares = new();
            List<BracePair> angles = new();

            foreach (IMappingTagSpan<IClassificationTag> mappingSpan in spans)
            {
                if (!mappingSpan.Tag.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Punctuation) &&
                    !mappingSpan.Tag.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Operator))
                {
                    continue;
                }

                SnapshotSpan span = mappingSpan.Span.GetSpans(_buffer).FirstOrDefault();

                if (span == null || span.Length != 1)
                {
                    continue;
                }

                char c = span.GetText()[0];

                Span braceSpan = new(span.Start, 1);

                if (c == '(' || c == ')')
                {
                    BuildPairs(parentheses, c, braceSpan, '(', ')');
                }
                else if (c == '{' || c == '}')
                {
                    BuildPairs(curlies, c, braceSpan, '{', '}');
                }
                else if (c == '[' || c == ']')
                {
                    BuildPairs(squares, c, braceSpan, '[', ']');
                }
                //else if (c == '<' || c == '>')
                //{
                //    BuildPairs(angles, c, braceSpan, '<', '>');
                //}
            }

            IEnumerable<BracePair> pairs = CleanPairs(parentheses.Union(curlies).Union(squares).Union(angles)).OrderBy(p => p.Open.Start);
            List<ITagSpan<IClassificationTag>> tags = GenerateTagSpans(pairs);

            _tags = tags;
            TagsChanged?.Invoke(this, new(new(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
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
            int level = Math.Min(pairs.Count(p => p.Close.IsEmpty) + 1, 10);
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

        private IEnumerable<BracePair> CleanPairs(IEnumerable<BracePair> pairs)
        {
            return from p in pairs
                   where p.Close.Start - p.Open.Start > 1
                   select p;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
