using System;

namespace Blossom.Core.Design;

/// <summary>
/// Escape hatch utilities for translating physical device pixels and window logical pixels into Blossom design units (<see cref="Ru"/>).
/// <para>
/// Blossom layout is strictly authored in design units relative to a <see cref="DesignCanvas"/>.
/// Use this class ONLY for rare pixel-snapped rendering requirements (e.g. 1px device hairlines, crisp pixel separators, or debug overlays).
/// Layout calculations must never depend directly on raw window pixels or <c>Browser.RenderRect</c>.
/// </para>
/// </summary>
public static class Px
{
    /// <summary>
    /// Returns the thickness in design units corresponding to approximately 1 physical device / framebuffer pixel under the current DPI scale.
    /// </summary>
    /// <param name="view">Optional host view (unused under 1:1 window-bound layout).</param>
    /// <returns>The design unit length equal to 1 device pixel (minimum 0.0001f).</returns>
    public static float Hairline(View? view = null)
    {
        float winW = Browser.RenderRect.Width;
        if (winW <= 0) return 1f;

        float dpiScale = (Renderer.FramebufferWidth > 0 && winW > 0)
            ? (float)Renderer.FramebufferWidth / winW
            : 1f;

        return dpiScale > 1e-6f ? 1f / dpiScale : 1f;
    }

    /// <summary>
    /// Returns the thickness in design units corresponding to 1 logical window pixel (1.0 under 1:1 window-bound layout).
    /// </summary>
    /// <param name="view">Optional host view.</param>
    /// <returns>The design unit length equal to 1 window logical pixel.</returns>
    public static float WindowPixel(View? view = null) => 1f;

    /// <summary>
    /// Converts a physical device pixel quantity into design canvas units.
    /// </summary>
    public static float FromPixels(float devicePixels, View? view = null) =>
        devicePixels * Hairline(view);

    /// <summary>
    /// Converts a design unit length into approximate physical device pixels.
    /// </summary>
    public static float ToPixels(float designUnits, View? view = null)
    {
        float hairline = Hairline(view);
        return hairline > 1e-6f ? designUnits / hairline : designUnits;
    }

    /// <summary>
    /// Returns a strongly-typed <see cref="Ru"/> representing 1 physical device pixel hairline.
    /// </summary>
    public static Ru HairlineRu(View? view = null) => new(Hairline(view));

    /// <summary>
    /// Returns a strongly-typed <see cref="Ru"/> representing 1 window logical pixel.
    /// </summary>
    public static Ru WindowPixelRu(View? view = null) => new(WindowPixel(view));
}
