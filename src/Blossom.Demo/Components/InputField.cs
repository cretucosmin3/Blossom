using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class InputField : VisualElement
{
    private const float PadLeft = 10f;
    private const float PadRight = 10f;
    private const float CaretWidth = 1.5f;
    private const float CaretHeight = 16f;

    private string _value = "";
    private string _placeholder = "Type here...";
    private readonly VisualElement _selectionHighlight;
    private readonly VisualElement _textElement;
    private readonly VisualElement _caret;
    private bool _isFocused;
    private bool _isMouseDown;

    private int _caretIndex;
    private int _selectionAnchor;
    private float _scrollOffset;

    public Action<string>? Changed;
    public Action<string>? OnValueChanged;
    public Action<string>? Submitted;
    public Action<string>? OnSubmit;
    public Action? Escaped;

    public int CaretIndex
    {
        get => _caretIndex;
        set
        {
            _caretIndex = Math.Clamp(value, 0, GraphemeCountOf(_value));
            _selectionAnchor = _caretIndex;
            UpdateCaretAndSelection();
        }
    }

    public int SelectionStart => Math.Min(_selectionAnchor, _caretIndex);
    public int SelectionLength => Math.Abs(_selectionAnchor - _caretIndex);
    public bool HasSelection => SelectionLength > 0;
    public string SelectedText
    {
        get
        {
            if (!HasSelection) return "";
            int start = GraphemeToUtf16(_value, SelectionStart);
            int end = GraphemeToUtf16(_value, SelectionStart + SelectionLength);
            return _value.Substring(start, end - start);
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            var newVal = value ?? "";
            if (_value == newVal) return;

            _value = newVal;
            _caretIndex = Math.Clamp(_caretIndex, 0, GraphemeCountOf(_value));
            _selectionAnchor = _caretIndex;
            UpdateText();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        }
    }

    public string Placeholder
    {
        get => _placeholder;
        set
        {
            _placeholder = value ?? "";
            UpdateText();
        }
    }

    public InputField(string placeholder = "Type here...", string initialValue = "")
    {
        Name = $"InputField_{Guid.NewGuid().ToString()[..4]}";
        _placeholder = placeholder;
        _value = initialValue ?? "";
        _caretIndex = GraphemeCountOf(_value);
        _selectionAnchor = _caretIndex;

        ReceivesKeyboard = true;
        Cursor = StandardCursor.IBeam;
        OverflowX = OverflowMode.Clip;
        OverflowY = OverflowMode.Clip;

        Style = new ElementStyle
        {
            BackColor = new SKColor(23, 23, 23),
            Border = new BorderStyle
            {
                Width = 1,
                Color = new SKColor(58, 58, 58),
                Roundness = 6
            }
        };

        _selectionHighlight = new VisualElement
        {
            Name = $"{Name}_SelectionHighlight",
            IsClickthrough = true,
            Visible = false,
            Style = new ElementStyle
            {
                BackColor = new SKColor(59, 130, 246, 120),
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent, Roundness = 2 }
            }
        };

        _textElement = new VisualElement
        {
            Name = $"{Name}_Text",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = SKColors.Transparent,
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
                Text = new TextStyle
                {
                    Color = new SKColor(120, 120, 120),
                    Size = 13,
                    Weight = 400,
                    Spacing = 0,
                    Alignment = TextAlign.Left,
                    Overflow = TextOverflow.Visible,
                    MaxLines = 1,
                    Font = "Roboto",
                    Padding = 0
                }
            }
        };

        _caret = new VisualElement
        {
            Name = $"{Name}_Caret",
            IsClickthrough = true,
            Visible = false,
            Style = new ElementStyle
            {
                BackColor = new SKColor(230, 230, 230),
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent, Roundness = 1 }
            }
        };

        AddChild(_selectionHighlight);
        AddChild(_textElement);
        AddChild(_caret);

        Action<Theme> onTheme = _ =>
        {
            UpdateText();
            if (!_isFocused && Themes.Current.TryColour("border", out var idle))
                Style.Border.Color = idle;
            if (_isFocused && Themes.Current.TryColour("accent", out var focus))
                Style.Border.Color = focus;
            InvalidatePaint();
        };
        Themes.CurrentChanged += onTheme;
        Disposed += _ => Themes.CurrentChanged -= onTheme;

        UpdateText();

        OnFocused = _ =>
        {
            _isFocused = true;
            Style.Border.Color = Themes.Current.TryColour("accent", out var accent)
                ? accent
                : new SKColor(170, 170, 170);
            _caret.Visible = true;
            UpdateText();
            InvalidatePaint();
        };

        OnFocusLost = _ =>
        {
            _isFocused = false;
            _isMouseDown = false;
            _selectionAnchor = _caretIndex;
            Style.Border.Color = Themes.Current.TryColour("border", out var border)
                ? border
                : new SKColor(58, 58, 58);
            _caret.Visible = false;
            _selectionHighlight.Visible = false;
            if (HasPointerCapture) ReleasePointer();
            UpdateText();
            InvalidatePaint();
        };

        Events.OnMouseDown += (s, args) =>
        {
            if (args.Button != 0) return;

            if (ParentView != null)
            {
                ParentView.SetActiveKeyboardElement(this);
            }

            _isMouseDown = true;
            CapturePointer();
            args.Handled = true;

            int clickedIdx = GetGraphemeIndexAtPointer(args.Global.X, args.Global.Y);
            _caretIndex = clickedIdx;
            if (!Events.IsShiftDown)
            {
                _selectionAnchor = clickedIdx;
            }

            UpdateCaretAndSelection();
        };

        Events.OnMouseMove += (s, args) =>
        {
            if (!_isMouseDown) return;
            args.Handled = true;
            _caretIndex = GetGraphemeIndexAtPointer(args.Global.X, args.Global.Y);
            UpdateCaretAndSelection();
        };

        Events.OnMouseUp += (s, args) =>
        {
            if (args.Button != 0) return;
            _isMouseDown = false;
            if (HasPointerCapture) ReleasePointer();
        };

        Events.OnMouseDoubleClick += (s, args) =>
        {
            SelectWordAt(_caretIndex);
            args.Handled = true;
        };

        Events.OnTextInput += e =>
        {
            if (!_isFocused) return;
            if (Events.IsControlDown || Events.IsAltDown) return;

            bool inserted = false;
            foreach (var rune in e.Text.EnumerateRunes())
            {
                if (rune.Value < 32 || rune.Value == 127)
                    continue;
                InsertText(rune.ToString());
                inserted = true;
            }

            if (inserted)
                e.Handled = true;
        };

        Events.OnKeyDown += e =>
        {
            if (!_isFocused) return;
            bool isCtrl = e.Control;
            bool isShift = e.Shift;

            switch (e.Key)
            {
                case Key.Backspace:
                    HandleBackspace(isCtrl);
                    e.Handled = true;
                    break;
                case Key.Delete:
                    HandleDelete(isCtrl);
                    e.Handled = true;
                    break;
                case Key.Left:
                    HandleLeft(isCtrl, isShift);
                    e.Handled = true;
                    break;
                case Key.Right:
                    HandleRight(isCtrl, isShift);
                    e.Handled = true;
                    break;
                case Key.Home:
                    MoveCaret(0, isShift);
                    e.Handled = true;
                    break;
                case Key.End:
                    MoveCaret(GraphemeCountOf(_value), isShift);
                    e.Handled = true;
                    break;
                case Key.A when isCtrl:
                    SelectAll();
                    e.Handled = true;
                    break;
                case Key.C when isCtrl:
                    Shell.SetClipboardText(HasSelection ? SelectedText : _value);
                    e.Handled = true;
                    break;
                case Key.X when isCtrl:
                    if (HasSelection)
                    {
                        Shell.SetClipboardText(SelectedText);
                        DeleteSelection();
                        NotifyChanged();
                    }
                    e.Handled = true;
                    break;
                case Key.V when isCtrl:
                    HandlePaste();
                    e.Handled = true;
                    break;
                case Key.Enter:
                case Key.KeypadEnter:
                    Submitted?.Invoke(_value);
                    OnSubmit?.Invoke(_value);
                    ParentView?.SetActiveKeyboardElement(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    Escaped?.Invoke();
                    ParentView?.SetActiveKeyboardElement(null);
                    e.Handled = true;
                    break;
            }
        };
    }

    public void SelectAll()
    {
        _selectionAnchor = 0;
        _caretIndex = GraphemeCountOf(_value);
        UpdateCaretAndSelection();
    }

    public void Clear()
    {
        Value = "";
        _caretIndex = 0;
        _selectionAnchor = 0;
        UpdateText();
    }

    private void SelectWordAt(int index)
    {
        int count = GraphemeCountOf(_value);
        if (count == 0)
        {
            SelectAll();
            return;
        }

        int i = Math.Clamp(index, 0, count);
        if (i == count) i--;

        var clusters = new List<string>(count);
        var e = StringInfo.GetTextElementEnumerator(_value);
        while (e.MoveNext())
            clusters.Add(e.GetTextElement());

        static bool IsWs(string g)
        {
            if (string.IsNullOrEmpty(g)) return true;
            foreach (var r in g.EnumerateRunes())
            {
                if (!Rune.IsWhiteSpace(r))
                    return false;
            }
            return true;
        }

        bool hitWs = IsWs(clusters[i]);
        int start = i;
        int end = i;
        while (start > 0 && IsWs(clusters[start - 1]) == hitWs) start--;
        while (end < clusters.Count && IsWs(clusters[end]) == hitWs) end++;

        _selectionAnchor = start;
        _caretIndex = end;
        UpdateCaretAndSelection();
    }

    private void InsertText(string text)
    {
        if (HasSelection) DeleteSelection();
        int utf = GraphemeToUtf16(_value, _caretIndex);
        _value = _value.Insert(utf, text);
        _caretIndex += GraphemeCountOf(text);
        _selectionAnchor = _caretIndex;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        UpdateText();
        Changed?.Invoke(_value);
        OnValueChanged?.Invoke(_value);
    }

    private void HandleBackspace(bool isCtrl)
    {
        if (HasSelection)
        {
            DeleteSelection();
            NotifyChanged();
            return;
        }

        if (_caretIndex <= 0) return;

        var layout = LayoutFor(_value);
        int from = isCtrl
            ? layout.MoveByWord(_caretIndex, -1)
            : layout.MoveByGrapheme(_caretIndex, -1);
        int utfFrom = GraphemeToUtf16(_value, from);
        int utfTo = GraphemeToUtf16(_value, _caretIndex);
        _value = _value.Remove(utfFrom, utfTo - utfFrom);
        _caretIndex = from;
        _selectionAnchor = _caretIndex;
        NotifyChanged();
    }

    private void HandleDelete(bool isCtrl)
    {
        if (HasSelection)
        {
            DeleteSelection();
            NotifyChanged();
            return;
        }

        var layout = LayoutFor(_value);
        if (_caretIndex >= layout.GraphemeCount) return;

        int to = isCtrl
            ? layout.MoveByWord(_caretIndex, 1)
            : layout.MoveByGrapheme(_caretIndex, 1);
        int utfFrom = GraphemeToUtf16(_value, _caretIndex);
        int utfTo = GraphemeToUtf16(_value, to);
        _value = _value.Remove(utfFrom, utfTo - utfFrom);
        NotifyChanged();
    }

    private void HandleLeft(bool isCtrl, bool isShift)
    {
        var layout = LayoutFor(_value);
        int target = isCtrl
            ? layout.MoveByWord(_caretIndex, -1)
            : layout.MoveByGrapheme(_caretIndex, -1);
        if (!isShift && HasSelection)
        {
            target = SelectionStart;
        }
        MoveCaret(target, isShift);
    }

    private void HandleRight(bool isCtrl, bool isShift)
    {
        var layout = LayoutFor(_value);
        int target = isCtrl
            ? layout.MoveByWord(_caretIndex, 1)
            : layout.MoveByGrapheme(_caretIndex, 1);
        if (!isShift && HasSelection)
        {
            target = SelectionStart + SelectionLength;
        }
        MoveCaret(target, isShift);
    }

    private void MoveCaret(int index, bool isShift)
    {
        _caretIndex = Math.Clamp(index, 0, GraphemeCountOf(_value));
        if (!isShift) _selectionAnchor = _caretIndex;
        UpdateCaretAndSelection();
    }

    private void HandlePaste()
    {
        string paste = Shell.GetClipboardText();
        if (string.IsNullOrEmpty(paste)) return;
        paste = paste.Replace("\r", "").Replace("\n", " ");
        InsertText(paste);
    }

    private void DeleteSelection()
    {
        if (!HasSelection) return;
        int start = SelectionStart;
        int utfStart = GraphemeToUtf16(_value, start);
        int utfEnd = GraphemeToUtf16(_value, start + SelectionLength);
        _value = _value.Remove(utfStart, utfEnd - utfStart);
        _caretIndex = start;
        _selectionAnchor = start;
    }

    private SKPaint TextPaint => _textElement.Style.Text.Paint;
    private SKFont TextFont => _textElement.Style.Text.SkFont;

    private TextLayout LayoutFor(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new TextLayout();
        return TextLayout.Build(text, TextFont, TextPaint.Color, float.MaxValue, float.MaxValue, TextOverflow.Visible, 1);
    }

    private static int GraphemeCountOf(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        return new StringInfo(text).LengthInTextElements;
    }

    private static int GraphemeToUtf16(string text, int graphemeIndex)
    {
        if (string.IsNullOrEmpty(text) || graphemeIndex <= 0)
            return 0;
        var e = StringInfo.GetTextElementEnumerator(text);
        int i = 0;
        int utf = 0;
        while (e.MoveNext())
        {
            if (i >= graphemeIndex)
                break;
            utf += e.GetTextElement().Length;
            i++;
        }
        return Math.Min(utf, text.Length);
    }

    private int GetGraphemeIndexAtPointer(float globalX, float globalY)
    {
        float textOriginX = Transform.Computed.X + PadLeft - _scrollOffset;
        float textOriginY = Transform.Computed.Y;
        var layout = LayoutFor(_value);
        return layout.CaretIndexFromPoint(globalX - textOriginX, globalY - textOriginY);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = Transform.Width > 0 ? Transform.Width : 160f;
        float h = Transform.Height > 0 ? Transform.Height : 34f;
        h = Math.Max(h, 34f);
        if (maxWidth > 0) w = Math.Min(Math.Max(w, 80f), maxWidth);
        if (maxHeight > 0) h = Math.Min(h, maxHeight);
        return new SKSize(w, h);
    }

    protected override void LayoutChildren()
    {
        UpdateCaretAndSelection();
    }

    private void UpdateText()
    {
        bool empty = string.IsNullOrEmpty(_value);
        if (empty)
        {
            _textElement.Text = _placeholder;
            _textElement.Style.Text.Color = Themes.Current.TryColour("muted", out var muted)
                ? muted
                : new SKColor(120, 120, 120);
        }
        else
        {
            _textElement.Text = _value;
            _textElement.Style.Text.Color = Themes.Current.TryColour("text", out var text)
                ? text
                : SKColors.White;
        }

        if (Themes.Current.TryColour("text", out var caret))
            _caret.Style.BackColor = caret;
        if (Themes.Current.TryColour("accent", out var accent))
            _selectionHighlight.Style.BackColor = accent.WithAlpha(80);

        UpdateCaretAndSelection();
        InvalidatePaint();
    }

    private void UpdateCaretAndSelection()
    {
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;
        float fieldW = Math.Max(1f, Transform.Width);
        float fieldH = Math.Max(1f, Transform.Height);
        float viewW = Math.Max(10f, fieldW - PadLeft - PadRight);

        bool showingPlaceholder = string.IsNullOrEmpty(_value);
        var displayLayout = LayoutFor(showingPlaceholder ? (_placeholder ?? "") : _value);
        float caretTextW = showingPlaceholder ? 0f : displayLayout.CaretRect(_caretIndex).Left;
        float fullTextW = displayLayout.Width;
        float lineBox = displayLayout.Height > 1f ? displayLayout.Height : CaretHeight;

        if (caretTextW - _scrollOffset > viewW - 4f)
        {
            _scrollOffset = caretTextW - viewW + 8f;
        }
        else if (caretTextW - _scrollOffset < 0f)
        {
            _scrollOffset = Math.Max(0f, caretTextW - 8f);
        }

        if (showingPlaceholder || fullTextW <= viewW)
        {
            _scrollOffset = 0f;
        }

        float textX = originX + PadLeft - _scrollOffset;
        float textW = Math.Max(viewW, fullTextW + 8f);
        _textElement.Transform.SetAbsoluteFrame(textX, originY, textW, fieldH);

        if (_isFocused && HasSelection && !showingPlaceholder)
        {
            float selStartW = displayLayout.CaretRect(SelectionStart).Left;
            float selEndW = displayLayout.CaretRect(SelectionStart + SelectionLength).Left;
            float selX = originX + PadLeft + selStartW - _scrollOffset;
            float selW = Math.Max(2f, selEndW - selStartW);
            float selH = lineBox;
            float selY = originY + Math.Max(0, (fieldH - selH) / 2f);
            _selectionHighlight.Visible = true;
            _selectionHighlight.Transform.SetAbsoluteFrame(selX, selY, selW, selH);
        }
        else
        {
            _selectionHighlight.Visible = false;
        }

        float caretX = originX + PadLeft + caretTextW - _scrollOffset;
        float caretY = originY + Math.Max(0, (fieldH - lineBox) / 2f);
        _caret.Transform.SetAbsoluteFrame(caretX, caretY, CaretWidth, lineBox);
        _caret.Visible = _isFocused;

        InvalidatePaint();
    }
}
