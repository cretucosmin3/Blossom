using System;
using System.Linq;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Pages;

/// <summary>Stat cards, a chart and an activity feed: all derived from <see cref="AppState"/> memos.</summary>
public sealed class DashboardPage : ScrollView
{
    public DashboardPage() : base(20, new Thickness(28, 8, 28, 28))
    {
        Name = "DashboardPage";

        var stats = new ColumnLayout("*, *, *, *", 16);
        stats.AddChild(Stat("Open tasks", "check-square",
            () => AppState.OpenCount.Value.ToString(),
            () => $"{AppState.Tasks.Value.Count(t => t.Status == TaskStatus.Active)} in progress",
            new float[] { 4, 6, 5, 7, 6, 8, 7, 9 }));
        stats.AddChild(Stat("Completion", "trending-up",
            () => $"{AppState.Completion.Value * 100:0}%",
            () => $"{AppState.DoneCount.Value} of {AppState.Tasks.Value.Count} done",
            null, progress: () => AppState.Completion.Value));
        stats.AddChild(Stat("Throughput", "activity",
            () => AppState.Throughput.Value.Values.Sum().ToString("0"),
            () => AppState.ChartRange.Value switch { 1 => "this month", 2 => "this year", _ => "this week" },
            new float[] { 3, 5, 4, 8, 6, 9, 12, 11 }));
        stats.AddChild(Stat("Updates", "bell",
            () => AppState.Activity.Value.Count.ToString(),
            () => $"latest {AppState.Ago(AppState.Activity.Value.FirstOrDefault()?.At ?? DateTime.Now)}",
            new float[] { 2, 2, 3, 2, 4, 3, 5, 6 }));
        Content.AddChild(stats);

        var middle = new ColumnLayout("2*, *");
        middle.AddChild(ChartCard());
        middle.AddChild(ActivityCard());
        Content.AddChild(middle);

        var bottom = new ColumnLayout("*, *");
        bottom.AddChild(CategoryCard());
        bottom.AddChild(ActionsCard());
        Content.AddChild(bottom);
    }

    private static Card Stat(string title, string icon, Func<string> value, Func<string> caption, float[]? spark, Func<float>? progress = null)
    {
        var card = new Card();
        card.Gap = 10;

        var glyph = new Icon(icon, 16f);
        glyph.Bind(g => g.Color = Look.Current.Value.Colour("text-subtle"));
        card.AddChild(Ui.Row(8, Ui.Text(title, "text.muted").Grow(), glyph));
        card.AddChild(Ui.Text(value, "text.value"));

        if (progress != null)
        {
            var bar = new ProgressBar(6f);
            bar.Bind(b => b.Value = progress());
            card.AddChild(bar);
        }
        else if (spark != null)
        {
            var line = new Sparkline { Values = spark };
            line.Height(28);
            line.Bind(l => l.LineColor = Look.Current.Value.Colour("primary"));
            card.AddChild(line);
        }

        card.AddChild(Ui.Text(caption, "text.small"));
        return card;
    }

    private static Card ChartCard()
    {
        var card = new Card();
        var range = new Segmented("Week", "Month", "Year").BindSelected(AppState.ChartRange);
        card.AddChild(Ui.Row(12,
            Ui.Column(4, Ui.Text("Throughput", "text.heading"), Ui.Text("Tasks closed per period. Hover a bar.", "text.muted")).Grow(),
            range));

        var chart = new BarChart();
        chart.Bind(c =>
        {
            var (values, labels) = AppState.Throughput.Value;
            c.SetData(values, labels);
        });
        card.AddChild(chart);
        return card;
    }

    private static Card ActivityCard()
    {
        var card = new Card("Recent activity", "Moves on the Tasks board appear here.");
        var list = Ui.Column(14);
        For.Each(list, () => AppState.Activity.Value.Take(5), a => a.Id, a =>
        {
            var what = Ui.Text($"{a.Who.Split(' ')[0]} {a.What}", "text.body");
            what.Style.Text.Overflow = TextOverflow.Ellipsis;
            var col = Ui.Column(2, what, Ui.Text(AppState.Ago(a.At), "text.small")).Grow();
            return Ui.Row(12, new Avatar(a.Who, 32f), col);
        });
        card.AddChild(list);
        return card;
    }

    private static Card CategoryCard()
    {
        var card = new Card("Progress by category", "Each bar is a memo over the task list.");
        foreach (var category in AppState.Categories)
        {
            var share = CreateMemo(() =>
            {
                var items = AppState.Tasks.Value.Where(t => t.Category == category).ToList();
                return items.Count == 0 ? 0f : items.Count(t => t.Status == TaskStatus.Done) / (float)items.Count;
            });
            var bar = new ProgressBar(6f);
            bar.Bind(b => b.Value = share.Value);
            card.AddChild(Ui.Column(8,
                Ui.Row(8, Ui.Text(category, "text.label").Grow(), Ui.Text(() => $"{share.Value * 100:0}%", "text.small")),
                bar));
        }
        return card;
    }

    private static Card ActionsCard()
    {
        var card = new Card("Quick actions", "Buttons are ButtonBase primitives; the look is a theme recipe.");
        var wrap = new WrapPanel { Gap = 8, LineGap = 8 };

        var add = new Button("New task", "plus");
        add.Clicked += () => Dialogs.NewTask(add.ParentView);
        wrap.AddChild(add);

        var tasks = new Button("Open board", "arrow-right", ButtonVariant.Secondary);
        tasks.Clicked += () => AppState.Navigate(Route.Tasks);
        wrap.AddChild(tasks);

        var accent = new Button("Next accent", "droplet", ButtonVariant.Outline);
        accent.Clicked += () =>
        {
            int i = Array.FindIndex(Look.Accents, a => a.Name == Look.Accent.Peek());
            Look.Accent.Value = Look.Accents[(i + 1) % Look.Accents.Length].Name;
        };
        wrap.AddChild(accent);

        var ping = new Button("Notify me", "bell", ButtonVariant.Ghost);
        ping.Clicked += () => AppState.Notify("Hello from Blossom", "Toasts are a keyed list rendered with For.Each.", Tone.Success);
        wrap.AddChild(ping);

        card.AddChild(wrap);
        return card;
    }
}
