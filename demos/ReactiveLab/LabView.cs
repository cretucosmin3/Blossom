using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Desktop;
using Blossom.Primitives;
using Blossom.Reactive;
using ReactiveLab.Pages;
using ReactiveLab.State;
using ReactiveLab.Ui;
using static Blossom.Reactive.ReactiveEngine;
using static ReactiveLab.Ui.UiKit;

namespace ReactiveLab;

/// <summary>
/// Top bar (page tabs, accent, dark mode) over the routed page area. Pages mount and unmount with
/// <see cref="Show.When"/>; switching away disposes the page with all its bindings.
/// </summary>
public sealed class LabView : View
{
    public static readonly Signal<int> Page = CreateSignal(0);

    /// <summary>Ticks twice a second; read by text that shows plain (non-signal) counters.</summary>
    public static readonly Signal<long> Clock = CreateSignal(0L);

    public LabView() : base("Reactive Lab") { }

    public override void Init()
    {
        var root = new Root
        {
            Transform = new Transform(0, 0, Width, Height)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        root.Bind(_ => BackColor = Look.Current.Value.Bg);
        AddElement(root);

        Ops.StartLiveDriver();
        Shell.PostPeriodic(TimeSpan.FromMilliseconds(500), () => Clock.Value = Clock.Peek() + 1);

        if (Board.Items.Peek().Count == 0)
            Ops.Run("Seed 200 cards", () => Board.Add(200));
    }

    private sealed class Root : VisualElement
    {
        private readonly Stack _layout;

        public Root()
        {
            Name = "LabRoot";
            _layout = new Stack { Orientation = Orientation.Vertical, Gap = 12 };
            _layout.AddChild(TopBar());

            var pages = new Grid { Columns = "*", Rows = "*" };
            Mount(pages, 0, () => new BoardPage());
            Mount(pages, 1, () => new FeatureLabPage());
            _layout.AddChild(pages.Grow());
            AddChild(_layout);
        }

        private static void Mount(Grid host, int page, Func<VisualElement> build)
        {
            Show.When(host, () => Page.Value == page, () =>
            {
                var el = build();
                Grid.SetCell(el, 0, 0);
                return el;
            });
        }

        private static VisualElement TopBar()
        {
            var bar = Row(12,
                Text("Blossom Reactive Lab", 16f, 700),
                Choice(Page, "Board", "Feature lab"),
                Spacer(),
                Text("Accent", 12f, 600),
                Choice(Look.Accent, Look.AccentNames),
                new Check("Dark").BindChecked(Look.Dark));
            bar.Padding = new Thickness(16, 12, 16, 0);
            bar.Height(46);
            return bar;
        }

        protected override void LayoutChildren()
        {
            _layout.Transform.SetAbsoluteFrame(Transform.AbsoluteX, Transform.AbsoluteY, Transform.Width, Transform.Height);
        }
    }
}
