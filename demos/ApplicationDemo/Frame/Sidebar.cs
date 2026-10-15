using System;
using System.IO;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;

namespace Blossom.Testing.Frame;

/// <summary>Brand, navigation (bound to <see cref="AppState.Route"/>) and appearance controls.</summary>
public sealed class Sidebar : Stack
{
    public Sidebar()
    {
        Name = "Sidebar";
        Orientation = Orientation.Vertical;
        Gap = 2;
        Padding = new Thickness(14, 18, 14, 16);
        this.UseStyle("sidebar");

        var logo = new VisualElement { IsClickthrough = true }.Width(26).Height(26);
        logo.BackgroundImageScale = ImageScaleMode.Contain;
        logo.LoadImageFromFile(Path.Combine(AppContext.BaseDirectory, "assets", "icon.png"));

        var brand = Ui.Row(10, logo, Ui.Text("Blossom", "text.brand"), Ui.Spacer(), new Badge("0.2", Tone.Primary));
        brand.Padding = new Thickness(6, 0, 4, 0);
        brand.Height(32);
        AddChild(brand);
        AddChild(Space(18));

        AddChild(Section("Workspace"));
        AddChild(Nav("Overview", "home", Route.Dashboard));
        AddChild(Nav("Components", "grid", Route.Components));
        AddChild(Nav("Tasks", "check-square", Route.Tasks, () => AppState.OpenCount.Value.ToString()));
        AddChild(Nav("Playground", "zap", Route.Playground));

        AddChild(Ui.Spacer());

        AddChild(Section("Appearance"));
        var mode = new Segmented("Dark", "Light");
        mode.SelectionChanged += i => Look.Mode.Value = i == 0 ? ThemeMode.Dark : ThemeMode.Light;
        mode.Bind(s => s.SelectedIndex = Look.Mode.Value == ThemeMode.Dark ? 0 : 1);
        AddChild(mode);
        AddChild(Space(10));

        var swatches = Ui.Row(6);
        swatches.Padding = new Thickness(4, 0);
        foreach (var (name, scale) in Look.Accents)
        {
            var swatch = new Swatch(scale[500]);
            swatch.Clicked += () => Look.Accent.Value = name;
            swatch.Bind(s =>
            {
                s.Selected = Look.Accent.Value == name;
                s.RingColor = Look.Current.Value.Colour("text-muted");
            });
            swatches.AddChild(swatch);
        }
        AddChild(swatches);
        AddChild(Space(14));
        AddChild(Ui.Divider());
        AddChild(Space(12));

        var avatar = new Avatar(AppState.UserName.Peek(), 30f);
        avatar.Bind(a => a.Name = AppState.UserName.Value);
        var user = Ui.Row(10, avatar,
            Ui.Column(1,
                Ui.Text(() => AppState.UserName.Value, "text.strong"),
                Ui.Text("ada@blossom.dev", "text.small")));
        user.Padding = new Thickness(4, 0);
        AddChild(user);
    }

    private static NavItem Nav(string text, string icon, Route route, Func<string>? count = null)
    {
        var item = new NavItem(text, icon);
        item.Clicked += () => AppState.Navigate(route);
        item.Bind(n => n.Active = AppState.Route.Value == route);
        if (count != null)
            item.Bind(n => n.Count = count());
        return item;
    }

    private static VisualElement Section(string text)
    {
        var label = Ui.Text(text.ToUpperInvariant(), "text.caption");
        label.Padding = new Thickness(10, 0, 0, 0);
        label.Height(26);
        return label;
    }

    private static VisualElement Space(float h) => new VisualElement { IsClickthrough = true }.Height(h);
}
