using System;
using System.Diagnostics;
using System.Linq;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Silk.NET.Input;

namespace Blossom.Primitives;

/// <summary>
/// Headless shell for single-line text input controls: manages the text buffer,
/// cursor navigation, selection range, clipboard operations (copy/cut/paste),
/// undo/caret blinking, and keyboard/mouse interaction. Does not enforce any visual appearance.
/// </summary>
public class TextInputBase : Control
{
    private const int MultiClickThresholdMs = 400;
    private const int BlinkPeriodMs = 530;

    private string _text = string.Empty;
    private string _placeholder = string.Empty;
    private int? _maxLength;
    private bool _isPassword;
    private bool _digitsOnly;
    private bool _isReadOnly;

    private int _caret;
    private int _anchor;
    private bool _isFocused;
    private bool _isCaretShown;
    private bool _dragging;
    private long _lastMouseDownMs;
    private int _clickCount;
    private IDisposable? _blinkTimer;

    public TextInputBase()
    {
        ReceivesKeyboard = true;
        Cursor = StandardCursor.IBeam;

        OnFocused += _ => SetFocused(true);
        OnFocusLost += _ => SetFocused(false);

        Events.OnMouseDown += OnPointerDown;
        Events.OnMouseMove += OnPointerMove;
        Events.OnMouseUp += OnPointerUp;
        Events.OnKeyDown += OnKeyDown;
        Events.OnTextInput += OnTextInput;
    }

    /// <summary>Raised when the text content is modified by user interaction.</summary>
    public event Action<string>? Changed;

    /// <summary>Raised when Enter is pressed.</summary>
    public event Action? Submitted;

    /// <summary>Raised when Escape is pressed.</summary>
    public event Action? Escaped;

    public new string Text
    {
        get => _text;
        set
        {
            string sanitized = Sanitize(value ?? string.Empty);
            if (_text == sanitized)
                return;
            _text = sanitized;
            _caret = Math.Clamp(_caret, 0, _text.Length);
            _anchor = _caret;
            OnTextModified();
            InvalidatePaint();
        }
    }

    public string Placeholder
    {
        get => _placeholder;
        set => SetAndPaint(ref _placeholder, value ?? string.Empty);
    }

    public int? MaxLength
    {
        get => _maxLength;
        set => SetAndPaint(ref _maxLength, value);
    }

    public bool IsPassword
    {
        get => _isPassword;
        set => SetAndPaint(ref _isPassword, value);
    }

