using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.Models;
using Blossom.Testing.Tabs;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Views;

public class StudioView : View
{
    private readonly Signal<StudioTab> _activeTab = CreateSignal(StudioTab.Board);

    public StudioView() : base("Blossom Studio")
    {
        DemoThemes.Ensure();
        BackColor = Themes.Current.Colour("app");
        Themes.CurrentChanged += th =>
        {
            if (th.TryColour("app", out var app))
                BackColor = app;
        };
    }

    public override void Init()
    {
        DemoThemes.Ensure();

        var root = new Stack
        {
            Name = "Studio_Root",
            Orientation = Orientation.Vertical,
            Transform = new Transform(0, 0, Width, Height)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        root.UseStyle("app");

        var header = new Stack
        {
            Name = "Studio_Header",
            Orientation = Orientation.Horizontal,
            Gap = 12,
            Align = LayoutAlign.Center,
            Padding = new Thickness(18, 12, 16, 12)
        };
        header.MinHeight = 56;
        header.Transform.Height = 56;
        header.UseStyle("chrome");

        var title = new VisualElement
        {
            Name = "Studio_Logo",
            Text = "Blossom",
            IsClickthrough = true
        };
        title.MinWidth = 110;
        title.UseStyle("logo");

        var subtitle = new VisualElement
        {
            Name = "Studio_Subtitle",
            Text = "Retained UI  ·  SkiaSharp  ·  Blossom.Reactive",
            IsClickthrough = true,
            Visible = Width >= 980f
        };
        subtitle.UseStyle("muted");

        var themeSwitch = new Stack
        {
            Name = "Studio_ThemeSwitch",
            Orientation = Orientation.Horizontal,
            Gap = 4,
            Align = LayoutAlign.Stretch
        };
        themeSwitch.MinHeight = 28;
        themeSwitch.Transform.Height = 28;

        foreach (var themeName in DemoThemes.Names)
        {
            string name = themeName;
            var btn = new Button(name, enable3DEffect: false);
            btn.Style.Text.Size = 11f;
            btn.MinWidth = 56;
            btn.MinHeight = 26;
            btn.Clicked += () => DemoThemes.Select(name);
            btn.Bind(b =>
            {
                var th = DemoThemes.Current;
                bool on = DemoThemes.Active.Value == name;
                b.NormalColor = on ? th.Colour("accent") : th.Colour("ghost");
                b.Style.Text.Color = on ? th.Colour("on-accent") : th.Colour("text");
                b.Style.Border.Width = on ? 0 : th.Number("stroke", 1f);
                b.Style.Border.Color = th.Colour("border");
                b.Style.Border.Roundness = th.Number("radius");
                b.Style.Shadow = null!;
            });
            themeSwitch.AddChild(btn);
        }

        var nav = new Stack
        {
            Name = "Studio_Nav",
            Orientation = Orientation.Horizontal,
            Gap = 4,
            Padding = new Thickness(3),
            Align = LayoutAlign.Stretch
        };
        nav.MinWidth = 212;
        nav.MinHeight = 32;
        nav.Transform.Width = 212;
        nav.Transform.Height = 32;
        nav.UseStyle("nav");

        var tabBoardBtn = MakeTabButton("Board");
        var tabControlsBtn = MakeTabButton("Controls");
        Stack.SetGrow(tabBoardBtn, 1f);
        Stack.SetGrow(tabControlsBtn, 1f);

        tabBoardBtn.Bind(b =>
        {
            var th = DemoThemes.Current;
            bool on = _activeTab.Value == StudioTab.Board;
            b.NormalColor = on ? th.Colour("accent") : th.Colour("ghost");
            b.Style.Text.Color = on ? th.Colour("on-accent") : th.Colour("text");
            b.Style.Border.Width = on ? 0 : th.Number("stroke", 1f);
            b.Style.Border.Color = th.Colour("border");
            b.Style.Border.Roundness = th.Number("radius");
        });
        tabBoardBtn.Clicked += () => _activeTab.Value = StudioTab.Board;

        tabControlsBtn.Bind(b =>
        {
            var th = DemoThemes.Current;
            bool on = _activeTab.Value == StudioTab.Components;
            b.NormalColor = on ? th.Colour("accent") : th.Colour("ghost");
            b.Style.Text.Color = on ? th.Colour("on-accent") : th.Colour("text");
            b.Style.Border.Width = on ? 0 : th.Number("stroke", 1f);
            b.Style.Border.Color = th.Colour("border");
            b.Style.Border.Roundness = th.Number("radius");
        });
        tabControlsBtn.Clicked += () => _activeTab.Value = StudioTab.Components;

        nav.AddChild(tabBoardBtn);
        nav.AddChild(tabControlsBtn);

        header.AddChild(title);
        header.AddChild(subtitle);
        header.AddChild(DemoLayout.GrowSpacer("Studio_HeaderSpacer"));
        header.AddChild(themeSwitch);
        header.AddChild(nav);

        var contentSlot = new Grid
        {
            Name = "Studio_ContentSlot",
            Columns = "*",
            Rows = "*"
        };
        contentSlot.Style.BackColor = SKColors.Transparent;
        contentSlot.Style.Border.Width = 0;
        contentSlot.OverflowX = OverflowMode.Clip;
        contentSlot.OverflowY = OverflowMode.Clip;

        var tasksTab = new TasksTab();
        var controlsTab = new ControlsTab();
        tasksTab.BindVisible(() => _activeTab.Value == StudioTab.Board);
        controlsTab.BindVisible(() => _activeTab.Value == StudioTab.Components);

        contentSlot.AddChild(tasksTab);
        contentSlot.AddChild(controlsTab);
        Grid.SetCell(tasksTab, 0, 0);
        Grid.SetCell(controlsTab, 0, 0);
        Stack.SetGrow(contentSlot, 1f);

        var statusBar = new Stack
        {
            Name = "Studio_StatusBar",
            Orientation = Orientation.Horizontal,
            Align = LayoutAlign.Center,
            Padding = new Thickness(16, 5)
        };
        statusBar.MinHeight = 26;
        statusBar.Transform.Height = 26;
        statusBar.UseStyle("chrome");

        var engineStatus = new VisualElement
        {
            Text = "Dirty-rect retained pipeline  ·  Fine-grained signals  ·  Keyed For.Each",
            IsClickthrough = true
        };
        engineStatus.UseStyle("status");

        var tabStatus = new VisualElement { IsClickthrough = true }
            .BindText(() =>
            {
                string view = _activeTab.Value == StudioTab.Board ? "Board" : "Controls";
                return $"Theme: {DemoThemes.Active.Value}  ·  View: {view}";
            });
        tabStatus.MinWidth = 220;
        tabStatus.UseStyle("status-end");

        statusBar.AddChild(engineStatus);
        statusBar.AddChild(tabStatus);
        Stack.SetGrow(engineStatus, 1f);

        root.AddChild(header);
        root.AddChild(contentSlot);
        root.AddChild(statusBar);
        root.SizeChanged += (_, w, _) =>
        {
            bool show = w >= 1100f;
            if (subtitle.Visible != show)
                subtitle.Visible = show;
        };

        AddElement(root);
    }

    private static Button MakeTabButton(string label)
    {
        var btn = new Button(label, enable3DEffect: false);
        btn.Style.Border.Width = 0;
        btn.Style.Shadow = null!;
        btn.Style.Text.Size = 12f;
        return btn;
    }
}
