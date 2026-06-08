using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class InputField : VisualElement
{
    private string _value = "";
    private readonly VisualElement _bgBox;
    private readonly VisualElement _textElement;
    private readonly VisualElement _caret;
    private bool _isFocused;
    private string _placeholder = "Type here...";

    public Action<string>? OnValueChanged;
    public Action<string>? OnSubmit;

    [BuilderProperty("Text Value", "InputField")]
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                UpdateText();
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    [BuilderProperty("Placeholder", "InputField")]
    public string Placeholder
    {
        get => _placeholder;
        set
        {
            _placeholder = value;
            UpdateText();
        }
    }

    public InputField(string placeholder = "Type here...", string initialValue = "")
    {
        Name = $"InputField_{Guid.NewGuid().ToString().Substring(0, 4)}";
        _placeholder = placeholder;
        _value = initialValue;
        Focusable = true;

        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        // Background box
        _bgBox = new VisualElement
        {
            Name = $"{Name}_Bg",
            Style = new ElementStyle
            {
                BackColor = new SKColor(16, 20, 30, 200),
                Border = new BorderStyle
                {
                    Width = 1f,
                    Color = new SKColor(71, 85, 105),
                    Roundness = 6f
                }
            },
            Transform = new Transform(0, 0, 200, 32)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };

        // Text element inside
        _textElement = new VisualElement
        {
            Name = $"{Name}_Text",
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = string.IsNullOrEmpty(_value) ? new SKColor(100, 116, 139) : SKColors.White, // Slate-500 for placeholder
                    Size = 13,
                    Weight = 400,
                    Alignment = TextAlign.Left,
                    Padding = 8
                }
            },
            Transform = new Transform(0, 0, 200, 32)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };

        // Caret line
        _caret = new VisualElement
        {
            Name = $"{Name}_Caret",
            Style = new ElementStyle
            {
                BackColor = new SKColor(56, 189, 248) // Cyberpunk Cyan
            },
            Transform = new Transform(8, 7, 2, 18)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };
        _caret.Visible = false;

        AddChild(_bgBox);
        _bgBox.AddChild(_textElement);
        _bgBox.AddChild(_caret);

        UpdateText();

        // Mouse click to focus
        Events.OnMouseDown += (s, e) =>
        {
            GetFocus();
        };

        OnFocused += (s) =>
        {
            _isFocused = true;
            _bgBox.Style.Border.Color = new SKColor(56, 189, 248); // Cyan border
            _bgBox.Style.Shadow = new ShadowStyle
            {
                Color = new SKColor(56, 189, 248, 80),
                SpreadX = 4,
                SpreadY = 4
            };
            _caret.Visible = true;
            UpdateText();
        };

        OnFocusLost += (s) =>
        {
            _isFocused = false;
            _bgBox.Style.Border.Color = new SKColor(71, 85, 105);
            _bgBox.Style.Shadow = null!;
            _caret.Visible = false;
            UpdateText();
        };

        // Key type (for text character input)
        Events.OnKeyType += (ch) =>
        {
            if (!_isFocused) return;
            // Ignore control characters
            if (ch >= 32 && ch != 127)
            {
                Value += ch;
            }
        };

        // Key down (for backspace, enter, etc.)
        Events.OnKeyDown += (k) =>
        {
            if (!_isFocused) return;
            Key key = (Key)k;
            if (key == Key.Backspace)
            {
                if (Value.Length > 0)
                {
                    Value = Value.Substring(0, Value.Length - 1);
                }
            }
            else if (key == Key.Enter)
            {
                OnSubmit?.Invoke(Value);
                if (ParentView != null)
                {
                    ParentView.FocusedElement = null!; // Defocus
                }
            }
        };
    }

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(_value))
        {
            _textElement.Text = _placeholder;
            _textElement.Style.Text.Color = new SKColor(100, 116, 139);
        }
        else
        {
            _textElement.Text = _value;
            _textElement.Style.Text.Color = SKColors.White;
        }

        float textWidth = _textElement.Style.Text.Paint.MeasureText(_textElement.Text);
        _caret.Transform.X = Transform.X + Math.Min(Transform.Width - 10, 8 + textWidth + 2);
    }

    public override void AddedToView()
    {
        base.AddedToView();
        UpdateText();
        Transform.OnChanged += (t) => UpdateText();
    }
}
