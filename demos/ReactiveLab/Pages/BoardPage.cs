using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using ReactiveLab.State;
using ReactiveLab.Ui;
using static ReactiveLab.Ui.UiKit;

namespace ReactiveLab.Pages;

/// <summary>
/// Operations on the left, the board in the middle (four keyed <see cref="For.Each"/> columns over
/// filtered memos), and an inspector on the right that edits the selected card's signals directly.
/// </summary>
public sealed class BoardPage : Grid
{
    public BoardPage()
    {
        Name = "BoardPage";
        Columns = "300, *, 290";
        Rows = "*";
        ColumnGap = 14;
        Padding = new Thickness(14, 0, 14, 14);

        var side = new Scroller(12, new Thickness(0, 0, 8, 0));
        side.Content.AddChild(OperationsPanel());
        side.Content.AddChild(LivePanel());
        side.Content.AddChild(StatsPanel());
        side.Content.AddChild(LogPanel());

        var main = Column(12, FilterBar(), BoardColumns().Grow());

        AddChild(side);
        AddChild(main);
        AddChild(Inspector());
    }

    // ── Left: operations, live mode, stats, log ────────────────────────────────────────────

    private static Btn Op(string label, string name, Action action, BtnKind kind = BtnKind.Normal)
    {
        var b = new Btn(label, kind, small: true);
        b.Clicked += () => Ops.Run(name, action);
        return b;
    }

    private static VisualElement OperationsPanel()
    {
        var panel = Panel(8);
        panel.AddChild(Section("Operations", "Each one is timed: writes, memos, effects and list reconciles (layout and paint come after)."));
        panel.AddChild(Buttons(Op("Add 1", "Add 1", () => Board.Add(1)), Op("Add 100", "Add 100", () => Board.Add(100)), Op("Add 1 000", "Add 1 000", () => Board.Add(1000))));
        panel.AddChild(Buttons(Op("Add 5 000", "Add 5 000", () => Board.Add(5000)), Op("Prepend 100", "Prepend 100", () => Board.Prepend(100))));
        panel.AddChild(Buttons(Op("Shuffle", "Shuffle", Board.Shuffle), Op("Reverse", "Reverse", Board.Reverse)));
        panel.AddChild(Buttons(Op("Edit 100 random", "Edit 100 random", () => Board.EditRandom(100)), Op("Move 100 random", "Move 100 random", () => Board.MoveRandom(100))));
        panel.AddChild(Buttons(Op("Remove half", "Remove random half", Board.RemoveRandomHalf), Op("Remove done", "Remove done", Board.RemoveDone)));
        panel.AddChild(Buttons(Op("Clear all", "Clear all", Board.Clear, BtnKind.Danger)));
        panel.AddChild(new Check("Batch each operation").BindChecked(Ops.Batched));
        panel.AddChild(Paragraph("Unbatched, every signal write flushes on its own: try \"Edit 100 random\" while sorting by points and compare the memo counts in the log."));
        return panel;
    }

    private static VisualElement LivePanel()
    {
        var panel = Panel(8);
        panel.AddChild(Section("Live updates", "A timer edits random cards (points, titles, priority, stars) in one batch per tick."));
        panel.AddChild(Text("Ticks per second", 12f, 600));
        panel.AddChild(Choice(Ops.LiveRate, "Off", "10", "30", "60"));
        panel.AddChild(Text("Edits per tick", 12f, 600));
        panel.AddChild(Choice(Ops.LiveEdits, "1", "10", "100", "500"));
        panel.AddChild(new Check("Also move cards between columns").BindChecked(Ops.LiveMoves));
        panel.AddChild(Text(() => Ops.LiveRate.Value == 0
            ? "Stopped"
            : $"{Ops.LiveTicks.Value} ticks · last tick {Ops.LiveTickMs.Value:F2} ms", 12f, muted: true));
        return panel;
    }

    private static VisualElement StatsPanel()
    {
        var panel = Panel(6);
        panel.AddChild(Section("Derived stats", "Memos of memos. Progress reads two memos that both depend on every card's points."));
        panel.AddChild(Text(() => $"{Board.Items.Value.Count:N0} items · {Board.Visible.Value.Count:N0} visible"));
        panel.AddChild(Text(() => $"{Board.DonePoints.Value:N0} of {Board.TotalPoints.Value:N0} points done ({Board.Progress.Value:P0})"));
        panel.AddChild(new Bar().Bind(b => b.Fraction = Board.Progress.Value));
        panel.AddChild(Text(() => $"{Board.StarredCount.Value:N0} starred · {Board.UrgentOpen.Value:N0} urgent and open"));
        var totals = Text(() =>
        {
            _ = LabView.Clock.Value;
            return $"Totals so far: {Diag.CardUpdates:N0} card updates · filter memo {Diag.VisibleRuns:N0} · column memos {Diag.ColumnRuns:N0} · stats memos {Diag.StatsRuns:N0}";
        }, 11f, muted: true).Height(32);
        totals.Style.Text.Overflow = Blossom.Core.Visual.Enums.TextOverflow.Wrap;
        totals.Style.Text.MaxLines = 2;
        panel.AddChild(totals);
        return panel;
    }

