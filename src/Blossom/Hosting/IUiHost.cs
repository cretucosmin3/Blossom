using System;
using System.Threading.Tasks;

namespace Blossom.Hosting;

/// <summary>
/// Process-wide window / loop services: marshal, idle wake, clipboard, cursor.
/// Logical size and GPU context are not on the host.
/// </summary>
public interface IUiHost
{
    bool CheckAccess();
    void Post(Action action);
    Task PostAsync(Action action);
    IDisposable PostDelayed(TimeSpan delay, Action action);
    IDisposable PostPeriodic(TimeSpan period, Action action);

    /// <summary>Wake an idle wait. No-op on a continuously ticking game loop.</summary>
    void Wake();

    float DpiScale { get; }

    string? GetClipboardText();
    void SetClipboardText(string text);
    void SetCursor(UiCursor cursor);

    /// <summary>Optional. Desktop uses this for stats overlay / native handle. Games return 0.</summary>
    nint NativeHandle { get; }
}
