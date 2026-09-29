using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
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
        BackColor = new SKColor(23, 23, 23);
    }

    public override void Init()
    {
        var root = new StudioRoot
        {
            Name = "Studio_Root",
            Transform = new Transform(0, 0, Width, Height)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };

        var header = new Container(new SKColor(38, 38, 38), 0f)
        {
            Name = "Studio_Header"
        };
        header.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(58, 58, 58), Roundness = 0f };
        header.Style.Shadow = null!;

        var title = new VisualElement
        {
            Name = "Studio_Logo",
            Text = "Blossom",
            IsClickthrough = true
        };
        title.Style.Text = new TextStyle { Color = SKColors.White, Size = 18f, Weight = 700, Alignment = TextAlign.Left };

        var subtitle = new VisualElement
        {
            Name = "Studio_Subtitle",
            Text = "Retained UI  ·  SkiaSharp  ·  Blossom.Reactive",
            IsClickthrough = true
        };
        subtitle.Style.Text = new TextStyle { Color = new SKColor(163, 163, 163), Size = 12f, Weight = 500, Alignment = TextAlign.Left };

        var nav = new Container(new SKColor(23, 23, 23), 8f)
        {
            Name = "Studio_Nav"
        };
        nav.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(58, 58, 58), Roundness = 8f };

        var tabBoardBtn = MakeTabButton("Board");
        var tabControlsBtn = MakeTabButton("Controls");

        tabBoardBtn.Bind(b =>
        {
            b.NormalColor = _activeTab.Value == StudioTab.Board
                ? new SKColor(79, 70, 229)
                : new SKColor(38, 38, 38);
        });
        tabBoardBtn.Clicked += () => _activeTab.Value = StudioTab.Board;

        tabControlsBtn.Bind(b =>
        {
            b.NormalColor = _activeTab.Value == StudioTab.Components
                ? new SKColor(79, 70, 229)
                : new SKColor(38, 38, 38);
        });
        tabControlsBtn.Clicked += () => _activeTab.Value = StudioTab.Components;

        nav.AddChild(tabBoardBtn);
        nav.AddChild(tabControlsBtn);

        header.AddChild(title);
        header.AddChild(subtitle);
        header.AddChild(nav);

        var contentSlot = new Container(SKColors.Transparent, 0f)
        {
            Name = "Studio_ContentSlot"
        };
        contentSlot.Style.Border.Width = 0;
        contentSlot.Style.Shadow = null!;
        contentSlot.OverflowX = OverflowMode.Clip;
        contentSlot.OverflowY = OverflowMode.Clip;

        var tasksTab = new TasksTab();
        var controlsTab = new ControlsTab();
        tasksTab.BindVisible(() => _activeTab.Value == StudioTab.Board);
        controlsTab.BindVisible(() => _activeTab.Value == StudioTab.Components);

        contentSlot.AddChild(tasksTab);
        contentSlot.AddChild(controlsTab);

        var statusBar = new Container(new SKColor(28, 28, 28), 0f)
        {
            Name = "Studio_StatusBar"
        };
        statusBar.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(45, 45, 45), Roundness = 0f };
        statusBar.Style.Shadow = null!;

        var engineStatus = new VisualElement
        {
            Text = "Dirty-rect retained pipeline  ·  Fine-grained signals  ·  Keyed For.Each",
            IsClickthrough = true
        };
        engineStatus.Style.Text = new TextStyle { Color = new SKColor(163, 163, 163), Size = 11f, Weight = 500, Alignment = TextAlign.Left };

        var tabStatus = new VisualElement { IsClickthrough = true }
            .BindText(() => _activeTab.Value == StudioTab.Board ? "View: Board" : "View: Controls");
        tabStatus.Style.Text = new TextStyle { Color = new SKColor(163, 163, 163), Size = 11f, Weight = 500, Alignment = TextAlign.Right };

        statusBar.AddChild(engineStatus);
        statusBar.AddChild(tabStatus);

        root.Header = header;
        root.Title = title;
        root.Subtitle = subtitle;
        root.Nav = nav;
        root.TabBoard = tabBoardBtn;
        root.TabControls = tabControlsBtn;
        root.ContentSlot = contentSlot;
        root.TasksTab = tasksTab;
        root.ControlsTab = controlsTab;
        root.StatusBar = statusBar;
        root.EngineStatus = engineStatus;
        root.StatusText = tabStatus;

        root.AddChild(header);
        root.AddChild(contentSlot);
        root.AddChild(statusBar);

        AddElement(root);
    }

    private static Button MakeTabButton(string label)
    {
        var btn = new Button(label, new SKColor(38, 38, 38), enable3DEffect: false);
        btn.Style.Border.Width = 0;
        btn.Style.Shadow = null!;
        btn.Style.Text.Size = 12f;
        return btn;
    }

    private sealed class StudioRoot : Container
    {
        public Container Header { get; set; } = null!;
        public VisualElement Title { get; set; } = null!;
        public VisualElement Subtitle { get; set; } = null!;
        public Container Nav { get; set; } = null!;
        public Button TabBoard { get; set; } = null!;
        public Button TabControls { get; set; } = null!;
        public Container ContentSlot { get; set; } = null!;
        public TasksTab TasksTab { get; set; } = null!;
        public ControlsTab ControlsTab { get; set; } = null!;
        public Container StatusBar { get; set; } = null!;
        public VisualElement EngineStatus { get; set; } = null!;
        public VisualElement StatusText { get; set; } = null!;

        public StudioRoot() : base(new SKColor(23, 23, 23), 0f)
        {
            Style.Border.Width = 0;
            Style.Shadow = null!;
        }

        protected override void LayoutChildren()
        {
            base.LayoutChildren();

            float w = Transform.Computed.Width;
            float h = Transform.Computed.Height;
            float x = Transform.Computed.X;
            float y = Transform.Computed.Y;
            if (w < 100 || h < 100) return;

            const float headerH = 56f;
            const float statusH = 26f;
            float contentH = Math.Max(80f, h - headerH - statusH);

            Header.Transform.SetAbsoluteFrame(x, y, w, headerH);

            Title.Transform.SetAbsoluteFrame(x + 18f, y + 16f, 110f, 24f);

            bool showSubtitle = w >= 980f;
            Subtitle.Visible = showSubtitle;
            if (showSubtitle)
            {
                Subtitle.Transform.SetAbsoluteFrame(x + 132f, y + 19f, 360f, 18f);
            }

            const float navW = 212f;
            const float navH = 32f;
            float navX = x + w - navW - 16f;
            Nav.Transform.SetAbsoluteFrame(navX, y + 12f, navW, navH);
            TabBoard.Transform.SetAbsoluteFrame(navX + 3f, y + 15f, 102f, 26f);
            TabControls.Transform.SetAbsoluteFrame(navX + 107f, y + 15f, 102f, 26f);

            ContentSlot.Transform.SetAbsoluteFrame(x, y + headerH, w, contentH);
            TasksTab.Transform.SetAbsoluteFrame(x, y + headerH, w, contentH);
            ControlsTab.Transform.SetAbsoluteFrame(x, y + headerH, w, contentH);

            StatusBar.Transform.SetAbsoluteFrame(x, y + h - statusH, w, statusH);
            EngineStatus.Transform.SetAbsoluteFrame(x + 16f, y + h - statusH + 5f, Math.Max(80f, w - 200f), 16f);
            StatusText.Transform.SetAbsoluteFrame(x + w - 140f, y + h - statusH + 5f, 124f, 16f);
        }
    }
}
