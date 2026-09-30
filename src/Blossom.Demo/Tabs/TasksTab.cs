using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing;
using Blossom.Testing.Components;
using Blossom.Testing.Models;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Tabs;

public class TasksTab : Stack
{
    private readonly Memo<(int Total, int Done, int Percent, string Text)> _statsMemo;

    private static readonly string[] ColumnNames = { "Backlog", "In Progress", "Done" };
    private static readonly string[] FilterNames = { "All", "Core", "Reactive", "UI", "Engine" };
    private static readonly SKColor[] ColumnPips =
    {
        new SKColor(163, 163, 163),
        new SKColor(212, 168, 75),
        new SKColor(110, 170, 130)
    };

    public TasksTab()
    {
        Name = "Studio_TasksTab";
        Orientation = Orientation.Vertical;
        Gap = 12;
        Padding = new Thickness(16, 12);
        Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        this.UseStyle("app");

        var tasks = CreateSignal(new List<TaskItem>
        {
            new("Harden button pointer capture", "Done", "Core"),
            new("Fine-grained reactive signals", "Done", "Reactive"),
            new("Keyed collection reconciler", "Done", "Reactive"),
            new("Studio showcase shell", "In Progress", "UI"),
            new("Dirty-rect scissor pipeline", "In Progress", "Engine"),
            new("Virtual scrolling benchmarks", "Backlog", "Core"),
            new("SVG icon rendering", "Backlog", "UI"),
            new("Theme design tokens", "Backlog", "UI")
        });

        var searchQuery = CreateSignal("");
        var categoryFilter = CreateSignal("All");

        _statsMemo = CreateMemo(() =>
        {
            int total = tasks.Value.Count;
            int done = tasks.Value.Count(t => t.Column == "Done");
            int percent = total > 0 ? (done * 100) / total : 0;
            return (Total: total, Done: done, Percent: percent, Text: $"{done} / {total} done");
        });

        var toolbar = new Stack
        {
            Name = "Tasks_Toolbar",
            Orientation = Orientation.Vertical,
            Gap = 8,
            Padding = new Thickness(12, 10)
        };
        toolbar.MinHeight = 88;
        toolbar.Transform.Height = 88;
        toolbar.UseStyle("surface");
        toolbar.OverflowX = OverflowMode.Clip;

        var searchInput = new InputField("Search tasks...");
        searchInput.MinWidth = 140;
        searchInput.MaxWidth = 220;
        searchInput.Transform.Width = 180;
        searchInput.UseStyle("field");
        searchInput.Changed += text => searchQuery.Value = text ?? "";

        var row1 = new Stack
        {
            Orientation = Orientation.Horizontal,
            Gap = 6,
            Align = LayoutAlign.Center
        };
        row1.Transform.Height = 30;
        row1.AddChild(searchInput);

        for (int i = 0; i < FilterNames.Length; i++)
        {
            string name = FilterNames[i];
            var chip = new Button(name, enable3DEffect: false);
            chip.Style.Text.Size = 11f;
            chip.Style.Border.Width = 0;
            chip.MinHeight = 26;
            chip.Bind(b =>
            {
                var th = DemoThemes.Current;
                bool on = categoryFilter.Value == name;
                b.NormalColor = on ? th.Colour("accent") : th.Colour("ghost");
                b.Style.Text.Color = on ? th.Colour("on-accent") : th.Colour("text");
                b.Style.Border.Width = on ? 0 : th.Number("stroke", 1f);
                b.Style.Border.Color = th.Colour("border");
                b.Style.Border.Roundness = th.Number("radius");
            });
            chip.Clicked += () => categoryFilter.Value = name;
            row1.AddChild(chip);
        }

        var newTaskInput = new InputField("New task title...");
        newTaskInput.UseStyle("field");

        void SubmitNewTask()
        {
            var text = newTaskInput.Value?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Batch(() =>
            {
                var updated = new List<TaskItem>(tasks.Value);
                updated.Insert(0, new TaskItem(text, "Backlog", "UI"));
                tasks.Value = updated;
                newTaskInput.Value = "";
            });
        }

        newTaskInput.Submitted += _ => SubmitNewTask();

        var addTaskBtn = new Button("+ Add", enable3DEffect: false);
        addTaskBtn.MinWidth = 88;
        addTaskBtn.Transform.Width = 88;
        DemoThemes.TintButton(addTaskBtn, "accent");

        var statsCol = new Stack
        {
            Orientation = Orientation.Vertical,
            Gap = 4,
            Align = LayoutAlign.Stretch
        };
        statsCol.MinWidth = 150;
        statsCol.Transform.Width = 150;

        var statsBadge = new VisualElement { Name = "Tasks_Stats", IsClickthrough = true }
            .BindText(() => _statsMemo.Value.Text);
        statsBadge.UseStyle("status-end");

        var progress = new ProgressTrack { Name = "Tasks_ProgressBg" };
        progress.Fraction = () => _statsMemo.Value.Percent / 100f;

        statsCol.AddChild(statsBadge);
        statsCol.AddChild(progress);

        this.Bind(tab =>
        {
            _ = _statsMemo.Value;
            progress.InvalidateLayout();
        });

        var row2 = new Stack
        {
            Orientation = Orientation.Horizontal,
            Gap = 8,
            Align = LayoutAlign.Center
        };
        row2.Transform.Height = 30;
        row2.AddChild(newTaskInput);
        row2.AddChild(addTaskBtn);
        row2.AddChild(statsCol);
        Stack.SetGrow(newTaskInput, 1f);

        toolbar.AddChild(row1);
        toolbar.AddChild(row2);
        AddChild(toolbar);

        var board = new Grid
        {
            Name = "Tasks_Board",
            Columns = "*, *, *",
            ColumnGap = 12
        };
        Stack.SetGrow(board, 1f);

        for (int i = 0; i < 3; i++)
        {
            string colName = ColumnNames[i];
            var pipColor = ColumnPips[i];

            var col = new KanbanColumn { Name = $"Col_{colName}" };
            col.UseStyle("surface");

            var header = new Stack
            {
                Name = $"ColHeader_{colName}",
                Orientation = Orientation.Horizontal,
                Gap = 8,
                Align = LayoutAlign.Center,
                Padding = new Thickness(14, 9, 14, 9)
            };
            header.MinHeight = 40;
            header.Transform.Height = 40;
            header.UseStyle("header");

            var pip = new VisualElement { Name = $"Pip_{colName}", IsClickthrough = true };
            pip.MinWidth = 8;
            pip.MinHeight = 8;
            pip.Transform.Width = 8;
            pip.Transform.Height = 8;
            pip.Style.BackColor = pipColor;
            pip.Style.Border = new BorderStyle { Width = 0, Roundness = 4f };
            pip.Style.Shadow = null!;

            var title = new VisualElement
            {
                Name = $"ColTitle_{colName}",
                Text = colName,
                IsClickthrough = true
            };
            title.UseStyle("heading");

            var badge = new VisualElement
            {
                Name = $"ColBadge_{colName}",
                IsClickthrough = true
            };
            badge.MinWidth = 30;
            badge.Transform.Width = 30;
            badge.Transform.Height = 22;
            badge.UseStyle("badge");
            badge.Style.Text.Color = pipColor;
            badge.Style.Text.Size = 11f;
            badge.Style.Text.Weight = 700;
            badge.Style.Text.Alignment = TextAlign.Center;

            header.AddChild(pip);
            header.AddChild(title);
            header.AddChild(DemoLayout.GrowSpacer());
            header.AddChild(badge);
            Stack.SetGrow(title, 1f);

            var scroller = new ColumnScrollContainer { Name = $"ColScroll_{colName}" };

            var empty = new VisualElement
            {
                Name = $"ColEmpty_{colName}",
                Text = "No tasks",
                IsClickthrough = true
            };
            empty.UseStyle("empty");

            col.Scroller = scroller;
            col.Empty = empty;
            col.AddChild(header);
            col.AddChild(scroller);
            col.AddChild(empty);
            Layout.SetManual(empty, true);
            Stack.SetGrow(scroller, 1f);

            string targetCol = colName;
            var columnTasks = CreateMemo(() =>
            {
                var query = searchQuery.Value.Trim();
                var cat = categoryFilter.Value;
                return tasks.Value.Where(t =>
                {
                    if (t.Column != targetCol) return false;
                    if (query.Length > 0 && t.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) return false;
                    if (cat != "All" && t.Category != cat) return false;
                    return true;
                }).ToList();
            });

            badge.BindText(() => columnTasks.Value.Count.ToString());
            empty.BindVisible(() => columnTasks.Value.Count == 0);

            For.Each<TaskItem, Guid>(
                parent: scroller.List,
                items: () => columnTasks.Value,
                keySelector: item => item.Id,
                template: item => new TaskCard(item, tasks));

            board.AddChild(col);
        }

        AddChild(board);
    }

