using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Blossom.Core.Visual;

namespace Blossom.Primitives;

/// <summary>
/// Process-wide current <see cref="Theme"/> and helpers to bind elements to named styles.
/// </summary>
public static class Themes
{
    private static Theme _current = new("default");

    public static Theme Current
    {
        get => _current;
        set
        {
            _current = value ?? throw new ArgumentNullException(nameof(value));
            CurrentChanged?.Invoke(_current);
            ThemeBinding.Refresh();
        }
    }

    public static event Action<Theme>? CurrentChanged;

    /// <summary>
    /// Remember style names on <paramref name="element"/> and apply <see cref="Current"/>.
    /// Switching <see cref="Current"/> re-applies those names.
    /// </summary>
    public static T UseStyle<T>(this T element, params string[] styleNames) where T : VisualElement
    {
        ThemeBinding.Bind(element, styleNames);
        return element;
    }
}

internal static class ThemeBinding
{
    private static readonly ConditionalWeakTable<VisualElement, StyleNames> Map = new();
    private static readonly List<WeakReference<VisualElement>> Live = new();

    public static void Bind(VisualElement element, string[] styleNames)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        var names = styleNames ?? Array.Empty<string>();
        var box = Map.GetValue(element, _ =>
        {
            lock (Live)
                Live.Add(new WeakReference<VisualElement>(element));
            return new StyleNames();
        });
        box.Names = names;
        Themes.Current.Apply(element, names);
    }

    public static void Refresh()
    {
        lock (Live)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (!Live[i].TryGetTarget(out var el) || el.IsDisposed)
                {
                    Live.RemoveAt(i);
                    continue;
                }
                if (Map.TryGetValue(el, out var box) && box.Names.Length > 0)
                    Themes.Current.ApplyAvailable(el, box.Names);
            }
        }
    }

    private sealed class StyleNames
    {
        public string[] Names = Array.Empty<string>();
    }
}
