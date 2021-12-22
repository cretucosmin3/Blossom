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

window.Resize += (newSize) =>
{
	Clean();
	renderTarget = new GRBackendRenderTarget(newSize.X, newSize.Y, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
	surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
	canvas = surface.Canvas;

};

window.Render += d =>
{
	grContext.ResetContext();

	canvas.Clear(SKColors.AliceBlue);

	using var red = new SKPaint();
	red.Color = SKColors.Blue;
	red.StrokeWidth = 5;
	red.IsAntialias = true;
	red.Style = SKPaintStyle.Stroke;
	red.PathEffect = SKPathEffect.CreateDash(new float[] { 15, 8 }, 0);

	using var text = new SKPaint();
	text.Color = SKColors.Red;
	text.Style = SKPaintStyle.Fill;
	text.IsLinearText = true;
	text.IsAntialias = true;
	text.TextSize = 30;
	text.FakeBoldText = true;

	canvas.DrawRoundRect(new SKRect(40, 40, 250, 100), 25, 15, red);
	canvas.DrawText("Some Text!", new SKPoint(60, 80), text);

	canvas.Flush(); grGlInterface.Validate();
};

window.Run();