    private sealed class ProgressTrack : VisualElement
    {
        private readonly VisualElement _fill;
        public Func<float>? Fraction;

        public ProgressTrack()
        {
            IsClickthrough = true;
            MinHeight = 6;
            Transform.Height = 6;
            this.UseStyle("progress-track");

            _fill = new VisualElement { IsClickthrough = true };
            _fill.UseStyle("progress-fill");
            AddChild(_fill);
            Layout.SetManual(_fill, true);
        }

        protected override void LayoutChildren()
        {
            float f = Math.Clamp(Fraction?.Invoke() ?? 0f, 0f, 1f);
            _fill.Transform.SetAbsoluteFrame(
                Transform.AbsoluteX,
                Transform.AbsoluteY,
                Transform.Width * f,
                Transform.Height);
        }
    }

    private sealed class KanbanColumn : Stack
    {
        public ColumnScrollContainer Scroller { get; set; } = null!;
        public VisualElement Empty { get; set; } = null!;

        public KanbanColumn()
        {
            Orientation = Orientation.Vertical;
            OverflowX = OverflowMode.Clip;
            OverflowY = OverflowMode.Clip;
        }

        protected override void LayoutChildren()
        {
            base.LayoutChildren();
            if (Empty == null || Scroller == null) return;
            Empty.Transform.SetAbsoluteFrame(
                Transform.AbsoluteX,
                Scroller.Transform.AbsoluteY + 24f,
                Transform.Width,
                20f);
        }
    }

