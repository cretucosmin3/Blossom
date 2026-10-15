using System;
using System.Threading.Tasks;

namespace Blossom.Hosting;

/// <summary>
/// CPU-only host for tests and software-raster CI. Wake is a no-op; Post still
/// enqueues the process dispatcher for <see cref="UiSession.Tick"/>.
/// </summary>
public sealed class SoftwareUiHost : IUiHost
{
    int _uiThreadId;

    public SoftwareUiHost() => _uiThreadId = Environment.CurrentManagedThreadId;

    public float DpiScale { get; set; } = 1f;
    public nint NativeHandle => 0;

    public bool CheckAccess()
    {
        if (_uiThreadId == 0)
            _uiThreadId = Environment.CurrentManagedThreadId;
        return Environment.CurrentManagedThreadId == _uiThreadId;
    }

    public void Post(Action action) => UiDispatcher.Post(action);
    public Task PostAsync(Action action) => UiDispatcher.PostAsync(action);
    public IDisposable PostDelayed(TimeSpan delay, Action action) => UiDispatcher.PostDelayed(delay, action);
    public IDisposable PostPeriodic(TimeSpan period, Action action) => UiDispatcher.PostPeriodic(period, action);

    public void Wake() { }

    public string? GetClipboardText() => null;
    public void SetClipboardText(string text) { }
    public void SetCursor(UiCursor cursor) { }
}
