using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;

namespace Blossom.Core.Design;

/// <summary>
/// Manages the embedding, synthetic resizing, and layout lifecycle of <see cref="PluginRoot"/> instances inside host slots.
/// </summary>
public static class PluginEmbed
{
    private static readonly Dictionary<PluginRoot, (VisualElement slotHost, Action<VisualElement, float, float> sizeHandler)> _attachedPlugins = new();

    /// <summary>
    /// Attaches a <see cref="PluginRoot"/> into a destination <paramref name="slotHost"/> element in host design units.
    /// Runs synthetic resize reflowing child anchors to match the slot dimensions.
    /// </summary>
    /// <param name="plugin">The plugin root to embed.</param>
    /// <param name="slotHost">The host element / slot container in host design units.</param>
    public static void Attach(PluginRoot plugin, VisualElement slotHost)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (slotHost == null) throw new ArgumentNullException(nameof(slotHost));

        // Detach from previous host if already attached elsewhere
        if (_attachedPlugins.TryGetValue(plugin, out var existing))
        {
            if (existing.slotHost == slotHost)
            {
                // Already attached to this slotHost, just update layout
                UpdateLayout(plugin, slotHost);
                return;
            }
            Detach(plugin);
        }

        // Ensure design anchors are captured before slot resizing
        plugin.EnsureDesignAnchorsCaptured();

        // Add to slot host tree if not already a child
        if (plugin.Parent != slotHost)
        {
            slotHost.AddChild(plugin);
        }

        // Configure anchor to fill slot host
        plugin.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;

        // Hook slot size changes for synthetic resize update
        Action<VisualElement, float, float> sizeHandler = (host, width, height) =>
        {
            UpdateLayout(plugin, host, width, height);
        };
        slotHost.SizeChanged += sizeHandler;
        _attachedPlugins[plugin] = (slotHost, sizeHandler);

        // Perform initial layout mapping
        float destW = slotHost.Transform.Computed.Width > 0 ? slotHost.Transform.Computed.Width : slotHost.Transform.Width;
        float destH = slotHost.Transform.Computed.Height > 0 ? slotHost.Transform.Computed.Height : slotHost.Transform.Height;

        if (destW <= 0) destW = plugin.Canvas.DesignWidth;
        if (destH <= 0) destH = plugin.Canvas.DesignHeight;

        UpdateLayout(plugin, slotHost, destW, destH);
    }

    /// <summary>
    /// Detaches an embedded plugin from its host slot and cleans up event subscriptions.
    /// </summary>
    /// <param name="plugin">The plugin root to detach.</param>
    public static void Detach(PluginRoot plugin)
    {
        if (plugin == null) return;

        if (_attachedPlugins.TryGetValue(plugin, out var entry))
        {
            entry.slotHost.SizeChanged -= entry.sizeHandler;
            _attachedPlugins.Remove(plugin);

            if (plugin.Parent == entry.slotHost)
            {
                entry.slotHost.RemoveChild(plugin);
            }
        }
        else if (plugin.Parent != null)
        {
            plugin.Parent.RemoveChild(plugin);
        }
    }

    /// <summary>
    /// Re-evaluates embed layout for the given plugin inside its host slot.
    /// </summary>
    /// <param name="plugin">The embedded plugin root.</param>
    /// <param name="slotHost">The host slot visual element.</param>
    /// <param name="width">Optional override width, or 0 to query slotHost dimensions.</param>
    /// <param name="height">Optional override height, or 0 to query slotHost dimensions.</param>
    public static void UpdateLayout(PluginRoot plugin, VisualElement slotHost, float width = 0, float height = 0)
    {
        if (plugin == null || slotHost == null) return;

        float destW = width > 0 ? width : (slotHost.Transform.Computed.Width > 0 ? slotHost.Transform.Computed.Width : slotHost.Transform.Width);
        float destH = height > 0 ? height : (slotHost.Transform.Computed.Height > 0 ? slotHost.Transform.Computed.Height : slotHost.Transform.Height);

        if (destW <= 0 || destH <= 0) return;

        // Reflow plugin into slot dimensions
        plugin.ApplyEmbedFit(destW, destH);
    }

    /// <summary>
    /// Returns true if the specified plugin is currently attached to a host slot via <see cref="PluginEmbed"/>.
    /// </summary>
    public static bool IsAttached(PluginRoot plugin) =>
        plugin != null && _attachedPlugins.ContainsKey(plugin);

    /// <summary>
    /// Returns the host slot element for an attached plugin, or null if not attached.
    /// </summary>
    public static VisualElement? GetSlotHost(PluginRoot plugin)
    {
        if (plugin == null) return null;
        return _attachedPlugins.TryGetValue(plugin, out var entry) ? entry.slotHost : plugin.Parent;
    }
}