    private sealed class TaskCard : Stack
    {
        public TaskCard(TaskItem item, Signal<List<TaskItem>> tasks)
        {
            Name = $"Card_{item.Id}";
            Orientation = Orientation.Vertical;
            Gap = 6;
            Padding = new Thickness(10, 8);
            Align = LayoutAlign.Stretch;
            MinHeight = 92;
            Transform.Height = 92;
            this.UseStyle(item.Column == "Done" ? "card-done" : "card");

            SKColor tagColor = item.Category switch
            {
                "Core" => new SKColor(190, 120, 120),
                "Reactive" => new SKColor(150, 130, 200),
                "Engine" => new SKColor(200, 160, 90),
                _ => new SKColor(120, 150, 200)
            };

            var catBadge = new VisualElement
            {
                Text = item.Category,
                IsClickthrough = true
            };
            catBadge.MinWidth = 48;
            catBadge.MaxWidth = 86;
            catBadge.Transform.Height = 18;
            catBadge.Style = new ElementStyle
            {
                BackColor = new SKColor(tagColor.Red, tagColor.Green, tagColor.Blue, 40),
                Border = new BorderStyle { Width = 1, Color = tagColor.WithAlpha(90), Roundness = 4f },
                Text = new TextStyle { Color = tagColor, Size = 10f, Weight = 700, Alignment = TextAlign.Center }
            };

            var deleteBtn = new Button("x", enable3DEffect: false);
            deleteBtn.Style.Text.Size = 13f;
            deleteBtn.MinWidth = 22;
            deleteBtn.MinHeight = 22;
            deleteBtn.Transform.Width = 22;
            deleteBtn.Transform.Height = 22;
            DemoThemes.TintButton(deleteBtn, "ghost", "text");
            deleteBtn.Clicked += () =>
            {
                var updated = new List<TaskItem>(tasks.Value);
                updated.RemoveAll(t => t.Id == item.Id);
                tasks.Value = updated;
            };

            var top = new Stack
            {
                Orientation = Orientation.Horizontal,
                Gap = 8,
                Align = LayoutAlign.Center
            };
            top.Transform.Height = 22;
            top.AddChild(catBadge);
            top.AddChild(DemoLayout.GrowSpacer());
            top.AddChild(deleteBtn);

            var title = new VisualElement
            {
                Text = item.Title,
                IsClickthrough = true
            };
            title.MinHeight = 32;
            title.Transform.Height = 32;
            title.UseStyle(item.Column == "Done" ? "card-title-done" : "card-title");

            void MoveTo(string column)
            {
                item.Column = column;
                tasks.Value = new List<TaskItem>(tasks.Value);
            }

            var actions = new Stack
            {
                Orientation = Orientation.Horizontal,
                Gap = 6,
                Align = LayoutAlign.Center
            };
            actions.Transform.Height = 22;

            void AddAction(string label, string colourToken, Action onClick)
            {
                var btn = new Button(label, enable3DEffect: false);
                btn.Style.Text.Size = 11f;
                btn.MinWidth = 58;
                btn.MinHeight = 22;
                btn.Transform.Height = 22;
                DemoThemes.TintButton(btn, colourToken, colourToken == "success" ? "on-accent" : "text");
                btn.Clicked += onClick;
                actions.AddChild(btn);
            }

            if (item.Column == "Backlog")
            {
                AddAction("Start", "ghost", () => MoveTo("In Progress"));
            }
            else if (item.Column == "In Progress")
            {
                AddAction("Back", "ghost", () => MoveTo("Backlog"));
                AddAction("Done", "success", () => MoveTo("Done"));
            }
            else
            {
                AddAction("Reopen", "ghost", () => MoveTo("In Progress"));
            }

            AddChild(top);
            AddChild(title);
            AddChild(actions);

            Events.OnMouseEnter += _ =>
            {
                if (!EffectiveInteractive) return;
                Style.Border.Color = Themes.Current.Colour("accent");
                InvalidatePaint();
            };
            Events.OnMouseLeave += _ =>
            {
                Style.Border.Color = Themes.Current.Colour(
                    item.Column == "Done" ? "border" : "border-strong");
                InvalidatePaint();
            };
        }
    }

    public sealed class ColumnScrollContainer : ScrollContainer
    {
        public Stack List { get; }

        public ColumnScrollContainer()
        {
            OverflowX = OverflowMode.Clip;
            OverflowY = OverflowMode.Scroll;
            ScrollbarVisibilityX = ScrollbarVisibility.Hidden;
            List = new Stack
            {
                Orientation = Orientation.Vertical,
                Gap = 8,
                Padding = new Thickness(8)
            };
            AddChild(List);
        }

        protected override void LayoutChildren()
        {
            base.LayoutChildren();
            float w = Math.Max(1f, Transform.Computed.Width);
            float pref = List.GetPreferredSize(w, 0).Height;
            List.Transform.SetLocalFrame(0, 0, w, Math.Max(1f, pref));
            SetContentSize(w, Math.Max(1f, pref));
        }
    }
}
