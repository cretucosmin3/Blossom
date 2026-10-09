using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.State;
using Blossom.Testing.Styling;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Pages;

/// <summary>
/// Three columns, each a <see cref="For.Each"/> over a filtered memo. Moving a card writes one signal;
/// only the two affected columns reconcile, and untouched cards keep their elements.
/// </summary>
public sealed class TasksPage : Stack
{
    private static readonly string[] Filters = { "All", "Core", "Reactive", "UI", "Engine" };

    public TasksPage()
    {
        Name = "TasksPage";
        Orientation = Orientation.Vertical;
        Gap = 16;
        Padding = new Thickness(28, 8, 28, 24);

        var filter = CreateSignal(0);
        var draft = CreateSignal("");

        var search = new TextField("Filter by title…", "search").Width(240).BindValue(AppState.Search);
        var categories = new Segmented(Filters).BindSelected(filter);

        var newTitle = new TextField("Add a task and press Enter", "plus").BindValue(draft);
        void Add()
        {
            string category = filter.Peek() == 0 ? "UI" : Filters[filter.Peek()];
            AppState.AddTask(draft.Peek(), category);
            draft.Value = "";
        }
        newTitle.Submitted += Add;
        var add = new Button("Add", "plus");
        add.Bind(b => b.Enabled = draft.Value.Trim().Length > 0);
        add.Clicked += Add;

        var toolbar = new Card();
        toolbar.Orientation = Orientation.Horizontal;
        toolbar.Align = LayoutAlign.Center;
        toolbar.Gap = 10;
        toolbar.Padding = new Thickness(14);
        toolbar.AddChild(search);
        toolbar.AddChild(categories);
        toolbar.AddChild(newTitle.Grow());
        toolbar.AddChild(add);
        AddChild(toolbar);

        var visible = CreateMemo(() =>
        {
            string q = AppState.Search.Value.Trim();
            string cat = Filters[filter.Value];
            return (IReadOnlyList<TaskItem>)AppState.Tasks.Value.Where(t =>
                (cat == "All" || t.Category == cat) &&
                (q.Length == 0 || t.Title.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        });

        var board = new Grid { Columns = "*, *, *", Rows = "*", ColumnGap = 16 };
        board.AddChild(Column("Backlog", TaskStatus.Backlog, "text-subtle", visible));
        board.AddChild(Column("In progress", TaskStatus.Active, "warning", visible));
        board.AddChild(Column("Done", TaskStatus.Done, "success", visible));
        AddChild(board.Grow());
    }

    private static VisualElement Column(string title, TaskStatus status, string dotToken, Memo<IReadOnlyList<TaskItem>> visible)
    {
        var items = CreateMemo(() => (IReadOnlyList<TaskItem>)visible.Value.Where(t => t.Status == status).ToList());

        var column = new Stack { Orientation = Orientation.Vertical, Gap = 4, Padding = new Thickness(6) };
        column.UseStyle("column");
        column.OverflowX = Core.Visual.Enums.OverflowMode.Clip;
        column.OverflowY = Core.Visual.Enums.OverflowMode.Clip;

        var dot = new VisualElement { IsClickthrough = true }.Width(8).Height(8);
        dot.UseStyle("dot");
        dot.Bind(d => d.Style.BackColor = Look.Current.Value.Colour(dotToken));
        var count = new Badge("0");
        count.Bind(b => b.Text = items.Value.Count.ToString());
        var header = Ui.Row(8, dot, Ui.Text(title, "text.strong").Grow(), count);
        header.Padding = new Thickness(10, 8, 8, 6);
        column.AddChild(header);

        var scroller = new ScrollView(10, new Thickness(4, 4, 4, 8));
        var empty = Ui.Text(status == TaskStatus.Done ? "Nothing finished yet" : "No tasks here", "text.small");
        empty.Style.Text.Alignment = TextAlign.Center;
        empty.BindVisible(() => items.Value.Count == 0);
        empty.Height(60);
        scroller.Content.AddChild(empty);
        // startIndex: 1 leaves the empty-state node alone; cards reconcile after it.
        For.Each(scroller.Content, () => items.Value, t => t.Id, t => new TaskCard(t), startIndex: 1);
        column.AddChild(scroller.Grow());

        return column;
    }

    private sealed class TaskCard : Stack
    {
        public TaskCard(TaskItem item)
        {
            Name = $"Task_{item.Id}";
            Orientation = Orientation.Vertical;
            Gap = 10;
            Padding = new Thickness(14, 12, 10, 10);
            this.UseStyle("task-card");

            var tone = item.Category switch
            {
                "Core" => Tone.Primary,
                "Reactive" => Tone.Info,
                "Engine" => Tone.Warning,
                _ => Tone.Success
            };

            var title = Ui.Paragraph(item.Title, item.Status == TaskStatus.Done ? "text.muted" : "text.strong");

            var actions = Ui.Row(2, new Badge(item.Category, tone), Ui.Spacer());
            if (item.Status != TaskStatus.Backlog)
                actions.AddChild(Action("arrow-left", () => AppState.Move(item, item.Status - 1)));
            if (item.Status == TaskStatus.Active)
                actions.AddChild(Action("check", () => AppState.Move(item, TaskStatus.Done)));
            if (item.Status == TaskStatus.Backlog)
                actions.AddChild(Action("arrow-right", () => AppState.Move(item, TaskStatus.Active)));
            actions.AddChild(Action("trash", () => AppState.Remove(item)));

            AddChild(title);
            AddChild(actions);
        }

        private static Button Action(string icon, Action onClick)
        {
            var b = Button.IconOnly(icon, ButtonVariant.Ghost, ButtonSize.Sm);
            b.Clicked += onClick;
            return b;
        }
    }
}