    public bool DigitsOnly
    {
        get => _digitsOnly;
        set => SetAndPaint(ref _digitsOnly, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetAndPaint(ref _isReadOnly, value);
    }

    public bool IsFocused => _isFocused;

    public bool IsCaretShown => _isCaretShown;

    public int CaretIndex => _caret;

    public int AnchorIndex => _anchor;

    public int SelectionStart => Math.Min(_anchor, _caret);

    public int SelectionLength => Math.Abs(_anchor - _caret);

    public bool HasSelection => SelectionLength > 0;

    public string SelectedText => HasSelection ? _text.Substring(SelectionStart, SelectionLength) : string.Empty;

    /// <summary>Selects the entire text range.</summary>
    public void SelectAll()
    {
        _anchor = 0;
        _caret = _text.Length;
        CaretMoved();
    }

    /// <summary>Clears any active selection, collapsing to the current caret position.</summary>
    public void ClearSelection()
    {
        _anchor = _caret;
        CaretMoved();
    }

    /// <summary>Selects the word surrounding <paramref name="index"/>.</summary>
    public void SelectWordAt(int index)
    {
        if (_text.Length == 0 || IsPassword)
        {
            SelectAll();
            return;
        }

        int pos = Math.Clamp(index, 0, Math.Max(0, _text.Length - 1));
        int cls = CharClass(_text[pos]);
        int start = pos;
        int end = pos + 1;

        while (start > 0 && CharClass(_text[start - 1]) == cls)
            start--;
        while (end < _text.Length && CharClass(_text[end]) == cls)
            end++;

        _anchor = start;
        _caret = end;
        CaretMoved();
    }

    /// <summary>Moves the caret to <paramref name="index"/>, optionally extending the selection.</summary>
    public void MoveCaret(int index, bool extendSelection = false)
    {
        _caret = Math.Clamp(index, 0, _text.Length);
        if (!extendSelection)
            _anchor = _caret;
        CaretMoved();
    }

    /// <summary>Replaces the active selection (or inserts at caret) with <paramref name="content"/>.</summary>
    public void InsertText(string content)
    {
        if (IsReadOnly || !Enabled)
            return;

        string sanitized = Sanitize(content);
        if (sanitized.Length == 0 && !HasSelection)
            return;

        int start = SelectionStart;
        int selLen = SelectionLength;
        int currentLenWithoutSel = _text.Length - selLen;

        if (MaxLength.HasValue && MaxLength.Value > 0)
        {
            int room = Math.Max(0, MaxLength.Value - currentLenWithoutSel);
            if (sanitized.Length > room)
                sanitized = sanitized[..room];
        }

        string next = _text.Remove(start, selLen).Insert(start, sanitized);
        _text = next;
        _caret = start + sanitized.Length;
        _anchor = _caret;

        OnTextModified();
        CaretMoved();
        Changed?.Invoke(_text);
    }

    /// <summary>Deletes the currently selected text.</summary>
    public void DeleteSelection()
    {
        if (IsReadOnly || !Enabled || !HasSelection)
            return;

        int start = SelectionStart;
        string next = _text.Remove(start, SelectionLength);
        _text = next;
        _caret = _anchor = start;

        OnTextModified();
        CaretMoved();
        Changed?.Invoke(_text);
    }

    /// <summary>Copies the selected text to the system clipboard.</summary>
    public void Copy()
    {
        if (HasSelection && !IsPassword)
            Shell.SetClipboardText(SelectedText);
    }

    /// <summary>Cuts the selected text to the system clipboard.</summary>
    public void Cut()
    {
        if (IsReadOnly || !Enabled || !HasSelection || IsPassword)
            return;

        Copy();
        DeleteSelection();
    }

    /// <summary>Pastes text from the system clipboard into the input.</summary>
    public void Paste()
    {
        if (IsReadOnly || !Enabled)
            return;

        string clip = Shell.GetClipboardText();
        if (!string.IsNullOrEmpty(clip))
            InsertText(clip);
    }

    /// <summary>Calculates the character index corresponding to <paramref name="localX"/>.</summary>
    protected virtual int IndexAt(float localX) => _text.Length;

    /// <summary>Calculates the horizontal visual advance width of the first <paramref name="charCount"/> characters.</summary>
    protected virtual float PrefixWidth(int charCount) => 0f;

    protected virtual void OnTextModified() { }

    protected virtual void OnSelectionChanged() { }

    protected virtual void OnCaretMoved() { }

    private void SetFocused(bool focused)
    {
        if (_isFocused == focused)
            return;

        _isFocused = focused;
        _blinkTimer?.Dispose();
        _blinkTimer = null;

        if (focused)
        {
            _isCaretShown = true;
            _blinkTimer = Shell.PostPeriodic(TimeSpan.FromMilliseconds(BlinkPeriodMs), () =>
            {
                if (!_isFocused) return;
                _isCaretShown = !_isCaretShown;
                InvalidatePaint();
            });
        }
        else
        {
            _isCaretShown = false;
            _dragging = false;
            _anchor = _caret;
        }

        OnStateChanged();
        InvalidatePaint();
    }

    private void CaretMoved()
    {
        _isCaretShown = true;
        OnSelectionChanged();
        OnCaretMoved();
        InvalidatePaint();
    }

    private void OnPointerDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != 0 || !Enabled)
            return;

        long now = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        _clickCount = (now - _lastMouseDownMs < MultiClickThresholdMs) ? _clickCount + 1 : 1;
        _lastMouseDownMs = now;

        int index = IndexAt(e.Relative.X);

        if (_clickCount == 2)
        {
            SelectWordAt(index);
            e.Handled = true;
            return;
        }

        if (_clickCount >= 3)
        {
            SelectAll();
            e.Handled = true;
            return;
        }

