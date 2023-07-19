using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace RainbowBraces.Tagger
{
    /// <summary>
    /// TODO WIP
    /// </summary>
    public class CPlusPlusAllowanceResolver : DefaultAllowanceResolver
    {
        /// <inheritdoc />
        /// <remarks>The C++ tags are generated in second phase.</remarks>
        public override bool CanChangeTags => true;

        private readonly Dictionary<(int Start, int End), string> _classificationByPosition = new ();

        private static readonly HashSet<string> _allowedPreviousOpenClassifications;
        private static readonly HashSet<string> _allowedPreviousCloseClassifications;
        private static readonly HashSet<string> _allowedNextOpenClassifications;
        private static readonly HashSet<string> _allowedClassifications;

        static CPlusPlusAllowanceResolver()
        {
            _allowedPreviousOpenClassifications = new()
            {
                "cppClassTemplate", // shared_ptr<...>
                "cppFunction", // make_shared<...>()
                "cppType", // enable_if_t<...>
                "cppGlobalVariable", // is_convertible_t<...>
                "cppLocalVariable", // x = variable<string>()
                "keyword", // template <...>
                "identifier" // <TFunc<...>>
            };

            _allowedPreviousCloseClassifications = new()
            {
                "cppType", // <TRet>
                "keyword", // <bool>
                "operator" // <TArgs...>
            };

            _allowedNextOpenClassifications = new()
            {
                "cppClassTemplate", // template<Concept T>
                "cppType", // <enable_if_t<...>>
                "cppGlobalVariable", // enable_if_t<is_invocable_v<...>>
                "keyword", // <bool>
                "identifier", // <invalid_token_but_preserve_brace_pairs>
                "cppNamespace" // <std::string>
            };

            _allowedClassifications = _allowedPreviousOpenClassifications
                .Concat(_allowedPreviousCloseClassifications)
                .Concat(_allowedNextOpenClassifications)
                .ToHashSet();
        }

        public override void Prepare()
        {
            _classificationByPosition.Clear();
        }

        public override void Cleanup()
        {
            _classificationByPosition.Clear();
        }

        protected override TagAllowance GetAllowance(IClassificationType tagType, IMappingSpan span)
        {
            // We'll remember important classifications.
            if (_allowedClassifications.Contains(tagType.Classification))
            {
                ITextBuffer buffer = span.AnchorBuffer;
                foreach (SnapshotSpan snapshotSpan in span.GetSpans(buffer))
                {
                    (int, int) position = (snapshotSpan.Start.Position, snapshotSpan.End.Position);
                    string classification = tagType.Classification;
                    _classificationByPosition[position] = classification;
                }
            }

            return base.GetAllowance(tagType, span);
        }

        public string GetClassification(int start, int end)
        {
            _classificationByPosition.TryGetValue((start, end), out string classification);
            return classification;
        }

        /// <inheritdoc />
        public override BracePairBuilderCollection CreateBuilders(General options)
        {
            // Create builders for each brace type only specific to C++
            BracePairBuilderCollection builders = new();
            if (options.Parentheses) builders.AddBuilder('(', ')');
            if (options.CurlyBrackets) builders.AddBuilder('{', '}');
            if (options.SquareBrackets) builders.AddBuilder('[', ']');
            if (options.ExperimentalCPlusPlusGenerics) builders.AddBuilder(collection => new CPlusPlusTemplateTagPairBuilder(collection, this));

            return builders;
        }
        
        /// <summary>
        /// Return <see langword="true"/> if classification is allowed to be before opening '&lt;' in C++ template.
        /// </summary>
        public static bool IsValidPreviousOpen(string classification) => _allowedPreviousOpenClassifications.Contains(classification);

        /// <summary>
        /// Return <see langword="true"/> if classification is allowed to be after opening '&lt;' in C++ template.
        /// </summary>
        public static bool IsValidPreviousClose(string classification) => _allowedPreviousCloseClassifications.Contains(classification);

        /// <summary>
        /// Return <see langword="true"/> if classification is allowed to be before closing '&gt;' in C++ template.
        /// </summary>
        public static bool IsValidNextOpen(string classification) => _allowedNextOpenClassifications.Contains(classification);
    }
}