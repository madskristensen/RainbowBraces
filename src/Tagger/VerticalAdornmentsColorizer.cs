using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;

namespace RainbowBraces
{
    internal class VerticalAdornmentsColorizer
    {
        private static bool _failedReflection;
        private static readonly CachedType _wpfTextViewType;
        private static readonly CachedProperty<object> _wpfTextViewContent;
        private static readonly CachedType _canvasType;
        private static readonly CachedProperty<IList> _canvasChildren;
        private static readonly CachedType _viewStackType;
        private static readonly CachedProperty<IList> _viewStackChildren;
        private static readonly CachedType _adornmentLayerType;
        private static readonly CachedProperty<IList> _adornmentLayerElements;
        private static readonly CachedType _adornmentAndDataType;
        private static readonly CachedProperty<SnapshotSpan?> _adornmentAndDataVisualSpan;
        private static readonly CachedProperty<object> _adornmentAndDataAdornment;
        private static readonly CachedType _lineType;
        private static readonly CachedProperty<object> _lineStroke;
        private static Dictionary<string, Brush> _braceColors = new();

        private readonly IClassificationFormatMapService _formatMap;
        private Dictionary<int, IClassificationTag> _tagsByStart;
        private Dictionary<int, IClassificationTag> _tagsByEnd;
        private readonly HashSet<ITextView> _views = new();
        private bool _tagsAvailable;

        static VerticalAdornmentsColorizer()
        {
            _wpfTextViewType = new("WpfTextView");
            _wpfTextViewContent = new("Content", _wpfTextViewType);
            _canvasType = new("Canvas");
            _canvasChildren = new("Children", _canvasType);
            _viewStackType = new("ViewStack");
            _viewStackChildren = new("Children", _viewStackType);
            _adornmentLayerType = new("AdornmentLayer");
            _adornmentLayerElements = new("Elements", _adornmentLayerType);
            _adornmentAndDataType = new("AdornmentAndData");
            _adornmentAndDataVisualSpan = new("VisualSpan", _adornmentAndDataType);
            _adornmentAndDataAdornment = new("Adornment", _adornmentAndDataType);
            _lineType = new("Line");
            _lineStroke = new("Stroke", _lineType);

            General.Saved += OnSettingsSaved;
        }

        public VerticalAdornmentsColorizer(IClassificationFormatMapService formatMap)
        {
            _formatMap = formatMap;
        }

        public bool Enabled { get; set; } = true;

        public async Task ColorizeVerticalAdornmentsAsync(IReadOnlyCollection<ITextView> views, IReadOnlyList<ITagSpan<IClassificationTag>> tags)
        {
            if (_failedReflection) return;
            ProcessTags(tags);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (ITextView view in views)
            {
                // Register view if wasn't
                RegisterViewOnMainThread(view);

                // And colorize vertical lines because tags could get changed
                ProcessView(view);
            }
        }

        private void ProcessTags(IReadOnlyList<ITagSpan<IClassificationTag>> tags)
        {
            if (tags.Count == 0)
            {
                _tagsAvailable = false;
                _tagsByStart = null;
                _tagsByEnd = null;
                return;
            }

            Dictionary<int, IClassificationTag> tagsByStartPosition = new();
            Dictionary<int, IClassificationTag> tagsByEndPosition = new();
            foreach (ITagSpan<IClassificationTag> tagSpan in tags)
            {
                tagsByStartPosition[tagSpan.Span.Start.Position] = tagSpan.Tag;
                tagsByEndPosition[tagSpan.Span.End.Position] = tagSpan.Tag;
            }

            _tagsByStart = tagsByStartPosition;
            _tagsByEnd = tagsByEndPosition;
            _tagsAvailable = true;
        }

        private void DelayedProcessView(ITextView view, int delay)
        {
            Task.Run(
                async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ProcessView(view);
                }).FireAndForget();
        }

        private void ProcessView(ITextView view)
        {
            if (!Enabled) return;
            if (view.IsClosed) return;
            if (!_tagsAvailable) return;

            try
            {
                if (!_wpfTextViewContent.TryGet(view, out object canvas)) return;
                if (!_canvasChildren.TryGet(canvas, out IList canvasEls)) return;
                if (canvasEls is not { Count: > 1 }) return;
                object viewStack = canvasEls[1];
                if (!_viewStackChildren.TryGet(viewStack, out IList adornments)) return;
                foreach (object o in adornments)
                {
                    ProcessAdornment(o, view);
                }
            }
            catch
            {
                // ignore exception
            }
        }

        private void ProcessAdornment(object adornment, ITextView view)
        {
            try
            {
                if (!_adornmentLayerElements.TryGet(adornment, out IList elements)) return;
                foreach (object element in elements)
                {
                    ProcessAdornmentElement(element, view);
                }
            }
            catch
            {
                // ignore exception
            }
        }