        MoveCaret(index, extendSelection: false);
        _dragging = true;
        CapturePointer();
        e.Handled = true;
    }

    private void OnPointerMove(object? sender, MouseEventArgs e)
    {
        if (_dragging && HasPointerCapture)
        {
            MoveCaret(IndexAt(e.Relative.X), extendSelection: true);
            e.Handled = true;
        }
    }

    private void OnPointerUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == 0 && _dragging)
        {
            _dragging = false;
            ReleasePointer();
            e.Handled = true;
        }
    }

    private void OnKeyDown(KeyEvent e)
    {
        if (!Enabled)
            return;

        bool ctrl = e.Control;
        bool shift = e.Shift;

        switch (e.Key)
        {
            case Key.A when ctrl:
                SelectAll();
                e.Handled = true;
                break;

            case Key.C when ctrl:
                Copy();
                e.Handled = true;
                break;

            case Key.X when ctrl:
                Cut();
                e.Handled = true;
                break;

            case Key.V when ctrl:
                Paste();
                e.Handled = true;
                break;

            case Key.Left:
                if (ctrl)
                    MoveCaret(WordLeft(_caret), shift);
                else if (shift)
                    MoveCaret(Prev(_caret), true);
                else if (HasSelection)
                    MoveCaret(SelectionStart, false);
                else
                    MoveCaret(Prev(_caret), false);
                e.Handled = true;
                break;

            case Key.Right:
                if (ctrl)
                    MoveCaret(WordRight(_caret), shift);
                else if (shift)
                    MoveCaret(Next(_caret), true);
                else if (HasSelection)
                    MoveCaret(SelectionStart + SelectionLength, false);
                else
                    MoveCaret(Next(_caret), false);
                e.Handled = true;
                break;

            case Key.Home:
                MoveCaret(0, shift);
                e.Handled = true;
                break;

            case Key.End:
                MoveCaret(_text.Length, shift);
                e.Handled = true;
                break;

            case Key.Backspace:
                if (HasSelection)
                    DeleteSelection();
                else if (ctrl)
                {
                    int left = WordLeft(_caret);
                    _anchor = left;
                    DeleteSelection();
                }
                else if (_caret > 0)
                {
                    int prev = Prev(_caret);
                    _anchor = prev;
                    DeleteSelection();
                }
                e.Handled = true;
                break;

            case Key.Delete:
                if (HasSelection)
                    DeleteSelection();
                else if (ctrl)
                {
                    int right = WordRight(_caret);
                    _anchor = right;
                    DeleteSelection();
                }
                else if (_caret < _text.Length)
                {
                    int next = Next(_caret);
                    _anchor = next;
                    DeleteSelection();
                }
                e.Handled = true;
                break;

            case Key.Enter:
                Submitted?.Invoke();
                e.Handled = true;
                break;

            case Key.Escape:
                Escaped?.Invoke();
                e.Handled = true;
                break;
        }
    }

    private void OnTextInput(TextEvent e)
    {
        if (!Enabled || IsReadOnly)
            return;

        InsertText(e.Text);
        e.Handled = true;
    }

    private string Sanitize(string s)
    {
        if (DigitsOnly)
            return string.Concat(s.Where(char.IsAsciiDigit));

        return s.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }

    private int Prev(int i) =>
        i <= 0 ? 0 : (i >= 2 && char.IsSurrogatePair(_text[i - 2], _text[i - 1])) ? i - 2 : i - 1;

    private int Next(int i) =>
        i >= _text.Length ? _text.Length : (i + 1 < _text.Length && char.IsSurrogatePair(_text[i], _text[i + 1])) ? i + 2 : i + 1;

    private static int CharClass(char ch) =>
        char.IsWhiteSpace(ch) ? 0 : (char.IsLetterOrDigit(ch) || ch == '_') ? 1 : 2;

    private int WordLeft(int i)
    {
        if (IsPassword) return 0;
        while (i > 0 && CharClass(_text[i - 1]) == 0) i--;
        if (i == 0) return 0;
        int cls = CharClass(_text[i - 1]);
        while (i > 0 && CharClass(_text[i - 1]) == cls) i--;
        return i;
    }

    private int WordRight(int i)
    {
        if (IsPassword) return _text.Length;
        int n = _text.Length;
        while (i < n && CharClass(_text[i]) == 0) i++;
        if (i == n) return n;
        int cls = CharClass(_text[i]);
        while (i < n && CharClass(_text[i]) == cls) i++;
        return i;
    }

    public override void Dispose()
    {
        _blinkTimer?.Dispose();
        _blinkTimer = null;
        base.Dispose();
    }
}
