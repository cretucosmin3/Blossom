using System.Collections.Generic;
using System;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Blossom.Core.Render;

internal class RenderLayers
{
    private readonly Dictionary<int, SKSurface> SurfaceLayers = new();

    public int Count { get => SurfaceLayers.Count; }

    public bool HasLayer(int index) => SurfaceLayers.ContainsKey(index);

    public void CreateLayer(int layer)
    {
        int width = Browser.window.Size.X;
        int height = Browser.window.Size.X;

        if (SurfaceLayers.ContainsKey(layer))
            DestroyLayer(layer);

        var imageInfo = new SKImageInfo(
            width,
            height,
            SKColorType.Rgb565,
            SKAlphaType.Opaque
        );

        SurfaceLayers.Add(layer, SKSurface.Create(imageInfo));
    }

    public void ClearLayer(int layer)
    {
        SurfaceLayers[layer].Canvas.Clear();
    }

    public void DestroyLayer(int layer)
    {
        if (SurfaceLayers.ContainsKey(layer))
        {
            SurfaceLayers[layer].Dispose();
            SurfaceLayers.Remove(layer);
        }
    }

    public SKSurface this[int index]
    {
        get => SurfaceLayers[index];
    }
}