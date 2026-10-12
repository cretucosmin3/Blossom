namespace Blossom.Hosting;

/// <summary>
/// Process-lifetime installed host for Post/Wake/clipboard/cursor.
/// Desktop installs once in <c>Shell.Initialize</c>.
/// </summary>
public static class UiHost
{
    static IUiHost s_installed = NullUiHost.Instance;

    public static IUiHost Current => s_installed;

    /// <summary>Process-lifetime host. Pass null to restore <see cref="NullUiHost"/>.</summary>
    public static void Install(IUiHost? host) =>
        s_installed = host ?? NullUiHost.Instance;
}
