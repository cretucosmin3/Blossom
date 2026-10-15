using System.Numerics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;

namespace Blossom.Hosting;

/// <summary>
/// Host-injected pointer, keyboard, and text. Coordinates are in the session's
/// logical viewport (origin top-left). Desktop Silk and engine mappers both
/// call these methods; the tree never reads native devices.
/// </summary>
public sealed class UiInput
{
    readonly UiSession _session;

    internal UiInput(UiSession session) => _session = session;

    /// <summary>Pointer moved. Capture continues to receive moves outside the viewport.</summary>
    public void PointerMove(float x, float y)
    {
        _session.ThrowIfDisposed();
        var pos = new Vector2(x, y);
        var app = _session.Application;
        app.Events.HandleMouseMove(pos);
        app.ActiveView?.Events.HandleMouseMove(pos);
    }

    /// <summary>Pointer button down. <paramref name="button"/> is 0=left, 1=right, 2=middle.</summary>
    public void PointerDown(float x, float y, int button)
    {
        _session.ThrowIfDisposed();
        var pos = new Vector2(x, y);
        var app = _session.Application;
        app.Events.HandleMouseDown(button, pos);
        app.ActiveView?.Events.HandleMouseDown(button, pos);
    }

    /// <summary>Pointer button up. Capture is released by the view after bubbling.</summary>
    public void PointerUp(float x, float y, int button)
    {
        _session.ThrowIfDisposed();
        var pos = new Vector2(x, y);
        var app = _session.Application;
        app.Events.HandleMouseUp(button, pos);
        app.ActiveView?.Events.HandleMouseUp(button, pos);
    }

    /// <summary>
    /// Pointer left the logical viewport. Clears hover only.
    /// Does not drop capture while button 0 or 1 is down.
    /// </summary>
    public void PointerLeave()
    {
        _session.ThrowIfDisposed();
        var view = _session.Application.ActiveView;
        if (view == null)
            return;

        bool held = view.PointerCaptureElement != null
            && (view.Events.IsMouseButtonDown(0) || view.Events.IsMouseButtonDown(1));
        if (held)
            return;

        view.ReleasePointerCapture();
        view.ClearHover();
    }

    /// <summary>Scroll wheel delta (not cumulative). Matches Silk <c>ScrollWheel</c>.</summary>
    public void Scroll(float dx, float dy)
    {
        _session.ThrowIfDisposed();
        var offset = new Vector2(dx, dy);
        var app = _session.Application;
        app.Events.HandleMouseScroll(offset);
        app.ActiveView?.Events.HandleMouseScroll(offset);
    }

    public void KeyDown(UiKey key, int scanCode = 0, bool isRepeat = false)
    {
        _session.ThrowIfDisposed();

        var app = _session.Application;
        var appEvents = app.Events;
        var view = app.ActiveView;
        var focused = view?.ActiveKeyboardElement;

        if (!isRepeat)
        {
            appEvents.TrackKeyDown(key);
            view?.Events.TrackKeyDown(key);
            for (var el = focused; el != null; el = el.Parent)
                el.Events.TrackKeyDown(key);
        }

        var e = new KeyEvent
        {
            Key = key,
            ScanCode = scanCode,
            IsRepeat = isRepeat,
            Control = appEvents.IsControlDown,
            Alt = appEvents.IsAltDown,
            Shift = appEvents.IsShiftDown,
            Super = appEvents.IsSuperDown
        };

        for (var el = focused; el != null && !e.Handled; el = el.Parent)
            el.Events.RaiseKeyDown(e);

        if (!e.Handled)
            view?.Events.RaiseKeyDown(e);

        if (!e.Handled)
            appEvents.RaiseKeyDown(e);

        if (!e.Handled && !isRepeat)
        {
            for (var el = focused; el != null && !e.Handled; el = el.Parent)
            {
                if (el.Events.TryInvokeHotkeys())
                    e.Handled = true;
            }

            if (!e.Handled && view != null && view.Events.TryInvokeHotkeys())
                e.Handled = true;

            if (!e.Handled && appEvents.TryInvokeHotkeys())
                e.Handled = true;
        }

        if (!e.Handled && key == UiKey.Tab && !e.Control && !e.Alt && !e.Super && view != null)
        {
            if (view.TryHandleDefaultTab(e.Shift))
                e.Handled = true;
        }
    }

    public void KeyUp(UiKey key, int scanCode = 0)
    {
        _session.ThrowIfDisposed();

        var app = _session.Application;
        var appEvents = app.Events;
        var view = app.ActiveView;
        var focused = view?.ActiveKeyboardElement;

        var e = new KeyEvent
        {
            Key = key,
            ScanCode = scanCode,
            IsRepeat = false,
            Control = appEvents.IsControlDown,
            Alt = appEvents.IsAltDown,
            Shift = appEvents.IsShiftDown,
            Super = appEvents.IsSuperDown
        };

        appEvents.TrackKeyUp(key);
        view?.Events.TrackKeyUp(key);
        for (var el = focused; el != null; el = el.Parent)
            el.Events.TrackKeyUp(key);

        for (var el = focused; el != null && !e.Handled; el = el.Parent)
            el.Events.RaiseKeyUp(e);

        if (!e.Handled)
            view?.Events.RaiseKeyUp(e);

        if (!e.Handled)
            appEvents.RaiseKeyUp(e);
    }

    /// <summary>UTF-16 text chunk. Desktop surrogate pairing happens in Shell before this call.</summary>
    public void Text(string utf16)
    {
        _session.ThrowIfDisposed();
        if (string.IsNullOrEmpty(utf16))
            return;

        var e = new TextEvent { Text = utf16 };
        var view = _session.Application.ActiveView;
        var focused = view?.ActiveKeyboardElement;

        for (var el = focused; el != null && !e.Handled; el = el.Parent)
            el.Events.RaiseTextInput(e);

        if (!e.Handled)
            view?.Events.RaiseTextInput(e);

        if (!e.Handled)
            _session.Application.Events.RaiseTextInput(e);
    }
}
