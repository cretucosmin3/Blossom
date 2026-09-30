using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Xunit;

namespace Blossom.Primitives.Tests;

public class ThemeTests : IDisposable
{
    private readonly Theme _previous;

    public ThemeTests()
    {
        _previous = Themes.Current;
    }

    public void Dispose()
    {
        Themes.Current = _previous;
    }

    [Fact]
    public void Apply_WritesElementStyleFromTokens()
    {
        var theme = new Theme("test");
        theme.SetColour("surface", Colours.Zinc[800]);
        theme.SetNumber("radius", 10f);
        theme.SetStyle("card", (el, th) =>
        {
            el.Style.BackColor = th.Colour("surface");
            el.Style.Border.Roundness = th.Number("radius");
        });

        var box = new VisualElement();
        theme.Apply(box, "card");

        Assert.Equal(Colours.Zinc[800], box.Style.BackColor);
        Assert.Equal(10f, box.Style.Border.Roundness);
    }

    [Fact]
    public void Apply_StacksNamedStylesInOrder()
    {
        var theme = new Theme("test");
        theme.SetStyle("base", (el, _) => el.Style.BackColor = Colours.Zinc[900]);
        theme.SetStyle("accent", (el, _) => el.Style.BackColor = Colours.Indigo[500]);

        var box = new VisualElement();
        theme.Apply(box, "base", "accent");
        Assert.Equal(Colours.Indigo[500], box.Style.BackColor);
    }

    [Fact]
    public void MissingStyle_Throws()
    {
        var theme = new Theme("test");
        Assert.Throws<KeyNotFoundException>(() => theme.Apply(new VisualElement(), "nope"));
    }

    [Fact]
    public void MissingColour_Throws()
    {
        var theme = new Theme("test");
        Assert.Throws<KeyNotFoundException>(() => theme.Colour("nope"));
    }

    [Fact]
    public void UseStyle_ReappliesWhenCurrentThemeChanges()
    {
        var dark = new Theme("dark");
        dark.SetColour("surface", Colours.Zinc[800]);
        dark.SetStyle("card", (el, th) => el.Style.BackColor = th.Colour("surface"));

        var light = new Theme("light");
        light.SetColour("surface", Colours.Zinc[100]);
        light.SetStyle("card", (el, th) => el.Style.BackColor = th.Colour("surface"));

        Themes.Current = dark;
        var box = new VisualElement();
        box.UseStyle("card");
        Assert.Equal(Colours.Zinc[800], box.Style.BackColor);

        Themes.Current = light;
        Assert.Equal(Colours.Zinc[100], box.Style.BackColor);
    }
}
