using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class ViewsSidebar : VisualElement
{
    private readonly VisualElement _contentContainer;
    private readonly List<string> _viewNames = new();
    private int _selectedIndex = -1;

    public Action<int>? OnViewSelected;
    public Action? OnAddViewRequested;

    public ViewsSidebar()
    {
        Name = "ViewsSidebar";

        Style = new ElementStyle
        {
            BackColor = new SKColor(16, 20, 30, 240),
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
            Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(120), SpreadX = 8, SpreadY = 0, OffsetX = 2 }
        };

        Transform = new Transform(0, 0, 260, 800)
        {
            Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
            FixedWidth = true,
            FixedHeight = false
        };

        // Brand Label
        var brand = new VisualElement
        {
            Name = "SidebarBrand",
            Text = "⚡ BLOSSOM BUILDER",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 18, Weight = 800, Alignment = TextAlign.Center, Padding = 15 }
            },
            Transform = new Transform(0, 10, 260, 50)
        };
        AddChild(brand);

        // Add View Button
        var addBtn = new Button("+ Add New View", new SKColor(34, 197, 94))
        {
            Transform = new Transform(15, 65, 230, 36)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };
        addBtn.OnClick += () => OnAddViewRequested?.Invoke();
        AddChild(addBtn);

        // Section header
        var header = new VisualElement
        {
            Name = "ViewsHeader",
            Text = "SCREENS / VIEWS",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 10, Weight = 800, Alignment = TextAlign.Left, Padding = 15 }
            },
            Transform = new Transform(0, 115, 260, 25)
        };
        AddChild(header);

        // Content list container
        _contentContainer = new VisualElement
        {
            Name = "ViewsListContainer",
            Style = new ElementStyle { BackColor = SKColors.Transparent },
            Transform = new Transform(0, 140, 260, 660)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
            }
        };
        AddChild(_contentContainer);
    }

    public void UpdateViewsList(List<string> names, int selectedIndex)
    {
        var children = _contentContainer.Children.ToArray();
        foreach (var child in children)
        {
            _contentContainer.RemoveChild(child);
            child.Dispose();
        }

        _viewNames.Clear();
        _viewNames.AddRange(names);
        _selectedIndex = selectedIndex;

        float startY = 5f;
        float itemH = 40f;

        for (int i = 0; i < _viewNames.Count; i++)
        {
            int index = i;
            bool isSelected = (index == _selectedIndex);

            var item = new VisualElement
            {
                Name = $"ViewItem_{index}",
                Text = $"📄  {_viewNames[index]}",
                Style = new ElementStyle
                {
                    BackColor = isSelected ? new SKColor(56, 189, 248, 40) : SKColors.Transparent,
                    Border = new BorderStyle
                    {
                        Width = isSelected ? 1 : 0,
                        Color = isSelected ? new SKColor(56, 189, 248) : SKColors.Transparent,
                        Roundness = 6f
                    },
                    Text = new TextStyle
                    {
                        Color = isSelected ? new SKColor(56, 189, 248) : new SKColor(203, 213, 225),
                        Size = 13,
                        Weight = isSelected ? 700 : 500,
                        Alignment = TextAlign.Left,
                        Padding = 12
                    }
                },
                Transform = new Transform(15, startY, 230, 34)
                {
                    FixedWidth = true,
                    FixedHeight = true
                }
            };

            item.Events.OnMouseEnter += (s) =>
            {
                if (!isSelected)
                {
                    item.Style.BackColor = new SKColor(255, 255, 255, 10);
                }
            };

            item.Events.OnMouseLeave += (s) =>
            {
                if (!isSelected)
                {
                    item.Style.BackColor = SKColors.Transparent;
                }
            };

            item.Events.OnMouseUp += (s, e) =>
            {
                OnViewSelected?.Invoke(index);
            };

            _contentContainer.AddChild(item);
            startY += itemH;
        }
    }
}
