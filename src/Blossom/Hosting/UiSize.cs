namespace Blossom.Hosting;

/// <summary>Logical layout size in design units (today: Shell.RenderRect as RectangleF). Not System.Drawing.Size.</summary>
public readonly record struct UiSize(float Width, float Height);

/// <summary>Device-pixel framebuffer size. Independent of <see cref="UiSize"/>.</summary>
public readonly record struct FramebufferSize(int Width, int Height);
