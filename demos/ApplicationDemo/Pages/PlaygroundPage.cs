using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;
using static Blossom.Reactive.ReactiveEngine;
using Blossom.Desktop;

namespace Blossom.Testing.Pages;

/// <summary>
/// Core rendering features driven by signals: SKSL background shaders, backdrop blur, and a
/// 3D-transformed card whose button still receives clicks (inverse hit-testing through the 4×4).
/// </summary>
public sealed class PlaygroundPage : Grid
{
    private static readonly (string Name, BackgroundShaderType Type)[] Shaders =
    {
        ("Plasma", BackgroundShaderType.LiquidPlasma),
        ("Grid", BackgroundShaderType.SynthwaveGrid),
        ("Lattice", BackgroundShaderType.HolographicLattice),
        ("Paint", BackgroundShaderType.LiquidPaint),
    };

    public PlaygroundPage()
    {
        Name = "PlaygroundPage";
        Columns = "320, *";
        Rows = "*";
        ColumnGap = 20;
        Padding = new Thickness(28, 8, 28, 28);

        var shader = CreateSignal(0);
        var pitch = CreateSignal(12f);
        var yaw = CreateSignal(-18f);
        var scale = CreateSignal(100f);
        var radius = CreateSignal(20f);
        var blur = CreateSignal(14f);
        var orbit = CreateSignal(false);
        var animate = CreateSignal(true);
        var hits = CreateSignal(0);

        // Controls ---------------------------------------------------------
        var panel = new ScrollView(0);
        var card = new Card("Scene", "Every control writes a signal; the stage binds to them.");
        card.AddChild(Ui.Column(8, Ui.Text("Background shader", "text.label"),
            new Segmented(Array.ConvertAll(Shaders, s => s.Name)).BindSelected(shader)));
        card.AddChild(SliderRow("Tilt X", pitch, -35, 35, "°"));
        card.AddChild(SliderRow("Tilt Y", yaw, -45, 45, "°"));
        card.AddChild(SliderRow("Scale", scale, 70, 120, "%"));
        card.AddChild(SliderRow("Corner radius", radius, 0, 40, "px"));
        card.AddChild(SliderRow("Glass blur", blur, 0, 30, ""));
        card.AddChild(Ui.Divider());
        card.AddChild(new Switch("Auto-orbit").BindChecked(orbit));
        card.AddChild(new Switch("Animate shader").BindChecked(animate));

        var reset = new Button("Reset scene", "rotate", ButtonVariant.Outline);
        reset.Clicked += () => Batch(() =>
        {
            pitch.Value = 12f; yaw.Value = -18f; scale.Value = 100f;
            radius.Value = 20f; blur.Value = 14f; orbit.Value = false;
        });
        card.AddChild(Ui.Row(10, reset, Ui.Text(() => $"3D clicks: {hits.Value}", "text.muted")));
        panel.Content.AddChild(card);
        AddChild(panel);

        // Stage ------------------------------------------------------------
        var stage = new Stage();
        stage.Bind(s =>
        {
            s.Style.BackgroundShader = Shaders[shader.Value].Type;
            s.Style.BackgroundShaderColor = Look.Current.Value.Colour("primary");
            s.Style.ShaderRenderMode = animate.Value ? EffectRenderMode.Continuous : EffectRenderMode.OnDemand;
        });

        var glass = stage.Glass;
        glass.Bind(g =>
        {
            var t = g.Transform;
            t.RotationX = pitch.Value;
            t.RotationY = yaw.Value;
            t.ScaleX = t.ScaleY = scale.Value / 100f;
            g.Style.Border.Roundness = radius.Value;
            g.Style.BackdropBlur = blur.Value;
        });

        var press = new Button("Click me in 3D", "zap");
        press.Clicked += () => hits.Value++;
        var title = Ui.Text("Glass card", "text.glass-title");
        var copy = Ui.Paragraph("Rotated with a perspective 4×4. The button is hit-tested by unprojecting the pointer through the same matrix.", "text.glass");
        var count = new Badge("0", Tone.Primary);
        count.Bind(b => b.Text = hits.Value.ToString());
        glass.AddChild(Ui.Row(8, new Icon("box", 20f).Tinted("text"), title.Grow(), count));
        glass.AddChild(copy);
        glass.AddChild(Ui.Row(8, press, Ui.Spacer()));
        AddChild(stage);

        // Auto-orbit: an effect owns a periodic timer while the switch is on.
        IDisposable? timer = null;
        var start = DateTime.Now;
        stage.Bind(_ =>
        {
            timer?.Dispose();
            timer = null;
            if (!orbit.Value) return;
            start = DateTime.Now;
            float baseYaw = yaw.Peek();
            timer = Shell.PostPeriodic(TimeSpan.FromMilliseconds(16), () =>
            {
                double s = (DateTime.Now - start).TotalSeconds;
                Batch(() =>
                {
                    yaw.Value = (float)Math.Round(baseYaw * Math.Cos(s * 0.9) + 30 * Math.Sin(s * 0.9), 1);
                    pitch.Value = (float)Math.Round(12 * Math.Cos(s * 1.3), 1);
                });
            });
        });
        stage.Disposed += _ => timer?.Dispose();
    }

    private static Stack SliderRow(string label, Signal<float> value, float min, float max, string unit)
    {
        return Ui.Column(6,
            Ui.Row(8, Ui.Text(label, "text.label").Grow(), Ui.Text(() => $"{value.Value:0}{unit}", "text.accent")),
            new Slider(min, max, value.Peek()).BindValue(value));
    }

    /// <summary>Shader surface that centres a fixed-size glass card.</summary>
    private sealed class Stage : VisualElement
    {
        public Stage()
        {
            Name = "Stage";
            OverflowX = Core.Visual.Enums.OverflowMode.Clip;
            OverflowY = Core.Visual.Enums.OverflowMode.Clip;
            this.UseStyle("stage");

            Glass = new GlassCard();
            AddChild(Glass);
            Layout.SetManual(Glass, true);
        }

        public GlassCard Glass { get; }

        protected override void LayoutChildren()
        {
            const float w = 380f, h = 230f;
            Glass.Transform.SetAbsoluteFrame(
                MathF.Round(Transform.AbsoluteX + (Transform.Width - w) / 2f),
                MathF.Round(Transform.AbsoluteY + (Transform.Height - h) / 2f), w, h);
        }
    }

    public sealed class GlassCard : Stack
    {
        public GlassCard()
        {
            Name = "GlassCard";
            Orientation = Orientation.Vertical;
            Gap = 14;
            Padding = new Thickness(24);
            Transform.TransformOriginX = 0.5f;
            Transform.TransformOriginY = 0.5f;
            Transform.Perspective = 900f;
            // Re-sample the blurred backdrop every frame: the shader behind it moves.
            Style.ShaderRenderMode = EffectRenderMode.Continuous;
            this.UseStyle("glass");
        }
    }
}

internal static class IconExtensions
{
    public static Icon Tinted(this Icon icon, string token)
    {
        icon.Bind(i => i.Color = Look.Current.Value.Colour(token));
        return icon;
    }
}
