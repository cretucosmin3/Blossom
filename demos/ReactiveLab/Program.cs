using System;
using Blossom;
using Blossom.Desktop;

namespace ReactiveLab;

internal static class Program
{
    private static void Main()
    {
        Log.Initialize();
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Fatal($"Unhandled exception: {e.ExceptionObject}");

        Shell.MaxFps = 120;
        Shell.ShowDebugOverlay = true;
        Shell.Initialize(new LabApplication());
        Environment.Exit(0);
    }
}

/// <summary>One window, one view. F12 toggles the stats overlay (frame time, element count).</summary>
public sealed class LabApplication : DesktopApplication
{
    public LabApplication()
    {
        Title = "Blossom Reactive Lab";
        EnableStatsOverlay = true;
        Window.Width = 1480;
        Window.Height = 900;
        Window.MinWidth = 1100;
        Window.MinHeight = 640;

        var view = new LabView();
        AddView(view);
        SetActiveView(view);
    }
}
