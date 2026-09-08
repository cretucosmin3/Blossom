using System;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;

namespace Blossom.Core.Design;

/// <summary>
/// Root visual element for an embeddable component or plugin declaring its own independent <see cref="DesignCanvas"/>.
/// <para>
/// Authors construct plugins in plugin design units against their declared design canvas. When embedded into a host slot,
/// <see cref="PluginEmbed"/> executes a synthetic resize reflowing the plugin's internal elements and anchors
/// to match the destination slot dimensions.
/// </para>
/// </summary>
public class PluginRoot : VisualElement
{
    private bool _designAnchorsCaptured;
    private DesignCanvas _canvas = null!;

    /// <summary>
    /// Unique identifier for this plugin (e.g. "com.blossom.board-stats").
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name of this plugin.
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// Version of this plugin.
    /// </summary>
    public Version Version { get; set; } = new(1, 0, 0);

    /// <summary>
    /// The declared design canvas defining authoring dimensions (<see cref="Ru"/>) for this plugin.
    /// </summary>
    public DesignCanvas Canvas
    {
        get => _canvas;
        set
        {
            if (_canvas != value)
            {
                if (_canvas != null)
                {
                    _canvas.Changed -= OnCanvasChanged;
                }
                _canvas = value ?? new DesignCanvas();
                _canvas.Changed += OnCanvasChanged;
                OnCanvasChanged();
            }
        }
    }

    /// <summary>
    /// Authoring width in design units (<see cref="Ru"/>). Alias for <c>Canvas.DesignWidth</c>.
    /// </summary>
    public float DesignWidth
    {
        get => Canvas.DesignWidth;
        set
        {
            Canvas.DesignWidth = value;
            _designAnchorsCaptured = false;
        }
    }

    /// <summary>
    /// Authoring height in design units (<see cref="Ru"/>). Alias for <c>Canvas.DesignHeight</c>.
    /// </summary>
    public float DesignHeight
    {
        get => Canvas.DesignHeight;
        set
        {
            Canvas.DesignHeight = value;
            _designAnchorsCaptured = false;
        }
    }

    /// <summary>
    /// Optional minimum slot width in host design units.
    /// </summary>
    public float? MinSlotWidth { get; set; }

    /// <summary>
    /// Optional minimum slot height in host design units.
    /// </summary>
    public float? MinSlotHeight { get; set; }

    /// <summary>
    /// Initializes a new <see cref="PluginRoot"/> with default 1000×1000 design canvas.
    /// </summary>
    public PluginRoot() : this(DesignCanvas.DefaultDesignWidth, DesignCanvas.DefaultDesignHeight)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="PluginRoot"/> with specified design canvas dimensions.
    /// </summary>
    /// <param name="designWidth">Authoring width in design units.</param>
    /// <param name="designHeight">Authoring height in design units.</param>
    public PluginRoot(float designWidth, float designHeight)
    {
        Canvas = new DesignCanvas(designWidth, designHeight);

        // Initialize root transform to match design canvas
        Transform.Width = Canvas.DesignWidth;
        Transform.Height = Canvas.DesignHeight;
        Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
    }

    private void OnCanvasChanged()
    {
        _designAnchorsCaptured = false;
        var host = PluginEmbed.GetSlotHost(this) ?? Parent;
        if (host != null)
        {
            float destW = host.Transform.Computed.Width > 0 ? host.Transform.Computed.Width : host.Transform.Width;
            float destH = host.Transform.Computed.Height > 0 ? host.Transform.Computed.Height : host.Transform.Height;
            if (destW > 0 && destH > 0)
            {
                ApplyEmbedFit(destW, destH);
                return;
            }
        }

        Transform.Width = Canvas.DesignWidth;
        Transform.Height = Canvas.DesignHeight;
    }

    /// <summary>
    /// Captures and initializes child anchor calculations against the declared design canvas dimensions.
    /// </summary>
    public void CaptureDesignAnchors()
    {
        float curW = Transform.Width;
        float curH = Transform.Height;

        // Temporarily set Transform to design canvas dimensions
        Transform.Width = Canvas.DesignWidth;
        Transform.Height = Canvas.DesignHeight;

        foreach (var child in Children)
        {
            if (child != null)
            {
                child.Transform.SetAnchorValues();
            }
        }

        Transform.Width = curW;
        Transform.Height = curH;
        _designAnchorsCaptured = true;
    }

    /// <summary>
    /// Ensures child anchor baselines have been captured against the design canvas prior to destination slot resize.
    /// </summary>
    public void EnsureDesignAnchorsCaptured()
    {
        if (!_designAnchorsCaptured)
        {
            CaptureDesignAnchors();
        }
    }

    /// <summary>
    /// Reflows this plugin into destination slot dimensions (slot size becomes root layout size; child anchors reflow).
    /// </summary>
    /// <param name="destinationWidth">Destination width in host design units.</param>
    /// <param name="destinationHeight">Destination height in host design units.</param>
    public void ApplyEmbedFit(float destinationWidth, float destinationHeight)
    {
        float destW = Math.Max(1f, destinationWidth);
        float destH = Math.Max(1f, destinationHeight);

        if (MinSlotWidth.HasValue) destW = Math.Max(destW, MinSlotWidth.Value);
        if (MinSlotHeight.HasValue) destH = Math.Max(destH, MinSlotHeight.Value);

        // Ensure design anchors are captured before applying reflow
        EnsureDesignAnchorsCaptured();

        // Product model: components always reflow into the slot (anchors, not scale/letterbox).
        Transform.ScaleX = 1f;
        Transform.ScaleY = 1f;
        Transform.TransformOriginX = 0f;
        Transform.TransformOriginY = 0f;
        Transform.SetLocalFrame(0, 0, destW, destH);

        // Mark descendant transforms dirty so anchor solvers re-evaluate against new dimensions
        MarkSubtreeTransformDirty(this);

        // Re-evaluate layout subtree
        ForceLayoutSubtree();

        // Invalidate paint
        InvalidatePaint();
    }

    /// <summary>
    /// Reflows this plugin into the specified destination rectangle in host units.
    /// </summary>
    public void ApplyEmbedFit(Rect destinationRect)
    {
        ApplyEmbedFit(destinationRect.Width, destinationRect.Height);
    }

    /// <summary>
    /// Applies synthetic resize to the specified destination dimensions (reflowing child anchors).
    /// </summary>
    /// <param name="destinationWidth">Destination width in host design units.</param>
    /// <param name="destinationHeight">Destination height in host design units.</param>
    public void ApplySyntheticResize(float destinationWidth, float destinationHeight)
    {
        ApplyEmbedFit(destinationWidth, destinationHeight);
    }

    /// <summary>
    /// Applies synthetic resize to the specified destination rectangle in host units (reflowing child anchors).
    /// </summary>
    public void ApplySyntheticResize(Rect destinationRect)
    {
        ApplyEmbedFit(destinationRect.Width, destinationRect.Height);
    }

    private static void MarkSubtreeTransformDirty(VisualElement element)
    {
        element.Transform._transformDirty = true;
        foreach (var child in element.Children)
        {
            if (child != null)
            {
                MarkSubtreeTransformDirty(child);
            }
        }
    }

    public override void Dispose()
    {
        if (_canvas != null)
        {
            _canvas.Changed -= OnCanvasChanged;
        }
        PluginEmbed.Detach(this);
        base.Dispose();
    }
}