    private static VisualElement LogPanel()
    {
        var panel = Panel(6);
        panel.AddChild(Section("Last operations", "cards = bindings of existing cards that re-ran, filter / columns / stats = memo recomputes."));
        var list = Column(6);
        For.Each(list, () => Ops.Log.Value, r => r.Seq, r =>
        {
            var row = Column(1,
                Text($"{r.Name}  ·  {r.Ms:F2} ms", 12.5f, 600),
                Text($"cards {r.Cards:N0} · filter {r.Visible} · columns {r.Columns} · stats {r.Stats}{(r.Batched ? "" : " · unbatched")}", 11f, muted: true));
            return row;
        });
        panel.AddChild(list);
        return panel;
    }

    // ── Middle: filters and the four columns ───────────────────────────────────────────────

    private static VisualElement FilterBar()
    {
        var bar = Panel(10, 10);
        bar.Orientation = Orientation.Horizontal;
        bar.Align = LayoutAlign.Center;
        bar.AddChild(new Field("Search titles…", 150).BindValue(Board.Query));
        bar.AddChild(Text("Sort", 12f, 600));
        bar.AddChild(Choice(Board.Sort, Board.SortNames));
        bar.AddChild(new Check("Desc").BindChecked(Board.Descending));
        bar.AddChild(new Check("Starred").BindChecked(Board.StarredOnly));
        bar.AddChild(Text("Priority", 12f, 600));
        bar.AddChild(Choice(Board.MinPriority, Board.PriorityFilterNames));
        bar.AddChild(Spacer());
        return bar;
    }

    private static VisualElement BoardColumns()
    {
        var grid = new Grid { Columns = "*, *, *, *", Rows = "*", ColumnGap = 10 };
        foreach (var stage in Enum.GetValues<Stage>())
            grid.AddChild(BoardColumn(stage));
        return grid;
    }

    private static VisualElement BoardColumn(Stage stage)
    {
        var items = Board.Column(stage);
        var column = Panel(8, 8);

        var dot = new VisualElement { IsClickthrough = true }.Width(8).Height(8);
        dot.Style.Border.Roundness = 4;
        dot.Bind(d => d.Style.BackColor = Look.Current.Value.StageColour(stage));
        var header = Row(8,
            dot,
            Text(stage.ToString(), 13.5f, 700).Grow(),
            Text(() => $"{items.Value.Count:N0} · {Board.ColumnPoints(stage).Value:N0} pts", 12f, muted: true).Width(110));
        header.Padding = new Thickness(4, 2, 4, 2);
        column.AddChild(header);

        var scroller = new Scroller(6, new Thickness(0, 0, 8, 0));
        // Cards and the empty-state line share one parent: For.Each splices its items in at index 0
        // and leaves the Show.When content (appended after them) alone.
        For.Each(scroller.Content, () => items.Value, i => i.Id, i => new CardView(i));
        Show.When(scroller.Content, () => items.Value.Count == 0, () => Text("No cards", 12f, muted: true).Height(40));
        column.AddChild(scroller.Grow());
        return column;
    }

    // ── Right: inspector for the selected card ─────────────────────────────────────────────

    private static VisualElement Inspector()
    {
        var panel = Panel(10);
        panel.AddChild(Section("Inspector", "Edits write the card's own signals; only that card's bindings re-run."));

        var host = Column(10);
        // Show.When created inside a re-running effect: the effect owns it, so selecting another card
        // removes the previous editor (and its bindings) before mounting the new one.
        host.Bind(h =>
        {
            var item = Board.Selected.Value;
            if (item != null)
                Show.When(h, () => true, () => Editor(item));
        });
        Show.When(panel, () => Board.Selected.Value == null, () => Paragraph("Click a card to edit it here.", 12.5f));
        panel.AddChild(host);
        return panel;
    }

    private static VisualElement Editor(Item item)
    {
        var editor = Column(10);
        editor.AddChild(Text(() => $"Card #{item.Id} · {item.Stage.Value}", 13.5f, 700));

        editor.AddChild(Text("Title", 12f, 600));
        editor.AddChild(new Field("Title", 240).BindValue(item.Title));

        editor.AddChild(Text("Points", 12f, 600));
        var minus = new Btn("−", small: true);
        var plus = new Btn("+", small: true);
        minus.Clicked += () => Ops.Run($"#{item.Id} points −1", () => Board.AddPoints(item, -1));
        plus.Clicked += () => Ops.Run($"#{item.Id} points +1", () => Board.AddPoints(item, +1));
        editor.AddChild(Row(8, minus, Text(() => item.Points.Value.ToString(), 14f, 700).Width(40), plus));

        editor.AddChild(Text("Priority", 12f, 600));
        editor.AddChild(Choice(() => (int)item.Priority.Value, i => item.Priority.Value = (Priority)i, Enum.GetNames<Priority>()));

        editor.AddChild(Text("Stage", 12f, 600));
        editor.AddChild(Choice(() => (int)item.Stage.Value, i => item.Stage.Value = (Stage)i, Enum.GetNames<Stage>()));

        editor.AddChild(new Check("Starred").BindChecked(item.Starred));

        var deselect = new Btn("Deselect", small: true);
        deselect.Clicked += () => Board.Select(null);
        var delete = new Btn("Delete card", BtnKind.Danger, small: true);
        delete.Clicked += () => Ops.Run($"Delete #{item.Id}", () => Board.Remove(item));
        editor.AddChild(Buttons(deselect, delete));
        return editor;
    }
}
