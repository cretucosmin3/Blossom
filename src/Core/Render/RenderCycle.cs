using System.Linq;
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

        if (!AffectedLayers.ContainsKey(element.Layer))
            AffectedLayers.Add(element.Layer, new());

        AffectedLayers[element.Layer].Add(element);
    }

    public void AddLayerToCycle(int layerIndex)
    {
        if (!AffectedLayers.ContainsKey(layerIndex))
            AffectedLayers.Add(layerIndex, new());
    }

    public void ResetCycle()
    {
        ElementsToRender.Clear();
        AffectedLayers.Clear();
    }

    public void RenderCycleImage()
    {
        int maxLayer = RenderLayers.Count;
        for (int i = 0; i < maxLayer; i++)
        {
            bool layerIsChanged = AffectedLayers.ContainsKey(i);

            if (layerIsChanged)
            {
                // Clear old layer image
                RenderLayers[i].Canvas.Clear();

                // Re-draw layer image

            }

            // Draw layer image to final image

        }
    }
}