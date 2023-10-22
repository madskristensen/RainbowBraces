using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RainbowBraces
{
    internal class VerticalAdornmentsColorizer
    {
        private static bool _failedReflection;
        private static readonly CachedType _wpfTextViewType;
        private static readonly CachedProperty<object> _wpfTextViewContent;
        private static readonly CachedType _viewStackType;
        private static readonly CachedProperty<IList> _viewStackChildren;
        private static readonly CachedType _adornmentLayerType;
        private static readonly CachedProperty<IList> _adornmentLayerElements;
        private static readonly CachedType _adornmentAndDataType;
        private static readonly CachedProperty<SnapshotSpan?> _adornmentAndDataVisualSpan;
        private static readonly CachedProperty<object> _adornmentAndDataAdornment;

        private static readonly CachedType _structureLine;
        private static readonly CachedField<Pen> _structureLinePen;

        private static Dictionary<string, Brush> _braceColors = new();
        private static Dictionary<Brush, Pen> _brushPens = new();

        private readonly IClassificationFormatMapService _formatMap;
        private Dictionary<int, IClassificationTag> _tagsByStart;
        private Dictionary<int, IClassificationTag> _tagsByEnd;
        private readonly HashSet<ITextView> _views = new();
        private bool _tagsAvailable;

        static VerticalAdornmentsColorizer()
        {
            _wpfTextViewType = new("WpfTextView");
            _wpfTextViewContent = new("Content", _wpfTextViewType);
            _viewStackType = new("ViewStack");
            _viewStackChildren = new("Children", _viewStackType);
            _adornmentLayerType = new("AdornmentLayer");
            _adornmentLayerElements = new("Elements", _adornmentLayerType);
            _adornmentAndDataType = new("AdornmentAndData");
            _adornmentAndDataVisualSpan = new("VisualSpan", _adornmentAndDataType);
            _adornmentAndDataAdornment = new("Adornment", _adornmentAndDataType); 

            // Preview version use custom line to draw vertical adornments
            _structureLine = new("StructureLine");
            _structureLinePen = new("drawingPen", _structureLine, false); // StructureLine is only used in newer versions of Visual Studio

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
                if (!_wpfTextViewContent.TryGet(view, out object content) || content is not Canvas canvas) return;
                UIElementCollection canvasEls = canvas.Children;
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
                if (!_adornmentAndDataAdornment.TryGet(element, out object verticalLine)) return;
                if (verticalLine is not Line && !_structureLine.IsOfType(verticalLine)) return;
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
                if (verticalLine is Line line)
                {
                    line.Stroke = color;
                }
                else
                {
                    // Try get actual pen ..
                    if (!_structureLinePen.TryGet(verticalLine, out Pen pen)) return;

                    // .. and replace it with new color pen ..
                    Pen newPen = GetPen(color, pen);
                    if (!_structureLinePen.TrySet(verticalLine, newPen)) return;

                    // .. and force it to rerender
                    if (verticalLine is UIElement uiElement)
                    {
                        uiElement.InvalidateVisual();
                    }
                }
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

        private static Pen GetPen(Brush brush, Pen originalPen)
        {
            // We assume that all pens are same so we can use cached pens by color
            Dictionary<Brush, Pen> brushPens = _brushPens;
            if (!brushPens.TryGetValue(brush, out Pen pen))
            {
                pen = originalPen.Clone();
                pen.Brush = brush;
                brushPens.Add(brush, pen);
            }
            return pen;
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
            // Clear cached colors
            _braceColors = new Dictionary<string, Brush>();
            _brushPens = new Dictionary<Brush, Pen>();
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

        private abstract class CachedMember<TMember>
        {
            private readonly string _name;
            private readonly bool _globalFail;
            private readonly CachedType _cachedType;
            private bool _failed;
            private TMember _member;

            protected CachedMember(string name, CachedType cachedType, bool globalFail)
            {
                _name = name;
                _cachedType = cachedType;
                _globalFail = globalFail;
            }

            private void TryInit()
            {
                if (_failed) return;

                try
                {
                    _member = GetMember(_cachedType.Type, _name);
                    CheckMember(_member);
                }
                catch
                {
                    if (_globalFail)
                    {
                        _failedReflection = true;
                    }
                    else
                    {
                        _failed = true;
                    }
                }
            }

            protected bool EnsureInitialized(object obj, out TMember member)
            {
                member = default;
                if (_failed) return false;
                if (!_cachedType.IsOfType(obj)) return false;

                if (_member == null)
                {
                    TryInit();
                }

                member = _member;
                return member != null;
            }

            protected abstract TMember GetMember(Type type, string name);

            protected virtual void CheckMember(TMember member)
            {

            }
        }

        private abstract class CachedFieldPropertyMember<TMember, TValue> : CachedMember<TMember>
        {
            protected CachedFieldPropertyMember(string name, CachedType cachedType, bool globalFail) : base(name, cachedType, globalFail)
            {
            }

            protected override void CheckMember(TMember member)
            {
                Type type = GetMemberType(member);
                if (typeof(TValue) != typeof(object) && !typeof(TValue).IsAssignableFrom(type))
                {
                    throw new Exception("Member type is not expected");
                }
            }

            public bool TryGet(object obj, out TValue value)
            {
                value = default;
                if (!EnsureInitialized(obj, out TMember member)) return false;

                try
                {
                    value = GetValue(member, obj);
                }
                catch
                {
                    return false;
                }
                return value != null;
            }

            public bool TrySet(object obj, TValue value)
            {
                if (!EnsureInitialized(obj, out TMember member)) return false;

                try
                {
                    SetValue(member, obj, value);
                }
                catch
                {
                    return false;
                }
                return true;
            }

            protected abstract Type GetMemberType(TMember member);

            protected abstract TValue GetValue(TMember member, object obj);

            protected abstract void SetValue(TMember member, object obj, TValue value);
        }

        private class CachedProperty<TValue> : CachedFieldPropertyMember<PropertyInfo, TValue>
        {
            public CachedProperty(string propertyName, CachedType cachedType, bool globalFail = true) : base(propertyName, cachedType, globalFail)
            {
            }

            protected override PropertyInfo GetMember(Type type, string name) => type.GetRuntimeProperty(name);

            protected override Type GetMemberType(PropertyInfo propertyInfo) => propertyInfo.PropertyType;

            protected override TValue GetValue(PropertyInfo propertyInfo, object obj) => (TValue)propertyInfo.GetValue(obj);

            protected override void SetValue(PropertyInfo propertyInfo, object obj, TValue value) => propertyInfo.SetValue(obj, value);
        }

        private class CachedField<TValue> : CachedFieldPropertyMember<FieldInfo, TValue>
        {
            public CachedField(string fieldName, CachedType cachedType, bool globalFail = true) : base(fieldName, cachedType, globalFail)
            {
            }

            protected override FieldInfo GetMember(Type type, string name) => type.GetRuntimeFields().First(f => f.Name == name);

            protected override Type GetMemberType(FieldInfo member) => member.FieldType;

            protected override TValue GetValue(FieldInfo member, object obj) => (TValue)member.GetValue(obj);

            protected override void SetValue(FieldInfo member, object obj, TValue value) => member.SetValue(obj, value);
        }
    }
}