        private void ProcessAdornmentElement(object element, ITextView view)
        {
            try
            {
                if (!_adornmentAndDataAdornment.TryGet(element, out object line)) return;
                if (!_lineType.IsOfType(line)) return;
                if (!_adornmentAndDataVisualSpan.TryGet(element, out SnapshotSpan? span) || span == null) return;
                if (!_tagsByEnd.TryGetValue(span.Value.End.Position, out IClassificationTag tag))
                {
                    // Try to find starting curly brackets in do { ... } while (...) construct
                    string text = span.Value.Snapshot.GetText(span.Value);
                    if (!text.StartsWith("do")) return;
                    int index = text.IndexOf('{');
                    if (index < 0 || !_tagsByStart.TryGetValue(index + span.Value.Start, out tag)) return;
                }
                Brush color = GetColor(tag, view);
                if (!_lineStroke.TrySet(line, color)) return;
            }
            catch
            {
                // ignore exception
            }
        }

        private Brush GetColor(IClassificationTag tag, ITextView view)
        {
            Dictionary<string, Brush> braceColors = _braceColors;
            IClassificationType classification = tag.ClassificationType;
            string classificationName = classification.Classification;
            if (!braceColors.TryGetValue(classificationName, out Brush brush))
            {
                IClassificationFormatMap formatMap = _formatMap.GetClassificationFormatMap(view);
                TextFormattingRunProperties formatProperties = formatMap.GetTextProperties(classification);
                brush = formatProperties.ForegroundBrush;
                braceColors.Add(classificationName, brush);
            }

            return brush;
        }

        public async Task RegisterViewAsync(ITextView view)
        {
            // sticky scroll container don't have vertical adornments
            if (view.Roles.Contains(CustomTextViewRoles.StickyScroll)) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            RegisterViewOnMainThread(view);
        }

        private void RegisterViewOnMainThread(ITextView view)
        {
            if (view.IsClosed) return;

            // If view wasn't registered yet subscribe to event handlers
            if (_views.Add(view))
            {
                view.Closed += View_OnClosed;
                view.LayoutChanged += View_OnLayoutChanged;
                view.GotAggregateFocus += View_OnGotAggregateFocus;
            }
        }

        private void View_OnGotAggregateFocus(object sender, EventArgs e)
        {
            ITextView view = (ITextView)sender;

            // When switching between tabs the view is recreated and vertical lines show up after some delay
            // In this case we can wait a little longer to show vertical lines
            DelayedProcessView(view, 1000);
        }

        private void View_OnClosed(object sender, EventArgs e)
        {
            ITextView view = (ITextView)sender;

            // Unregister view and unsubscribe all handlers
            _views.Remove(view);
            view.Closed -= View_OnClosed;
            view.LayoutChanged -= View_OnLayoutChanged;
            view.GotAggregateFocus -= View_OnGotAggregateFocus;
        }

        private void View_OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (!Enabled) return;

            ITextView view = (ITextView)sender;
            if (_tagsByEnd is not { Count: > 0 }) return;

            // Colorize vertical lines as soon as possible to reduce flickering
            ProcessView(view);

            // But not all vertical lines are constructed yet so wait some time and do the second pass
            DelayedProcessView(view, 10);
        }

        private static void OnSettingsSaved(General obj)
        {
            _braceColors = new Dictionary<string, Brush>();
        }

        private class CachedType
        {
            private readonly string _typeName;
            public Type Type { get; private set; }

            public CachedType(string typeName)
            {
                _typeName = typeName;
            }

            public bool IsOfType(object obj)
            {
                if (obj == null) return false;
                Type objType = obj.GetType();
                if (Type != null) return Type == objType;

                if (objType.Name != _typeName) return false;
                Type = objType;
                return true;
            }
        }

        private class CachedProperty<T>
        {
            private readonly string _propertyName;
            private readonly CachedType _cachedType;
            private PropertyInfo _propertyInfo;

            public CachedProperty(string propertyName, CachedType cachedType)
            {
                _propertyName = propertyName;
                _cachedType = cachedType;
            }

            private bool TryInit()
            {
                try
                {
                    _propertyInfo = _cachedType.Type.GetRuntimeProperty(_propertyName);
                    if (typeof(T) != typeof(object) && !typeof(T).IsAssignableFrom(_propertyInfo.PropertyType))
                    {
                        throw new Exception("Property type is not expected");
                    }

                    return true;
                }
                catch
                {
                    _failedReflection = true;
                    return false;
                }
            }

            public bool TryGet(object obj, out T value)
            {
                value = default;
                if (!_cachedType.IsOfType(obj)) return false;
                if (_propertyInfo == null && !TryInit()) return false;

                // _propertyInfo is not null when TryInit returns true
                value = (T)_propertyInfo!.GetValue(obj);
                return value != null;
            }

            public bool TrySet(object obj, T value)
            {
                if (!_cachedType.IsOfType(obj)) return false;
                if (_propertyInfo == null && !TryInit()) return false;

                // _propertyInfo is not null when TryInit returns true
                _propertyInfo!.SetValue(obj, value);
                return true;
            }
        }
    }
}
