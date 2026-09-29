using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
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
            _caretIndex = Math.Clamp(value, 0, _value.Length);
            _selectionAnchor = _caretIndex;
            UpdateCaretAndSelection();
        }
    }

    public int SelectionStart => Math.Min(_selectionAnchor, _caretIndex);
    public int SelectionLength => Math.Abs(_selectionAnchor - _caretIndex);
    public bool HasSelection => SelectionLength > 0;
    public string SelectedText => HasSelection ? _value.Substring(SelectionStart, SelectionLength) : "";

    public string Value
    {
        get => _value;
        set
        {
            var newVal = value ?? "";
            if (_value == newVal) return;

            _value = newVal;
            _caretIndex = Math.Clamp(_caretIndex, 0, _value.Length);
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
        _caretIndex = _value.Length;
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

        UpdateText();

        OnFocused = _ =>
        {
            _isFocused = true;
            Style.Border.Color = new SKColor(170, 170, 170);
            _caret.Visible = true;
            UpdateText();
            InvalidatePaint();
        };

        OnFocusLost = _ =>
        {
            _isFocused = false;
            _isMouseDown = false;
            _selectionAnchor = _caretIndex;
            Style.Border.Color = new SKColor(58, 58, 58);
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

            int clickedIdx = GetCharIndexAtPointer(args.Global.X);
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
            _caretIndex = GetCharIndexAtPointer(args.Global.X);
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

        Events.OnKeyType += ch =>
        {
            if (!_isFocused) return;
            if (Events.IsControlDown || Events.IsAltDown) return;
            if (char.IsControl(ch) || ch == 127 || ch < 32) return;

            InsertText(ch.ToString());
        };

        Events.OnKeyDown += k =>
        {
            if (!_isFocused) return;
            Key key = (Key)k;
            bool isCtrl = Events.IsControlDown;
            bool isShift = Events.IsShiftDown;

            switch (key)
            {
                case Key.Backspace:
                    HandleBackspace(isCtrl);
                    break;
                case Key.Delete:
                    HandleDelete(isCtrl);
                    break;
                case Key.Left:
                    HandleLeft(isCtrl, isShift);
                    break;
                case Key.Right:
                    HandleRight(isCtrl, isShift);
                    break;
                case Key.Home:
                    MoveCaret(0, isShift);
                    break;
                case Key.End:
                    MoveCaret(_value.Length, isShift);
                    break;
                case Key.A when isCtrl:
                    SelectAll();
                    break;
                case Key.C when isCtrl:
                    Browser.SetClipboardText(HasSelection ? SelectedText : _value);
                    break;
                case Key.X when isCtrl:
                    if (HasSelection)
                    {
                        Browser.SetClipboardText(SelectedText);
                        DeleteSelection();
                        NotifyChanged();
                    }
                    break;
                case Key.V when isCtrl:
                    HandlePaste();
                    break;
                case Key.Enter:
                case Key.KeypadEnter:
                    Submitted?.Invoke(_value);
                    OnSubmit?.Invoke(_value);
                    ParentView?.SetActiveKeyboardElement(null);
                    break;
                case Key.Escape:
                    Escaped?.Invoke();
                    ParentView?.SetActiveKeyboardElement(null);
                    break;
            }
        };
    }

    public void SelectAll()
    {
        _selectionAnchor = 0;
        _caretIndex = _value.Length;
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
        if (_value.Length == 0)
        {
            SelectAll();
            return;
        }

        int i = Math.Clamp(index, 0, _value.Length);
        if (i == _value.Length) i--;

        int start = i;
        int end = i;
        if (char.IsWhiteSpace(_value[i]))
        {
            while (start > 0 && char.IsWhiteSpace(_value[start - 1])) start--;
            while (end < _value.Length && char.IsWhiteSpace(_value[end])) end++;
        }
        else
        {
            while (start > 0 && !char.IsWhiteSpace(_value[start - 1])) start--;
            while (end < _value.Length && !char.IsWhiteSpace(_value[end])) end++;
        }

        _selectionAnchor = start;
        _caretIndex = end;
        UpdateCaretAndSelection();
    }

    private void InsertText(string text)
    {
        if (HasSelection) DeleteSelection();
        _value = _value.Insert(_caretIndex, text);
        _caretIndex += text.Length;
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

        if (isCtrl)
        {
            int prev = FindPreviousWordBoundary(_caretIndex);
            _value = _value.Remove(prev, _caretIndex - prev);
            _caretIndex = prev;
        }
        else
        {
            _value = _value.Remove(_caretIndex - 1, 1);
            _caretIndex--;
        }
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

        if (_caretIndex >= _value.Length) return;

        if (isCtrl)
        {
            int next = FindNextWordBoundary(_caretIndex);
            _value = _value.Remove(_caretIndex, next - _caretIndex);
        }
        else
        {
            _value = _value.Remove(_caretIndex, 1);
        }
        NotifyChanged();
    }

    private void HandleLeft(bool isCtrl, bool isShift)
    {
        int target = isCtrl ? FindPreviousWordBoundary(_caretIndex) : Math.Max(0, _caretIndex - 1);
        if (!isShift && HasSelection)
        {
            target = SelectionStart;
        }
        MoveCaret(target, isShift);
    }

    private void HandleRight(bool isCtrl, bool isShift)
    {
        int target = isCtrl ? FindNextWordBoundary(_caretIndex) : Math.Min(_value.Length, _caretIndex + 1);
        if (!isShift && HasSelection)
        {
            target = SelectionStart + SelectionLength;
        }
        MoveCaret(target, isShift);
    }

    private void MoveCaret(int index, bool isShift)
    {
        _caretIndex = Math.Clamp(index, 0, _value.Length);
        if (!isShift) _selectionAnchor = _caretIndex;
        UpdateCaretAndSelection();
    }

    private void HandlePaste()
    {
        string paste = Browser.GetClipboardText();
        if (string.IsNullOrEmpty(paste)) return;
        paste = paste.Replace("\r", "").Replace("\n", " ");
        InsertText(paste);
    }

    private void DeleteSelection()
    {
        if (!HasSelection) return;
        int start = SelectionStart;
        _value = _value.Remove(start, SelectionLength);
        _caretIndex = start;
        _selectionAnchor = start;
    }

    private int FindPreviousWordBoundary(int fromIdx)
    {
        if (fromIdx <= 0) return 0;
        int idx = fromIdx - 1;
        while (idx > 0 && char.IsWhiteSpace(_value[idx])) idx--;
        while (idx > 0 && !char.IsWhiteSpace(_value[idx - 1])) idx--;
        return Math.Max(0, idx);
    }

    private int FindNextWordBoundary(int fromIdx)
    {
        if (fromIdx >= _value.Length) return _value.Length;
        int idx = fromIdx;
        while (idx < _value.Length && !char.IsWhiteSpace(_value[idx])) idx++;
        while (idx < _value.Length && char.IsWhiteSpace(_value[idx])) idx++;
        return Math.Min(_value.Length, idx);
    }

    private SKPaint TextPaint => _textElement.Style.Text.Paint;

    private TextLayout LayoutFor(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new TextLayout();
        return TextLayout.Build(text, TextPaint, float.MaxValue, float.MaxValue, TextOverflow.Visible, 1);
    }

    private float MeasurePrefix(int length)
    {
        if (string.IsNullOrEmpty(_value) || length <= 0) return 0f;
        int safe = Math.Clamp(length, 0, _value.Length);
        if (safe == 0) return 0f;
        return LayoutFor(_value.Substring(0, safe)).Width;
    }

    private int GetCharIndexAtPointer(float globalX)
    {
        float textOriginX = Transform.Computed.X + PadLeft - _scrollOffset;
        float localX = globalX - textOriginX;
        return GetCharIndexAt(localX);
    }

    private int GetCharIndexAt(float localX)
    {
        if (string.IsNullOrEmpty(_value) || localX <= 0f) return 0;

        var layout = LayoutFor(_value);
        if (layout.Runs.Count == 0)
            return localX > 0f ? _value.Length : 0;

        using var paint = TextPaint.Clone();
        int charIndex = 0;
        foreach (var run in layout.Runs)
        {
            if (string.IsNullOrEmpty(run.Text))
                continue;

            paint.Typeface = run.Typeface;
            paint.TextSize = run.Size;
            float x = run.X;
            int i = 0;
            while (i < run.Text.Length)
            {
                int len = char.IsSurrogatePair(run.Text, i) ? 2 : 1;
                float charW = paint.MeasureText(run.Text.Substring(i, len));
                if (localX < x + charW * 0.5f)
                    return Math.Clamp(charIndex, 0, _value.Length);
                x += charW;
                charIndex += len;
                i += len;
            }
        }

        return _value.Length;
    }

    protected override void LayoutChildren()
    {
        UpdateCaretAndSelection();
    }

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(_value))
        {
            _textElement.Text = _placeholder;
            _textElement.Style.Text.Color = new SKColor(120, 120, 120);
        }
        else
        {
            _textElement.Text = _value;
            _textElement.Style.Text.Color = SKColors.White;
        }

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
        float caretTextW = showingPlaceholder ? 0f : MeasurePrefix(_caretIndex);
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
            float selStartW = MeasurePrefix(SelectionStart);
            float selEndW = MeasurePrefix(SelectionStart + SelectionLength);
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
