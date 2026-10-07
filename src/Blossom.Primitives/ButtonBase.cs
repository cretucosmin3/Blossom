using System;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Silk.NET.Input;

namespace Blossom.Primitives;

/// <summary>
/// Headless shell for buttons: handles click events, pointer capture on click/drag,
/// keyboard Space/Enter activation, and interaction states. Does not dictate any visual
/// presentation or drawing instructions.
/// </summary>
public class ButtonBase : Control
{
    public ButtonBase()
    {
        Cursor = StandardCursor.Hand;
        ReceivesKeyboard = true;

        Events.OnMouseDown += OnPointerDown;
        Events.OnMouseMove += OnPointerMove;
        Events.OnMouseUp += OnPointerUp;
        Events.OnKeyDown += OnKeyDown;
        Events.OnKeyUp += OnKeyUp;
    }

    /// <summary>Raised when the button is activated by click or keyboard.</summary>
    public event Action? Clicked;

    /// <summary>Invokes the click handlers as if the button was activated.</summary>
    public void PerformClick()
    {
        if (Enabled && EffectiveVisible)
            Clicked?.Invoke();
    }

    private void OnPointerDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != 0 || !Enabled)
            return;

        CapturePointer();
        SetPressed(true);
        e.Handled = true;
    }

    private void OnPointerMove(object? sender, MouseEventArgs e)
    {
        if (!HasPointerCapture)
            return;

        // If dragged outside bounds, visually unpress; if dragged back in, re-press
        bool inside = e.Relative.X >= 0 && e.Relative.X <= W && e.Relative.Y >= 0 && e.Relative.Y <= H;
        SetPressed(inside);
    }

    private void OnPointerUp(object? sender, MouseEventArgs e)
    {
        if (!HasPointerCapture || e.Button != 0)
            return;

        ReleasePointer();
        bool inside = e.Relative.X >= 0 && e.Relative.X <= W && e.Relative.Y >= 0 && e.Relative.Y <= H;
        SetPressed(false);

        if (inside && Enabled)
            PerformClick();

        e.Handled = true;
    }

    private void OnKeyDown(KeyEvent e)
    {
        if (!Enabled)
            return;

        if (e.Key is Key.Space or Key.Enter)
        {
            SetPressed(true);
            e.Handled = true;
        }
    }

    private void OnKeyUp(KeyEvent e)
    {
        if (!Enabled)
            return;

        if (e.Key is Key.Space or Key.Enter)
        {
            if (IsPressed)
            {
                SetPressed(false);
                PerformClick();
            }
            e.Handled = true;
        }
    }
}
