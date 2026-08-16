using System;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class InputField : VisualElement
{
    private string _value = "";
    private string _placeholder = "Type here...";
    private readonly VisualElement _selectionHighlight;
    private readonly VisualElement _textElement;
    private readonly VisualElement _caret;
    private bool _isFocused;
    private bool _isMouseDown;

    private int _caretIndex = 0;
    private int _selectionAnchor = 0;
    private float _scrollOffset = 0f;

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
            if (_value != newVal)
            {
                _value = newVal;
                _caretIndex = Math.Clamp(_caretIndex, 0, _value.Length);
                _selectionAnchor = _caretIndex;
                UpdateText();
                Changed?.Invoke(_value);
                OnValueChanged?.Invoke(_value);
            }
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

        Style = new ElementStyle
        {
            BackColor = new SKColor(23, 23, 23), // Gray 900
            Border = new BorderStyle
            {
                Width = 1,
                Color = new SKColor(58, 58, 58), // Gray 700
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
                BackColor = new SKColor(59, 130, 246, 120), // Translucent Accent Blue
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent, Roundness = 2 }
            }
        };

        _textElement = new VisualElement
        {
            Name = $"{Name}_Text",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(120, 120, 120), // Gray 500
                    Size = 13,
                    Weight = 400,
                    Alignment = TextAlign.Left
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
                BackColor = new SKColor(220, 220, 220) // Bright accent
            }
        };

        AddChild(_selectionHighlight);
        AddChild(_textElement);
        AddChild(_caret);

        UpdateText();

        OnFocused = (s) =>
        {
            _isFocused = true;
            Style.Border.Color = new SKColor(170, 170, 170); // Gray accent focus border
            _caret.Visible = true;
            UpdateText();
            InvalidatePaint();
        };

        OnFocusLost = (s) =>
        {
            _isFocused = false;
            _isMouseDown = false;
            _selectionAnchor = _caretIndex;
            Style.Border.Color = new SKColor(58, 58, 58); // Gray 700 border
            _caret.Visible = false;
            _selectionHighlight.Visible = false;
            UpdateText();
            InvalidatePaint();
        };

        Events.OnMouseDown += (s, args) =>
        {
            if (!_isFocused && ParentView != null)
            {
                ParentView.SetActiveKeyboardElement(this);
            }

            _isMouseDown = true;
            float localX = args.Global.X - (Transform.Computed.X + 10f) + _scrollOffset;
            int clickedIdx = GetCharIndexAt(localX);

            if (Events.IsShiftDown)
            {
                _caretIndex = clickedIdx;
            }
            else
            {
                _caretIndex = clickedIdx;
                _selectionAnchor = clickedIdx;
            }

            _caret.Visible = true;
            UpdateCaretAndSelection();
        };

        Events.OnMouseMove += (s, args) =>
        {
            if (_isMouseDown && _isFocused)
            {
                float localX = args.Global.X - (Transform.Computed.X + 10f) + _scrollOffset;
                _caretIndex = GetCharIndexAt(localX);
                UpdateCaretAndSelection();
            }
        };

        Events.OnMouseUp += (s, args) =>
        {
            _isMouseDown = false;
        };

        Events.OnMouseDoubleClick += (s, args) =>
        {
            SelectAll();
        };

        Events.OnKeyType += (ch) =>
        {
            if (!_isFocused) return;
            if (char.IsControl(ch) || ch == 127 || ch < 32) return;

            if (HasSelection)
            {
                DeleteSelection();
            }

            _value = _value.Insert(_caretIndex, ch.ToString());
            _caretIndex++;
            _selectionAnchor = _caretIndex;

            UpdateText();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        };

        Events.OnKeyDown += (k) =>
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
                    _caretIndex = 0;
                    if (!isShift) _selectionAnchor = 0;
                    UpdateCaretAndSelection();
                    break;

                case Key.End:
                    _caretIndex = _value.Length;
                    if (!isShift) _selectionAnchor = _value.Length;
                    UpdateCaretAndSelection();
                    break;

                case Key.A when isCtrl:
                    SelectAll();
                    break;

                case Key.C when isCtrl:
                    if (HasSelection)
                    {
                        Browser.SetClipboardText(SelectedText);
                    }
                    else
                    {
                        Browser.SetClipboardText(_value);
                    }
                    break;

                case Key.X when isCtrl:
                    if (HasSelection)
                    {
                        Browser.SetClipboardText(SelectedText);
                        DeleteSelection();
                        UpdateText();
                        Changed?.Invoke(_value);
                        OnValueChanged?.Invoke(_value);
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

    private void HandleBackspace(bool isCtrl)
    {
        if (HasSelection)
        {
            DeleteSelection();
            UpdateText();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        }
        else if (_caretIndex > 0)
        {
            if (isCtrl)
            {
                int prevWordIdx = FindPreviousWordBoundary(_caretIndex);
                int count = _caretIndex - prevWordIdx;
                _value = _value.Remove(prevWordIdx, count);
                _caretIndex = prevWordIdx;
                _selectionAnchor = _caretIndex;
            }
            else
            {
                _value = _value.Remove(_caretIndex - 1, 1);
                _caretIndex--;
                _selectionAnchor = _caretIndex;
            }
            UpdateText();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        }
    }

    private void HandleDelete(bool isCtrl)
    {
        if (HasSelection)
        {
            DeleteSelection();
            UpdateText();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        }
        else if (_caretIndex < _value.Length)
        {
            if (isCtrl)
            {
                int nextWordIdx = FindNextWordBoundary(_caretIndex);
                int count = nextWordIdx - _caretIndex;
                _value = _value.Remove(_caretIndex, count);
            }
            else
            {
                _value = _value.Remove(_caretIndex, 1);
            }
            UpdateText();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        }
    }

    private void HandleLeft(bool isCtrl, bool isShift)
    {
        int targetIdx = isCtrl ? FindPreviousWordBoundary(_caretIndex) : Math.Max(0, _caretIndex - 1);
        if (isShift)
        {
            _caretIndex = targetIdx;
        }
        else
        {
            if (HasSelection)
            {
                targetIdx = SelectionStart;
            }
            _caretIndex = targetIdx;
            _selectionAnchor = targetIdx;
        }
        UpdateCaretAndSelection();
    }

    private void HandleRight(bool isCtrl, bool isShift)
    {
        int targetIdx = isCtrl ? FindNextWordBoundary(_caretIndex) : Math.Min(_value.Length, _caretIndex + 1);
        if (isShift)
        {
            _caretIndex = targetIdx;
        }
        else
        {
            if (HasSelection)
            {
                targetIdx = SelectionStart + SelectionLength;
            }
            _caretIndex = targetIdx;
            _selectionAnchor = targetIdx;
        }
        UpdateCaretAndSelection();
    }

    private void HandlePaste()
    {
        string paste = Browser.GetClipboardText();
        if (string.IsNullOrEmpty(paste)) return;

        // Clean single line text
        paste = paste.Replace("\r", "").Replace("\n", " ");

        if (HasSelection)
        {
            DeleteSelection();
        }

        _value = _value.Insert(_caretIndex, paste);
        _caretIndex += paste.Length;
        _selectionAnchor = _caretIndex;

        UpdateText();
        Changed?.Invoke(_value);
        OnValueChanged?.Invoke(_value);
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

    private int GetCharIndexAt(float localX)
    {
        if (string.IsNullOrEmpty(_value) || localX <= 0f) return 0;

        using var paint = GetMeasurePaint();
        float currentX = 0f;
        for (int i = 0; i < _value.Length; i++)
        {
            float charW = paint.MeasureText(_value[i].ToString());
            if (localX < currentX + charW / 2f)
            {
                return i;
            }
            currentX += charW;
        }
        return _value.Length;
    }

    private SKPaint GetMeasurePaint()
    {
        return new SKPaint
        {
            Typeface = Blossom.Utils.Fonts.GetTypeface("Roboto", 400),
            TextSize = 13,
            IsAntialias = true
        };
    }

    private float MeasureSubstr(int start, int length)
    {
        if (string.IsNullOrEmpty(_value) || length <= 0 || start >= _value.Length) return 0f;
        int safeLength = Math.Min(length, _value.Length - start);
        using var paint = GetMeasurePaint();
        return paint.MeasureText(_value.Substring(start, safeLength));
    }

    protected override void LayoutChildren()
    {
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;
        const float padLeft = 10f;
        const float padRight = 10f;
        float textW = Math.Max(0, Transform.Width - (padLeft + padRight));
        float textH = Math.Max(1f, Transform.Height);

        _textElement.Transform.SetAbsoluteFrame(originX + padLeft - _scrollOffset, originY, textW + _scrollOffset, textH);
        UpdateCaretAndSelection();
    }

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(_value))
        {
            _textElement.Text = _placeholder;
            _textElement.Style.Text.Color = new SKColor(120, 120, 120); // Gray 500
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
        const float padLeft = 10f;
        const float padRight = 10f;
        float viewW = Math.Max(10f, Transform.Width - (padLeft + padRight));

        float caretTextW = MeasureSubstr(0, _caretIndex);

        // Adjust scroll offset to keep caret inside viewable area
        if (caretTextW - _scrollOffset > viewW)
        {
            _scrollOffset = caretTextW - viewW + 12f;
        }
        else if (caretTextW - _scrollOffset < 0)
        {
            _scrollOffset = Math.Max(0f, caretTextW - 12f);
        }

        if (string.IsNullOrEmpty(_value))
        {
            _scrollOffset = 0f;
        }

        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;

        // Position text element with scroll offset
        float textW = Math.Max(0, Transform.Width - (padLeft + padRight));
        float textH = Math.Max(1f, Transform.Height);
        _textElement.Transform.SetAbsoluteFrame(originX + padLeft - _scrollOffset, originY, textW + _scrollOffset, textH);

        // Position selection highlight
        if (_isFocused && HasSelection)
        {
            float selStartW = MeasureSubstr(0, SelectionStart);
            float selLenW = MeasureSubstr(SelectionStart, SelectionLength);
            float selX = originX + padLeft + selStartW - _scrollOffset;
            float selH = 18f;
            float selY = originY + Math.Max(0, (Transform.Height - selH) / 2f);

            _selectionHighlight.Visible = true;
            _selectionHighlight.Transform.SetAbsoluteFrame(selX, selY, selLenW, selH);
        }
        else
        {
            _selectionHighlight.Visible = false;
        }

        // Position caret
        float caretX = originX + padLeft + caretTextW - _scrollOffset;
        float caretHeight = 16f;
        float caretY = originY + Math.Max(0, (Transform.Height - caretHeight) / 2f);
        _caret.Transform.SetAbsoluteFrame(caretX, caretY, 2f, caretHeight);
        _caret.Visible = _isFocused;

        InvalidatePaint();
    }
}
