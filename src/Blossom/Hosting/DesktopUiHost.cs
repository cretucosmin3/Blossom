using System;
using System.Threading.Tasks;
using Blossom.Platform;
using Silk.NET.GLFW;
using Silk.NET.Input;

namespace Blossom.Hosting;

/// <summary>
/// GLFW desktop host. Wake posts an empty event so <c>glfwWaitEventsTimeout</c> returns.
/// </summary>
public sealed class DesktopUiHost : IUiHost
{
    public bool CheckAccess() => UiDispatcher.CheckAccess();

    public void Post(Action action) => UiDispatcher.Post(action);
    public Task PostAsync(Action action) => UiDispatcher.PostAsync(action);
    public IDisposable PostDelayed(TimeSpan delay, Action action) => UiDispatcher.PostDelayed(delay, action);
    public IDisposable PostPeriodic(TimeSpan period, Action action) => UiDispatcher.PostPeriodic(period, action);

    public void Wake()
    {
        if (!Shell.IsLoaded)
            return;
        try
        {
            GlfwProvider.GLFW.Value.PostEmptyEvent();
        }
        catch { }
    }

    public float DpiScale => DisplayScale.Factor;
    public nint NativeHandle => Shell.NativeHandle;

    public string? GetClipboardText() => Shell.GetClipboardText();
    public void SetClipboardText(string text) => Shell.SetClipboardText(text);
    public void SetCursor(StandardCursor cursor) => Shell.SetCursor(cursor);
}
