using System;

namespace Blossom.Primitives;

/// <summary>
/// Headless shell for toggleable controls (checkboxes, toggle switches, toggle buttons, radio buttons).
/// Extends <see cref="ButtonBase"/> with checked state tracking and toggling logic.
/// </summary>
public class ToggleBase : ButtonBase
{
    private bool _isChecked;

    public ToggleBase(bool initialChecked = false)
    {
        _isChecked = initialChecked;
        Clicked += OnToggleClicked;
    }

    /// <summary>Raised when <see cref="IsChecked"/> changes.</summary>
    public event Action<bool>? CheckedChanged;

    /// <summary>True when the toggle is active / checked.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetAndPaint(ref _isChecked, value))
            {
                OnCheckedChanged(value);
                CheckedChanged?.Invoke(value);
            }
        }
    }

    /// <summary>Toggles the checked state.</summary>
    public void Toggle() => IsChecked = !IsChecked;

    protected virtual void OnCheckedChanged(bool isChecked) { }

    private void OnToggleClicked()
    {
        if (Enabled)
            Toggle();
    }
}
