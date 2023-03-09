using System.Collections.Generic;
using System;
using Silk.NET.Windowing;
using SkiaSharp;
using Blossom.Core.Visual;

namespace Blossom.Core.Render;

internal class RenderCycle
{
    public RenderLayers RenderLayers;

    private readonly List<VisualElement> ElementsToRender = new();
    private readonly Dictionary<int, List<VisualElement>> AffectedLayers = new();

    public void AddToCycle(VisualElement element)
    {
        ElementsToRender.Add(element);

        if (!AffectedLayers.ContainsKey(element.LayerPosition))
            AffectedLayers.Add(element.LayerPosition, new());

        AffectedLayers[element.LayerPosition].Add(element);
    }

    public void ResetCycle()
    {
        ElementsToRender.Clear();
    }

    public void RenderCycleImage()
    {
        // Use AffectedLayers to create an image of all layers combined
    }
}