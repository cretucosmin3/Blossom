using System;
using System.Threading.Tasks;

namespace Blossom.Hosting;

/// <summary>
/// Default host before <see cref="UiHost.Install"/>. Post still enqueues
/// the process dispatcher; Wake is a no-op.
/// </summary>
public sealed class NullUiHost : IUiHost
{
    public static NullUiHost Instance { get; } = new();

    NullUiHost() { }

    public bool CheckAccess() => false;

    public void Post(Action action) => UiDispatcher.Post(action);
    public Task PostAsync(Action action) => UiDispatcher.PostAsync(action);
    public IDisposable PostDelayed(TimeSpan delay, Action action) => UiDispatcher.PostDelayed(delay, action);
    public IDisposable PostPeriodic(TimeSpan period, Action action) => UiDispatcher.PostPeriodic(period, action);

    public void Wake() { }

    public float DpiScale => 1f;
    public nint NativeHandle => 0;

    public string? GetClipboardText() => null;
    public void SetClipboardText(string text) { }
    public void SetCursor(UiCursor cursor) { }
}
