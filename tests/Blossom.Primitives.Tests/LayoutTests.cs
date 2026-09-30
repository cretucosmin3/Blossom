using Blossom.Core.Visual;
using Blossom.Primitives;
using Xunit;

namespace Blossom.Primitives.Tests;

public class LayoutTests
{
    [Fact]
    public void LayoutLength_ParsesUnits()
    {
        Assert.Equal(LayoutUnit.Auto, LayoutLength.Parse("auto").Unit);
        Assert.Equal(LayoutUnit.Star, LayoutLength.Parse("*").Unit);
        Assert.Equal(2f, LayoutLength.Parse("2*").Value);
        Assert.Equal(120f, LayoutLength.Parse("120").Value);
        Assert.Equal(3, LayoutLength.ParseList("200, *, 120").Count);
    }

    [Fact]
    public void Stack_PlacesAndSizesInColumn()
    {
        var stack = new Stack { Orientation = Orientation.Vertical, Gap = 8 };
        stack.Transform.SetAbsoluteFrame(10, 20, 200, 400);

        var a = Box(200, 40);
        var b = Box(80, 30);
        stack.AddChild(a);
        stack.AddChild(b);
        stack.ForceLayoutSubtree();

        Assert.Equal(10, a.Transform.AbsoluteX);
        Assert.Equal(20, a.Transform.AbsoluteY);
        Assert.Equal(200, a.Transform.Width);
        Assert.Equal(40, a.Transform.Height);
        Assert.Equal(20 + 40 + 8, b.Transform.AbsoluteY);
        Assert.Equal(200, b.Transform.Width);
    }

    [Fact]
    public void Stack_GrowTakesLeftover()
    {
        var stack = new Stack { Orientation = Orientation.Vertical, Gap = 0 };
        stack.Transform.SetAbsoluteFrame(0, 0, 100, 100);
        var a = Box(100, 20);
        var b = Box(100, 20);
        stack.AddChild(a);
        stack.AddChild(b);
        Stack.SetGrow(b, 1f);
        stack.ForceLayoutSubtree();

        Assert.Equal(20, a.Transform.Height);
        Assert.Equal(80, b.Transform.Height);
        Assert.Equal(20, b.Transform.AbsoluteY);
    }

    [Fact]
    public void Grid_FlowAndStarColumns()
    {
        var grid = new Grid { Columns = "100, *", ColumnGap = 10, RowGap = 0 };
        grid.Transform.SetAbsoluteFrame(0, 0, 300, 80);
        var a = Box(10, 20);
        var b = Box(10, 20);
        grid.AddChild(a);
        grid.AddChild(b);
        grid.ForceLayoutSubtree();

        Assert.Equal(0, a.Transform.AbsoluteX);
        Assert.Equal(100, a.Transform.Width);
        Assert.Equal(110, b.Transform.AbsoluteX);
        Assert.Equal(190, b.Transform.Width);
        Assert.Equal(80, a.Transform.Height);
        Assert.Equal(80, b.Transform.Height);
    }

    [Fact]
    public void Grid_SetCellPinsAndFlowsTheRest()
    {
        var grid = new Grid { Columns = "*, *", Rows = "40, 40" };
        grid.Transform.SetAbsoluteFrame(0, 0, 200, 80);
        var pinned = Box(1, 1);
        var flowed = Box(1, 1);
        grid.AddChild(pinned);
        grid.AddChild(flowed);
        Grid.SetCell(pinned, column: 1, row: 0);
        grid.ForceLayoutSubtree();

        Assert.Equal(100, pinned.Transform.AbsoluteX);
        Assert.Equal(0, pinned.Transform.AbsoluteY);
        Assert.Equal(0, flowed.Transform.AbsoluteX);
        Assert.Equal(0, flowed.Transform.AbsoluteY);
    }

    [Fact]
    public void Split_SizesTwoPanes()
    {
        var split = new Split { Ratio = 0.25f, BarSize = 8 };
        split.Transform.SetAbsoluteFrame(0, 0, 400, 100);
        var left = Box(1, 1);
        var right = Box(1, 1);
        split.AddChild(left);
        split.AddChild(right);
        split.ForceLayoutSubtree();

        Assert.Equal(0, left.Transform.AbsoluteX);
        Assert.Equal(98, left.Transform.Width); // 0.25 * (400-8)
        Assert.Equal(98, right.Transform.AbsoluteX - 8);
        Assert.Equal(400 - 8 - 98, right.Transform.Width);
        Assert.Equal(100, left.Transform.Height);
    }

    [Fact]
    public void Split_BarSizeZero_PlacesPanesFlush()
    {
        var split = new Split { Ratio = 0.5f, BarSize = 0 };
        split.Transform.SetAbsoluteFrame(0, 0, 400, 100);
        var left = Box(1, 1);
        var right = Box(1, 1);
        split.AddChild(left);
        split.AddChild(right);
        split.ForceLayoutSubtree();

        Assert.Equal(200, left.Transform.Width);
        Assert.Equal(200, right.Transform.AbsoluteX);
        Assert.Equal(200, right.Transform.Width);
        Assert.False(split.Bar.Visible);
    }

    [Fact]
    public void Split_BarIsStylable_AndResizableCanBeOff()
    {
        var split = new Split { IsResizable = false };
        split.Bar.Style.BackColor = Colours.Indigo[500];
        Assert.False(split.IsResizable);
        Assert.Equal(Colours.Indigo[500], split.Bar.Style.BackColor);
    }

    [Fact]
    public void Layout_ManualChildIsSkipped()
    {
        var stack = new Stack { Orientation = Orientation.Vertical, Gap = 0 };
        stack.Transform.SetAbsoluteFrame(0, 0, 100, 100);
        var a = Box(100, 20);
        var b = Box(100, 20);
        stack.AddChild(a);
        stack.AddChild(b);
        b.Transform.SetAbsoluteFrame(50, 50, 10, 10);
        Layout.SetManual(b, true);
        stack.ForceLayoutSubtree();

        Assert.Equal(0, a.Transform.AbsoluteY);
        Assert.Equal(50, b.Transform.AbsoluteX);
        Assert.Equal(50, b.Transform.AbsoluteY);
        Assert.Equal(10, b.Transform.Width);
    }

    private static VisualElement Box(float w, float h)
    {
        var el = new VisualElement();
        el.Transform.Width = w;
        el.Transform.Height = h;
        return el;
    }
}
