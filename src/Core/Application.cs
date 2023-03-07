using System;
using Blossom.Core.Input;
using System.Collections.Generic;

namespace Blossom.Core;

public abstract class Application : IDisposable
{
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

    private Dictionary<string, View> Views = new();
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

        _ActiveView = name;

        if (Browser.IsLoaded && !ActiveView.IsLoaded)
        {
            ActiveView.Main();
            ActiveView.IsLoaded = true;
        }

        ActiveView.RenderRequired = true;
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
    }
}