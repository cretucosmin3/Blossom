using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Hosting;
using SkiaSharp;
using Xunit;

namespace Blossom.Hosting.Tests;

public class BackdropEffectTests
{
    const int W = 120, H = 80;
    static readonly SKRectI Glass = new(30, 20, 90, 60);

    [Theory]
    [InlineData(nameof(SKSLShaderManager.CrtShaderSource))]
    [InlineData(nameof(SKSLShaderManager.GridShaderSource))]
    [InlineData(nameof(SKSLShaderManager.PlasmaShaderSource))]
    [InlineData(nameof(SKSLShaderManager.GlassRefractionShaderSource))]
    [InlineData(nameof(SKSLShaderManager.GlassBorderShaderSource))]
    [InlineData(nameof(SKSLShaderManager.HolographicLatticeShaderSource))]
    [InlineData(nameof(SKSLShaderManager.LiquidPaintShaderSource))]
    [InlineData(nameof(SKSLShaderManager.HalftoneTransitionShaderSource))]
    [InlineData(nameof(SKSLShaderManager.PhotoEditorShaderSource))]
    public void BuiltInShader_Compiles(string name)
    {
        var source = (string)typeof(SKSLShaderManager).GetField(name)!.GetValue(null)!;
        using var effect = SKRuntimeEffect.CreateShader(source, out var errors);
        Assert.True(effect != null, $"{name}: {errors}");
    }

    [Theory]
    [InlineData(EffectRenderMode.Continuous)]
    [InlineData(EffectRenderMode.OnDemand)]
    public void BackdropBlur_MatchesBlurOfTheWholeBackdrop(EffectRenderMode mode)
    {
        // The blur copies only the backdrop under the element plus the kernel's reach. Stripes make
        // the result depend on pixels outside the element, so a reach that is too short shows up here.
        const float sigma = 4f;
        using var backdrop = Render(withGlass: false, mode, sigma);
        using var actual = Render(withGlass: true, mode, sigma);

        using var expected = SKSurface.Create(new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var blur = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(sigma, sigma) })
        {
            expected.Canvas.Clear(SKColors.Transparent);
            expected.Canvas.DrawImage(backdrop, 0, 0, blur);
        }
        using var expectedImage = expected.Snapshot();

        using var a = actual.PeekPixels();
        using var e = expectedImage.PeekPixels();
        int worst = 0;
        for (int y = Glass.Top + 2; y < Glass.Bottom - 2; y++)
            for (int x = Glass.Left + 2; x < Glass.Right - 2; x++)
            {
                var pa = a.GetPixelColor(x, y);
                var pe = e.GetPixelColor(x, y);
                worst = Math.Max(worst, Math.Max(Math.Abs(pa.Red - pe.Red), Math.Max(Math.Abs(pa.Green - pe.Green), Math.Abs(pa.Blue - pe.Blue))));
            }

        Assert.InRange(worst, 0, 3);
    }

    static SKImage Render(bool withGlass, EffectRenderMode mode, float sigma)
    {
        using var surface = SKSurface.Create(new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Premul));
        var view = new StripesView(withGlass, mode, sigma);
        using var session = new UiSession(new SoftwareUiHost(), view, new UiSessionOptions
        {
            PaintMode = PaintMode.FullFrame,
            ClearColor = SKColors.Transparent,
        });
        session.SetSize(W, H, W, H);
        session.Init();
        session.Paint(new RenderFrame(surface.Canvas, session.LogicalSize, session.FramebufferSize, surface: surface), force: true);
        return surface.Snapshot();
    }

    sealed class StripesView : View
    {
        readonly bool _withGlass;
        readonly EffectRenderMode _mode;
        readonly float _sigma;

        public StripesView(bool withGlass, EffectRenderMode mode, float sigma) : base("stripes")
        {
            _withGlass = withGlass;
            _mode = mode;
            _sigma = sigma;
        }

        public override void Init()
        {
            var root = new VisualElement { Name = "root" };
            root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
            root.Style.BackColor = SKColors.White;

            for (int x = 0; x < W; x += 8)
                root.AddChild(Box(x, 0, 4, H, (x / 8) % 2 == 0 ? SKColors.Red : SKColors.Blue));

            if (_withGlass)
            {
                var glass = Box(Glass.Left, Glass.Top, Glass.Width, Glass.Height, SKColors.Transparent);
                glass.Style.BackdropBlur = _sigma;
                glass.Style.ShaderRenderMode = _mode;
                root.AddChild(glass);
            }

            AddElement(root);
        }

        static VisualElement Box(float x, float y, float w, float h, SKColor color)
        {
            var box = new VisualElement();
            box.Transform.Anchor = Anchor.Left | Anchor.Top;
            box.Transform.LocalX = x;
            box.Transform.LocalY = y;
            box.Transform.Width = w;
            box.Transform.Height = h;
            box.Style.BackColor = color;
            box.Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
            return box;
        }
    }
}
