using System;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Pill button whose hover effect is an SKSL shader run over the live frame. It paints the pill and
/// label, then, while the effect is visible, snapshots the surface under its
/// <see cref="EffectOutset"/> and draws that back through <see cref="Effect"/>.
/// <para>
/// A browser cannot do this: <c>backdrop-filter</c> only takes fixed filters, and WebGL cannot read
/// DOM pixels. Here it is one runtime effect fed a snapshot of the surface.
/// </para>
/// <para>
/// Frames are requested only while the effect grows, lives or fades (<see cref="OnUpdate"/>). At
/// rest the button is static and the host goes idle.
/// </para>
/// Every effect declares <c>u_scene</c> (the snapshot, device px), <c>u_scale</c> (device px per
/// UI px), <c>u_p</c> (hover progress), <c>u_time</c> (seconds since the effect started) and
/// <c>u_pulse</c> (seconds since the last click, -1 when none).
/// </summary>
public abstract class ShaderButton : ButtonBase
{
    private readonly string _label;
    private SKColor _accent = new(0x8B, 0x5C, 0xF6);

    protected ShaderButton(string label)
    {
        _label = label;
        Transform.Width = 240;
        Transform.Height = 64;
        Clicked += () => Pulse = 0f;
    }

    /// <summary>Theme accent. Subclasses decide where it shows.</summary>
    public SKColor Accent
    {
        get => _accent;
        set => SetAndPaint(ref _accent, value);
    }

    /// <summary>Eased hover progress, 0 at rest and 1 fully on.</summary>
    protected float Progress { get; private set; }

    /// <summary>Seconds since the effect started from rest.</summary>
    protected float Time { get; private set; }

    /// <summary>Seconds since the last click, or -1 when no click effect is playing.</summary>
    protected float Pulse { get; private set; } = -1f;

    /// <summary>How far the effect reaches past the box on each side (left, top, right, bottom), UI px.</summary>
    protected abstract SKRect EffectOutset { get; }

    protected abstract SKRuntimeEffect Effect { get; }

    /// <summary>Colour of the rest glow and border sheen.</summary>
    protected virtual SKColor GlowColour => _accent;

    protected virtual float GrowRate => 2.4f;
    protected virtual float FadeRate => 5f;
    protected virtual float PulseLength => 2.5f;

    /// <summary>Draws the small leading glyph centred on (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    protected abstract void PaintGlyph(SKCanvas c, float cx, float cy, float radius);

    /// <summary>Sets the effect's own uniforms. <paramref name="m"/> maps button space to device px.</summary>
    protected abstract void SetUniforms(SKRuntimeEffectUniforms uniforms, SKMatrix m);

    /// <summary>The hover effect is starting from rest.</summary>
    protected virtual void OnEffectStarted() { }

    /// <summary>Per-frame hook while the effect is running.</summary>
    protected virtual void OnEffectUpdate(float dt) { }

    protected static SKRuntimeEffect Compile(string source, string name) =>
        SKRuntimeEffect.CreateShader(source, out var errors)
        ?? throw new InvalidOperationException($"{name} shader failed to compile: {errors}");

    // Render bounds include the effect's reach, so dirty rects cover everything the shader rewrites.
    protected override SKRect GetLocalContentBounds()
    {
        var o = EffectOutset;
        return new SKRect(-o.Left, -o.Top, Transform.Width + o.Right, Transform.Height + o.Bottom);
    }

    protected override void OnHoverChanged()
    {
        if (IsHovered && Progress <= 0f)
        {
            Time = 0f;
            OnEffectStarted();
        }
    }

    protected override void OnUpdate(float dt)
    {
        float target = IsHovered ? 1f : 0f;
        if (Progress == target && Progress == 0f && Pulse < 0f)
            return; // Nothing to animate: no frames requested.

        float p = Progress + (target - Progress) * (1f - MathF.Exp(-dt * (IsHovered ? GrowRate : FadeRate)));
        Progress = MathF.Abs(target - p) < 0.002f ? target : p;

        Time += dt;
        if (Pulse >= 0f)
            Pulse = Pulse + dt > PulseLength ? -1f : Pulse + dt;

        OnEffectUpdate(dt);
        InvalidatePaint();
    }

