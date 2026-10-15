using System;

namespace Blossom.Hosting;

/// <summary>
/// Process-lifetime installed host for Post/Wake/clipboard/cursor.
/// Desktop installs once in <c>Shell.Initialize</c>.
/// Enter/Exit only guard nested Tick/Paint and never clear <see cref="Current"/>.
/// </summary>
public static class UiHost
{
    static IUiHost s_installed = NullUiHost.Instance;
    [ThreadStatic] static bool t_inTickOrPaint;

    public static IUiHost Current => s_installed;

    /// <summary>Process-lifetime host. Pass null to restore <see cref="NullUiHost"/>.</summary>
    public static void Install(IUiHost? host) =>
        s_installed = host ?? NullUiHost.Instance;

    /// <summary>Nested Tick/Paint guard only. Does not change Current.</summary>
    internal static void Enter()
    {
        if (t_inTickOrPaint)
            throw new InvalidOperationException("UiSession.Tick/Paint cannot nest.");
        t_inTickOrPaint = true;
    }

    internal static void Exit()
    {
        t_inTickOrPaint = false;
    }
}
