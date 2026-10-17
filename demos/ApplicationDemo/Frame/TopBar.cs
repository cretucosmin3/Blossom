using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;

namespace Blossom.Testing.Frame;

/// <summary>Page title (derived from the route), global search, theme toggle, notifications.</summary>
public sealed class TopBar : Stack
{
    public TopBar()
    {
        Name = "TopBar";
        Orientation = Orientation.Horizontal;
        Align = LayoutAlign.Center;
        Gap = 10;
        Padding = new Thickness(28, 14, 24, 14);
        this.Height(68);
        this.UseStyle("topbar");

        var titles = Ui.Column(2,
            Ui.Text(() => Title(AppState.Route.Value).Title, "text.title"),
            Ui.Text(() => Title(AppState.Route.Value).Subtitle, "text.muted"));
        Stack.SetGrow(titles, 1f);
        AddChild(titles);

        var search = new TextField("Search tasks…", "search").Width(260).BindValue(AppState.Search);
        search.Submitted += () => AppState.Navigate(Route.Tasks);
        AddChild(search);

        var theme = Button.IconOnly("moon", ButtonVariant.Outline);
        theme.Bind(b => b.IconName = Look.Mode.Value == ThemeMode.Dark ? "sun" : "moon");
        theme.Clicked += () => Look.Mode.Value = Look.Mode.Peek() == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        AddChild(theme);

        var bell = Button.IconOnly("bell", ButtonVariant.Outline);
        bell.Clicked += () => AppState.Notify("You're all caught up", $"{ReactiveEngine.Untrack(() => AppState.OpenCount.Value)} tasks are still open on the board.", Tone.Info);
        AddChild(bell);
    }

    private static (string Title, string Subtitle) Title(Route route) => route switch
    {
        Route.Components => ("Components", "Controls built on Blossom.Primitives, styled by one theme sheet"),
        Route.Tasks => ("Tasks", "Keyed lists with For.Each, memos and batched updates"),
        Route.Playground => ("Playground", "Shaders, backdrop blur and 3D transforms with live bindings"),
        Route.Horizon => ("Event Horizon", "Hover the buttons: SKSL shaders bend and burn the live pixels around them"),
        _ => ("Overview", "Everything on this page is derived from the same reactive store"),
    };
}
