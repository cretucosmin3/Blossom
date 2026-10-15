using System;
using Blossom.Core.Input;
using Blossom.Hosting;
using System.Collections.Generic;

namespace Blossom.Core;

public abstract class Application : IDisposable
{
    /// <summary>Initial window size, min size, resizable border, and center-on-load. Read by <see cref="Shell"/> at create time.</summary>
    public WindowOptions Window { get; } = new();

    /// <summary>
    /// When true, F12 toggles Blossom’s stats overlay and is consumed by the host
    /// before any element, view, or application key bind. Default false.
    /// </summary>
    public bool EnableStatsOverlay { get; set; }

    private string _title = "";
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            //! #render title only
        }
    }

    public UiSession? Session { get; internal set; }

    /// <summary>
    /// UI time in seconds, advanced only by <see cref="UiSession.Tick"/>. Element animations and the
    /// <c>dt</c> passed to <c>OnUpdate</c> are measured on this clock, so they follow the host's game time
    /// (pausing, slow motion) and never depend on how often the host ticks or paints.
    /// </summary>
    public double Time { get; private set; }

    internal void AdvanceTime(float seconds) => Time += seconds;

    private readonly Dictionary<string, View> Views = new();
    private string _ActiveView = "";
    public View ActiveView
    {
        get
        {
            if (Views.TryGetValue(_ActiveView, out var view))
                return view;

            return null;
        }
    }

    public void SetActiveView(string name)
    {
        if (!Views.ContainsKey(name))
            Log.Error($"View {name} does not exist");

        ActiveView?.OnDeactivated();

        _ActiveView = name;

        if (Session?.IsLoaded == true && ActiveView != null && !ActiveView.IsLoaded)
        {
            ActiveView.Init();
            ActiveView.IsLoaded = true;
        }

        ActiveView.OnActivated();
        ActiveView.ForceLayoutEvaluation();
    }

    public void SetActiveView(View view) => SetActiveView(view.Name);

    public readonly EventMap Events = new();

    public void AddView(View view)
    {
        if (!Views.ContainsKey(view.Name))
        {
            Views.Add(view.Name, view);
            view.Application = this;
        }
        else
        {
            Log.Error($"View with name {view.Name} already exists!");
        }
    }

    public void RemoveView(View view)
    {
        if (Views.ContainsKey(view.Name))
        {
            Views.Remove(view.Name);
            view.Dispose();
        }
        else
        {
            Log.Error($"View with name {view.Name} does not exist!");
        }
    }

    internal void Render() => ActiveView?.Render();

    public void Dispose()
    {
        foreach (var view in Views.Values)
        {
            view.Dispose();
        }

        Views.Clear();
        Events.Dispose();
        Session = null;
    }
}