using System;
using System.Threading.Tasks;
using Blossom.Hosting;
using Microsoft.Xna.Framework.Input;

namespace SnakeDemo;

/// <summary>
/// Blossom host services for a MonoGame game loop. Posting and timers go to Blossom's dispatcher
/// (drained by <see cref="UiSession.Tick"/>), and cursor requests from the UI (the hand over a
/// <c>ButtonBase</c>) become MonoGame cursors.
/// </summary>
public sealed class MonoGameUiHost : IUiHost
{
    private readonly SoftwareUiHost _inner = new();
    private UiCursor _cursor = UiCursor.Default;

    public float DpiScale => 1f;
    public nint NativeHandle => 0;

    public bool CheckAccess() => _inner.CheckAccess();

    public void Post(Action action) => _inner.Post(action);
    public Task PostAsync(Action action) => _inner.PostAsync(action);
    public IDisposable PostDelayed(TimeSpan delay, Action action) => _inner.PostDelayed(delay, action);
    public IDisposable PostPeriodic(TimeSpan period, Action action) => _inner.PostPeriodic(period, action);

    public void Wake() { } // The game loop ticks continuously.

    public string? GetClipboardText() => null;
    public void SetClipboardText(string text) { }

    public void SetCursor(UiCursor cursor)
    {
        if (cursor == _cursor)
            return;
        _cursor = cursor;
        Mouse.SetCursor(cursor switch
        {
            UiCursor.IBeam => MouseCursor.IBeam,
            UiCursor.Crosshair => MouseCursor.Crosshair,
            UiCursor.Hand => MouseCursor.Hand,
            UiCursor.HResize => MouseCursor.SizeWE,
            UiCursor.VResize => MouseCursor.SizeNS,
            UiCursor.NwseResize => MouseCursor.SizeNWSE,
            UiCursor.NeswResize => MouseCursor.SizeNESW,
            UiCursor.ResizeAll => MouseCursor.SizeAll,
            UiCursor.NotAllowed => MouseCursor.No,
            UiCursor.Wait => MouseCursor.Wait,
            UiCursor.WaitArrow => MouseCursor.WaitArrow,
            _ => MouseCursor.Arrow,
        });
    }
}
