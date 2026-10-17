using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;
using SkiaSharp;

namespace Blossom.Testing.Pages;

/// <summary>
/// Two shader buttons on a graph-paper field. Hovering one opens a black hole that bends the live UI
/// around it (<see cref="EventHorizonButton"/>); the other catches fire (<see cref="FireButton"/>).
/// The grid makes the distortion readable.
/// </summary>
public sealed class HorizonPage : Grid
{
    public HorizonPage()
    {
        Name = "HorizonPage";
        Columns = "*";
        Rows = "*";
        Padding = new Thickness(28, 8, 28, 28);
        AddChild(new Spacetime());
    }

    /// <summary>Clipped grid that centres the buttons side by side.</summary>
    private sealed class Spacetime : Control
    {
        private const float Cell = 24f;
        private const float Gap = 96f;
        private readonly EventHorizonButton _horizon;
        private readonly FireButton _fire;
        private SKColor _ink;

        public Spacetime()
        {
            Name = "Spacetime";
            OverflowX = Core.Visual.Enums.OverflowMode.Clip;
            OverflowY = Core.Visual.Enums.OverflowMode.Clip;
            this.UseStyle("stage");

            _horizon = new EventHorizonButton("Event Horizon");
            _horizon.Clicked += () => AppState.Notify("Spaghettified", "That click crossed the event horizon. It is not coming back.", Tone.Primary);
            _fire = new FireButton("Ignite");
            _fire.Clicked += () => AppState.Notify("Flare", "Careful, that one is hot.", Tone.Warning);

            // Painter's order: the fire paints first, so the black hole's lens bends the flames too.
            AddChild(_fire);
            AddChild(_horizon);
            Layout.SetManual(_fire, true);
            Layout.SetManual(_horizon, true);

            this.Bind(s =>
            {
                var theme = Look.Current.Value;
                s._ink = theme.Colour("text-subtle");
                s._horizon.Accent = theme.Colour("primary");
                s.InvalidatePaint();
            });
        }

        protected override void LayoutChildren()
        {
            var h = _horizon.Transform;
            var f = _fire.Transform;
            float x = MathF.Round(Transform.AbsoluteX + (Transform.Width - h.Width - Gap - f.Width) / 2f);
            float cy = Transform.AbsoluteY + Transform.Height / 2f;
            h.SetAbsoluteFrame(x, MathF.Round(cy - h.Height / 2f), h.Width, h.Height);
            f.SetAbsoluteFrame(x + h.Width + Gap, MathF.Round(cy - f.Height / 2f), f.Width, f.Height);
        }

        protected override void Paint(SKCanvas c)
        {
            // Lines are anchored on the centre so the button sits on an intersection.
            float cx = W / 2f, cy = H / 2f;
            float x0 = cx - MathF.Ceiling(cx / Cell) * Cell;
            float y0 = cy - MathF.Ceiling(cy / Cell) * Cell;

            using var paint = new SKPaint { IsAntialias = false, StrokeWidth = 1f };
            for (float x = x0; x <= W; x += Cell)
            {
                paint.Color = _ink.WithAlpha(Major(x - cx) ? (byte)70 : (byte)30);
                c.DrawLine(MathF.Round(x) + 0.5f, 0, MathF.Round(x) + 0.5f, H, paint);
            }
            for (float y = y0; y <= H; y += Cell)
            {
                paint.Color = _ink.WithAlpha(Major(y - cy) ? (byte)70 : (byte)30);
                c.DrawLine(0, MathF.Round(y) + 0.5f, W, MathF.Round(y) + 0.5f, paint);
            }
        }

        private static bool Major(float offset) => MathF.Abs(MathF.IEEERemainder(offset, Cell * 4f)) < 0.5f;
    }
}
