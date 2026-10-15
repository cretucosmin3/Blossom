using System;
using System.IO;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Stroke icon from <c>assets/icons/{name}.svg</c>, recoloured with a tint so one SVG
/// follows any theme token. Click-through by default: the parent control owns input.
/// </summary>
public sealed class Icon : VisualElement
{
    private string _name = "";
    private readonly float _size;

    public Icon(string name, float size = 16f)
    {
        _size = size;
        IsClickthrough = true;
        BackgroundImageScale = ImageScaleMode.Contain;
        Transform.Width = size;
        Transform.Height = size;
        IconName = name;
    }

    public string IconName
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value ?? "";
            if (_name.Length > 0)
                LoadSvgFromFile(Path.Combine(AppContext.BaseDirectory, "assets", "icons", _name + ".svg"));
        }
    }

    public SKColor Color
    {
        get => BackgroundImageTintColor;
        set => BackgroundImageTintColor = value;
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight) => new(_size, _size);
}
