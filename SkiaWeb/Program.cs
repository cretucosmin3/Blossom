using System;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using SkiaSharp;

var options = WindowOptions.Default;
options.Size = new Vector2D<int>(800, 600);
options.Title = "Silk.NET backed Skia rendering!";
options.PreferredStencilBufferBits = 8;
options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
GlfwWindowing.Use();
var window = Window.Create(options);
window.Initialize();

using var grGlInterface = GRGlInterface.Create();
grGlInterface.Validate();

using var grContext = GRContext.CreateGl(grGlInterface);

var renderTarget = new GRBackendRenderTarget(window.Size.X, window.Size.Y, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
var surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
var canvas = surface.Canvas;

void Clean()
{
	renderTarget.Dispose();
	surface.Dispose();
	canvas.Dispose();
}

window.FramebufferResize += (s) =>
{
	Clean();
	renderTarget = new GRBackendRenderTarget(s.X, s.Y, 1, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
	surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.RgbaF16);
	canvas = surface.Canvas;
};

float advance = 0;
window.Render += d =>
{
	grContext.ResetContext();

	canvas.Clear(SKColors.AliceBlue);

	using var red = new SKPaint();
	red.Color = SKColors.Blue;
	red.StrokeWidth = 4;
	red.IsAntialias = true;
	red.Style = SKPaintStyle.Stroke;
	red.PathEffect = SKPathEffect.CreateDash(new float[] { 15, 8 }, advance);

	using var text = new SKPaint();
	text.Color = SKColors.Red;
	text.Style = SKPaintStyle.Fill;
	text.IsLinearText = true;
	text.IsAntialias = true;
	text.TextSize = 45;
	text.FakeBoldText = true;
	text.StrokeWidth = 1;
	text.Style = SKPaintStyle.Stroke;
	text.PathEffect = SKPathEffect.CreateDash(new float[] { 15, 8 }, advance);

	canvas.DrawRoundRect(new SKRect(40, 40, 270, 120), 25, 15, red);
	canvas.DrawText("Some Text!", new SKPoint(45, 90), text);

	canvas.Flush(); grGlInterface.Validate();
	advance += 1.5f;

	if (advance >= 360)
		advance = 0f;
};

window.Run();