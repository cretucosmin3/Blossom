using System;
using Blossom.Primitives;
using SkiaSharp;
using Xunit;

namespace Blossom.Primitives.Tests;

public class ColourTests
{
    [Fact]
    public void Zinc800_MatchesTailwindHex()
    {
        var c = Colours.Zinc[800];
        Assert.Equal(0x27, c.Red);
        Assert.Equal(0x27, c.Green);
        Assert.Equal(0x2A, c.Blue);
        Assert.Equal(255, c.Alpha);
    }

    [Fact]
    public void Indigo500_MatchesTailwindHex()
    {
        var c = Colours.Indigo[500];
        Assert.Equal(0x63, c.Red);
        Assert.Equal(0x66, c.Green);
        Assert.Equal(0xF1, c.Blue);
    }

    [Fact]
    public void Neutral900_MatchesTailwindHex()
    {
        var c = Colours.Neutral[900];
        Assert.Equal(0x17, c.Red);
        Assert.Equal(0x17, c.Green);
        Assert.Equal(0x17, c.Blue);
    }

    [Fact]
    public void Grey_IsGrayAlias()
    {
        Assert.Same(Colours.Gray, Colours.Grey);
        Assert.Equal(Colours.Gray[700], Colours.Grey[700]);
    }

    [Fact]
    public void WhiteBlackTransparent()
    {
        Assert.Equal(new SKColor(255, 255, 255), Colours.White);
        Assert.Equal(new SKColor(0, 0, 0), Colours.Black);
        Assert.Equal(0, Colours.Transparent.Alpha);
    }

    [Fact]
    public void WithAlpha_KeepsRgb()
    {
        var faded = Colours.Indigo[500].WithAlpha(80);
        Assert.Equal(Colours.Indigo[500].Red, faded.Red);
        Assert.Equal(80, faded.Alpha);
    }

    [Fact]
    public void UnknownShade_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _ = Colours.Zinc[750]);
        Assert.Equal("shade", ex.ParamName);
    }

    [Fact]
    public void TryGet_RejectsUnknownShade()
    {
        Assert.False(Colours.Slate.TryGet(75, out _));
        Assert.True(Colours.Slate.TryGet(50, out var c));
        Assert.Equal(0xF8, c.Red);
        Assert.Equal(0xFA, c.Green);
        Assert.Equal(0xFC, c.Blue);
    }

    [Fact]
    public void EveryScaleHasEveryShade()
    {
        ColourScale[] scales =
        {
            Colours.Slate, Colours.Gray, Colours.Zinc, Colours.Neutral, Colours.Stone,
            Colours.Red, Colours.Orange, Colours.Amber, Colours.Yellow, Colours.Lime,
            Colours.Green, Colours.Emerald, Colours.Teal, Colours.Cyan, Colours.Sky,
            Colours.Blue, Colours.Indigo, Colours.Violet, Colours.Purple, Colours.Fuchsia,
            Colours.Pink, Colours.Rose
        };

        foreach (var scale in scales)
        {
            foreach (int shade in ColourScale.Shades)
            {
                Assert.True(scale.TryGet(shade, out var colour));
                Assert.Equal(255, colour.Alpha);
            }
        }
    }
}
