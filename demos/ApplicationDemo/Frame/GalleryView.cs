using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.Pages;
using Blossom.Testing.State;
using Blossom.Testing.Styling;
using SkiaSharp;

namespace Blossom.Testing.Frame;

/// <summary>
/// The demo's single view: sidebar + top bar + routed page area + toast stack.
/// Pages mount and unmount with <see cref="Show.When"/> as <see cref="AppState.Route"/> changes.
/// </summary>
public sealed class GalleryView : View
{
    public GalleryView() : base("Blossom Gallery")
    {
        Look.Initialize();
    }

    public override void Init()
    {
        var root = new AppFrame
        {
            Transform = new Transform(0, 0, Width, Height)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        root.Bind(_ => BackColor = Look.Current.Value.Colour("bg"));
        AddElement(root);
    }

    private sealed class AppFrame : VisualElement
    {
        private readonly Grid _layout;
        private readonly Stack _toasts;

        public AppFrame()
        {
            Name = "AppFrame";
            this.UseStyle("app");

            _layout = new Grid { Columns = "248, 1, *", Rows = "*" };
            var edge = new VisualElement { IsClickthrough = true };
            edge.UseStyle("sidebar-edge");

            var main = new Stack { Orientation = Orientation.Vertical };
            var pages = new Grid { Columns = "*", Rows = "*" };
            main.AddChild(new TopBar());
            main.AddChild(pages.Grow());

            Mount(pages, Route.Dashboard, () => new DashboardPage());
            Mount(pages, Route.Components, () => new ComponentsPage());
            Mount(pages, Route.Tasks, () => new TasksPage());
            Mount(pages, Route.Playground, () => new PlaygroundPage());
            Mount(pages, Route.Horizon, () => new HorizonPage());

            _layout.AddChild(new Sidebar());
            _layout.AddChild(edge);
            _layout.AddChild(main);
            AddChild(_layout);

            _toasts = new Stack { Name = "Toasts", Orientation = Orientation.Vertical, Gap = 10, ZIndex = 500, IsClickthrough = true };
            For.Each(_toasts, () => AppState.Toasts.Value, t => t.Id, t => new Toast(t));
            AddChild(_toasts);
            this.Bind(f =>
            {
                _ = AppState.Toasts.Value;
                f.InvalidateLayout();
            });
        }

        private static void Mount(Grid host, Route route, Func<VisualElement> page)
        {
            Show.When(host, () => AppState.Route.Value == route, () =>
            {
                var el = page();
                Grid.SetCell(el, 0, 0);
                return el;
            });
        }

        protected override void LayoutChildren()
        {
            float x = Transform.AbsoluteX, y = Transform.AbsoluteY;
            float w = Transform.Width, h = Transform.Height;
            _layout.Transform.SetAbsoluteFrame(x, y, w, h);

            const float toastW = 360f, margin = 20f;
            float th = _toasts.GetPreferredSize(toastW, 0).Height;
            _toasts.Transform.SetAbsoluteFrame(x + w - toastW - margin, y + h - th - margin, toastW, Math.Max(0f, th));
        }
    }
}