    protected override void Paint(SKCanvas c)
    {
        PaintBody(c);
        if (Progress > 0f || Pulse >= 0f)
            PaintEffect(c);
    }

    private void PaintBody(SKCanvas c)
    {
        var rect = new SKRect(0, 0, W, H);
        float radius = H / 2f;
        var glowColour = GlowColour;

        using (var glow = new SKPaint { IsAntialias = true, Color = glowColour.WithAlpha(70), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14f) })
            c.DrawRoundRect(new SKRect(10, 10, W - 10, H + 4), radius, radius, glow);

        using (var fill = new SKPaint { IsAntialias = true })
        {
            var top = IsPressed ? new SKColor(0x12, 0x10, 0x1A) : new SKColor(0x1D, 0x1A, 0x28);
            fill.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, H),
                new[] { top, new SKColor(0x07, 0x07, 0x0B) },
                SKShaderTileMode.Clamp);
            c.DrawRoundRect(rect, radius, radius, fill);
        }

        using (var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f })
        {
            stroke.Shader = SKShader.CreateSweepGradient(new SKPoint(W / 2f, H / 2f),
                new[] { glowColour.WithAlpha(220), SKColors.White.WithAlpha(60), glowColour.WithAlpha(90), SKColors.White.WithAlpha(140), glowColour.WithAlpha(220) });
            var edge = rect;
            edge.Inflate(-0.5f, -0.5f);
            c.DrawRoundRect(edge, radius, radius, stroke);
        }

        // Glyph + label, centred as a group.
        var font = Ui.Font(15f, 600);
        float textW = font.MeasureText(_label);
        const float glyph = 14f, gap = 10f;
        float x = (W - glyph - gap - textW) / 2f;
        float cy = H / 2f;

        PaintGlyph(c, x + glyph / 2f, cy, glyph / 2f);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(235) };
        var m = font.Metrics;
        c.DrawText(_label, x + glyph + gap, cy - (m.Ascent + m.Descent) / 2f, SKTextAlign.Left, font, paint);
    }

    private void PaintEffect(SKCanvas c)
    {
        var surface = c.Surface;
        var m = c.TotalMatrix;
        // Device-space effect: needs the frame surface and a plain scale + translate canvas.
        if (surface == null || m.SkewX != 0 || m.SkewY != 0 || m.Persp0 != 0 || m.Persp1 != 0 || m.ScaleX <= 0)
            return;

        var device = m.MapRect(GetLocalContentBounds());
        // Only the dirty region is being repainted; outside it the surface still holds last frame's output.
        var area = SKRectI.Intersect(SKRectI.Round(device), c.DeviceClipBounds);
        if (area.IsEmpty)
            return;

        // Everything under the effect has already been painted this frame (painter's order), including
        // the pill and label above. The shader reads that and writes its version back.
        using var scene = surface.Snapshot(area);
        if (scene == null)
            return;

        var effect = Effect;
        using var sceneShader = scene.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
            new SKSamplingOptions(SKFilterMode.Linear), SKMatrix.CreateTranslation(area.Left, area.Top));

        var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            ["u_scale"] = m.ScaleX,
            ["u_p"] = Progress,
            ["u_time"] = Time,
            ["u_pulse"] = Pulse,
        };
        SetUniforms(uniforms, m);
        var children = new SKRuntimeEffectChildren(effect) { ["u_scene"] = sceneShader };

        using var shader = effect.ToShader(uniforms, children);
        using var paint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.Src };
        c.Save();
        c.ResetMatrix();
        c.DrawRect(area, paint);
        c.Restore();
    }
}
