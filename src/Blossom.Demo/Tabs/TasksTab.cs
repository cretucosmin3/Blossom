using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Reactive;
using Blossom.Testing.Components;
using Blossom.Testing.Models;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Tabs;

public class TasksTab : Container
{
    private readonly Container _toolbar;
    private readonly InputField _searchInput;
    private readonly InputField _newTaskInput;
    private readonly Button _addTaskBtn;
    private readonly VisualElement _statsBadge;
    private readonly Container _progressBarBg;
    private readonly Container _progressBarFill;
    private readonly Button[] _filterChips;
    private readonly Memo<(int Total, int Done, int Percent, string Text)> _statsMemo;

    private readonly ColumnView[] _columns;

    private static readonly string[] ColumnNames = { "Backlog", "In Progress", "Done" };
    private static readonly string[] FilterNames = { "All", "Core", "Reactive", "UI", "Engine" };
    private static readonly SKColor[] ColumnPips =
    {
        new SKColor(163, 163, 163),
        new SKColor(212, 168, 75),
        new SKColor(110, 170, 130)
    };

    public TasksTab() : base(new SKColor(23, 23, 23), roundness: 0f)
    {
        Name = "Studio_TasksTab";
        Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        Style.Border.Width = 0;
        Style.Shadow = null!;

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

        _toolbar = new Container(new SKColor(38, 38, 38), 10f) { Name = "Tasks_Toolbar" };
        _toolbar.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(58, 58, 58), Roundness = 10f };

        _searchInput = new InputField("Search tasks...");
        _searchInput.Changed += text => searchQuery.Value = text ?? "";

        _newTaskInput = new InputField("New task title...");

        void SubmitNewTask()
        {
            var text = _newTaskInput.Value?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Batch(() =>
            {
                var updated = new List<TaskItem>(tasks.Value);
                updated.Insert(0, new TaskItem(text, "Backlog", "UI"));
                tasks.Value = updated;
                _newTaskInput.Value = "";
            });
        }

        _newTaskInput.Submitted += _ => SubmitNewTask();

        _addTaskBtn = new Button("+ Add", new SKColor(79, 70, 229), enable3DEffect: false);
        _addTaskBtn.Clicked += SubmitNewTask;

        _filterChips = new Button[FilterNames.Length];
        for (int i = 0; i < FilterNames.Length; i++)
        {
            string name = FilterNames[i];
            var chip = new Button(name, new SKColor(58, 58, 58), enable3DEffect: false);
            chip.Style.Text.Size = 11f;
            chip.Style.Border.Width = 0;
            chip.Bind(b =>
            {
                b.NormalColor = categoryFilter.Value == name
                    ? new SKColor(79, 70, 229)
                    : new SKColor(58, 58, 58);
            });
            chip.Clicked += () => categoryFilter.Value = name;
            _filterChips[i] = chip;
            _toolbar.AddChild(chip);
        }

        _progressBarBg = new Container(new SKColor(58, 58, 58), 3f) { Name = "Tasks_ProgressBg" };
        _progressBarBg.Style.Border.Width = 0;
        _progressBarBg.Style.Shadow = null!;

        _progressBarFill = new Container(new SKColor(79, 70, 229), 3f) { Name = "Tasks_ProgressFill" };
        _progressBarFill.Style.Border.Width = 0;
        _progressBarFill.Style.Shadow = null!;
        _progressBarBg.AddChild(_progressBarFill);

        _statsBadge = new VisualElement { Name = "Tasks_Stats", IsClickthrough = true }
            .BindText(() => _statsMemo.Value.Text);
        _statsBadge.Style.Text = new TextStyle
        {
            Color = new SKColor(163, 163, 163),
            Size = 12f,
            Weight = 600,
            Alignment = TextAlign.Right
        };

        this.Bind(tab =>
        {
            _ = _statsMemo.Value;
            tab.InvalidateLayout();
        });

        _toolbar.AddChild(_searchInput);
        _toolbar.AddChild(_newTaskInput);
        _toolbar.AddChild(_addTaskBtn);
        _toolbar.AddChild(_progressBarBg);
        _toolbar.AddChild(_statsBadge);
        AddChild(_toolbar);

        _columns = new ColumnView[3];
        for (int i = 0; i < 3; i++)
        {
            string colName = ColumnNames[i];
            var pipColor = ColumnPips[i];

            var col = new Container(new SKColor(38, 38, 38), 10f)
            {
                Name = $"Col_{colName}"
            };
            col.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(58, 58, 58), Roundness = 10f };
            col.OverflowX = OverflowMode.Clip;
            col.OverflowY = OverflowMode.Clip;

            var header = new Container(new SKColor(45, 45, 45), 0f)
            {
                Name = $"ColHeader_{colName}"
            };
            header.Style.Border = new BorderStyle
            {
                Width = 0,
                Color = SKColors.Transparent,
                RoundnessTopLeft = 10f,
                RoundnessTopRight = 10f,
                RoundnessBottomLeft = 0f,
                RoundnessBottomRight = 0f
            };
            header.Style.Shadow = null!;

            var pip = new Container(pipColor, 4f)
            {
                Name = $"Pip_{colName}"
            };
            pip.Style.Border.Width = 0;
            pip.Style.Shadow = null!;

            var title = new VisualElement
            {
                Name = $"ColTitle_{colName}",
                Text = colName,
                IsClickthrough = true
            };
            title.Style.Text = new TextStyle { Color = SKColors.White, Size = 13f, Weight = 700, Alignment = TextAlign.Left };

            var badge = new VisualElement
            {
                Name = $"ColBadge_{colName}",
                IsClickthrough = true
            };
            badge.Style = new ElementStyle
            {
                BackColor = new SKColor(58, 58, 58),
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent, Roundness = 10f },
                Text = new TextStyle { Color = pipColor, Size = 11f, Weight = 700, Alignment = TextAlign.Center }
            };

            header.AddChild(pip);
            header.AddChild(title);
            header.AddChild(badge);
            col.AddChild(header);

            var scroller = new ColumnScrollContainer { Name = $"ColScroll_{colName}" };

            var empty = new VisualElement
            {
                Name = $"ColEmpty_{colName}",
                Text = "No tasks",
                IsClickthrough = true
            };
            empty.Style.Text = new TextStyle
            {
                Color = new SKColor(120, 120, 120),
                Size = 12f,
                Weight = 500,
                Alignment = TextAlign.Center
            };

            col.AddChild(scroller);
            col.AddChild(empty);

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
                parent: scroller,
                items: () => columnTasks.Value,
                keySelector: item => item.Id,
                template: item => new TaskCard(item, tasks));

            _columns[i] = new ColumnView(col, header, pip, title, badge, scroller, empty);
            AddChild(col);
        }
    }

    protected override void LayoutChildren()
    {
        base.LayoutChildren();

        float viewW = Transform.Computed.Width;
        float viewH = Transform.Computed.Height;
        float ox = Transform.Computed.X;
        float oy = Transform.Computed.Y;
        if (viewW < 100 || viewH < 100) return;

        const float padX = 16f;
        const float padY = 12f;
        const float gap = 12f;
        const float toolbarH = 88f;

        float tbW = viewW - padX * 2;
        float tbX = ox + padX;
        float tbY = oy + padY;
        _toolbar.Transform.SetAbsoluteFrame(tbX, tbY, tbW, toolbarH);

        float innerX = tbX + 12f;
        float innerW = tbW - 24f;
        float row1Y = tbY + 10f;
        float rowH = 30f;

        float searchW = Math.Clamp(innerW * 0.28f, 140f, 220f);
        _searchInput.Transform.SetAbsoluteFrame(innerX, row1Y, searchW, rowH);

        float chipX = innerX + searchW + 10f;
        float chipH = 26f;
        float chipY = row1Y + 2f;
        float[] chipWidths = { 44f, 52f, 78f, 40f, 64f };
        for (int i = 0; i < _filterChips.Length; i++)
        {
            float cw = chipWidths[i];
            if (chipX + cw > innerX + innerW) cw = Math.Max(0, innerX + innerW - chipX);
            _filterChips[i].Visible = cw >= 28f;
            if (_filterChips[i].Visible)
            {
                _filterChips[i].Transform.SetAbsoluteFrame(chipX, chipY, cw, chipH);
            }
            chipX += cw + 6f;
        }

        float addW = 88f;
        float statsW = 150f;
        float row2Y = tbY + 48f;
        float statsX = innerX + innerW - statsW;
        float addX = statsX - 10f - addW;
        float inputW = Math.Max(100f, addX - innerX - 8f);
        addX = innerX + inputW + 8f;
        _newTaskInput.Transform.SetAbsoluteFrame(innerX, row2Y, inputW, rowH);
        _addTaskBtn.Transform.SetAbsoluteFrame(addX, row2Y, addW, rowH);

        _statsBadge.Transform.SetAbsoluteFrame(statsX, row2Y - 2f, statsW, 14f);
        _progressBarBg.Transform.SetAbsoluteFrame(statsX, row2Y + 16f, statsW, 6f);

        float fillW = statsW * (_statsMemo.Value.Percent / 100f);
        _progressBarFill.Transform.SetAbsoluteFrame(statsX, row2Y + 16f, Math.Max(0f, fillW), 6f);

        float colTop = oy + padY + toolbarH + gap;
        float colH = Math.Max(140f, oy + viewH - padY - colTop);
        float colW = Math.Max(180f, (tbW - gap * 2) / 3f);

        for (int i = 0; i < 3; i++)
        {
            float colX = ox + padX + i * (colW + gap);
            var c = _columns[i];
            c.Container.Transform.SetAbsoluteFrame(colX, colTop, colW, colH);
            c.Header.Transform.SetAbsoluteFrame(colX, colTop, colW, 40f);
            c.Pip.Transform.SetAbsoluteFrame(colX + 14f, colTop + 16f, 8f, 8f);
            c.Title.Transform.SetAbsoluteFrame(colX + 28f, colTop + 11f, Math.Max(40f, colW - 80f), 18f);
            c.Badge.Transform.SetAbsoluteFrame(colX + colW - 44f, colTop + 9f, 30f, 22f);

            float scrollY = colTop + 40f;
            float scrollH = Math.Max(40f, colH - 40f);
            c.Scroller.Transform.SetAbsoluteFrame(colX, scrollY, colW, scrollH);
            c.Empty.Transform.SetAbsoluteFrame(colX, scrollY + 24f, colW, 20f);
        }
    }

    private sealed class ColumnView
    {
        public Container Container { get; }
        public Container Header { get; }
        public Container Pip { get; }
        public VisualElement Title { get; }
        public VisualElement Badge { get; }
        public ColumnScrollContainer Scroller { get; }
        public VisualElement Empty { get; }

        public ColumnView(
            Container container,
            Container header,
            Container pip,
            VisualElement title,
            VisualElement badge,
            ColumnScrollContainer scroller,
            VisualElement empty)
        {
            Container = container;
            Header = header;
            Pip = pip;
            Title = title;
            Badge = badge;
            Scroller = scroller;
            Empty = empty;
        }
    }

    private sealed class TaskCard : Container
    {
        private readonly VisualElement _catBadge;
        private readonly VisualElement _title;
        private readonly Button _deleteBtn;
        private readonly List<Button> _actions = new();

        public TaskCard(TaskItem item, Signal<List<TaskItem>> tasks)
            : base(item.Column == "Done" ? new SKColor(38, 38, 38) : new SKColor(58, 58, 58), 6f)
        {
            Name = $"Card_{item.Id}";
            Style.Border = new BorderStyle
            {
                Width = 1,
                Color = item.Column == "Done" ? new SKColor(58, 58, 58) : new SKColor(82, 82, 82),
                Roundness = 6f
            };
            Style.Shadow = new ShadowStyle
            {
                Color = SKColors.Black.WithAlpha(40),
                SpreadX = 0,
                SpreadY = 1,
                OffsetX = 0,
                OffsetY = 1
            };

            SKColor tagColor = item.Category switch
            {
                "Core" => new SKColor(190, 120, 120),
                "Reactive" => new SKColor(150, 130, 200),
                "Engine" => new SKColor(200, 160, 90),
                _ => new SKColor(120, 150, 200)
            };

            _catBadge = new VisualElement
            {
                Text = item.Category,
                IsClickthrough = true
            };
            _catBadge.Style = new ElementStyle
            {
                BackColor = new SKColor(tagColor.Red, tagColor.Green, tagColor.Blue, 40),
                Border = new BorderStyle { Width = 1, Color = tagColor.WithAlpha(90), Roundness = 4f },
                Text = new TextStyle { Color = tagColor, Size = 10f, Weight = 700, Alignment = TextAlign.Center }
            };

            _deleteBtn = new Button("x", new SKColor(82, 82, 82), enable3DEffect: false);
            _deleteBtn.Style.Text.Size = 13f;
            _deleteBtn.Clicked += () =>
            {
                var updated = new List<TaskItem>(tasks.Value);
                updated.RemoveAll(t => t.Id == item.Id);
                tasks.Value = updated;
            };

            _title = new VisualElement
            {
                Text = item.Title,
                IsClickthrough = true
            };
            _title.Style.Text = new TextStyle
            {
                Color = item.Column == "Done" ? new SKColor(163, 163, 163) : SKColors.White,
                Size = 13f,
                Weight = 500,
                Alignment = TextAlign.Left,
                Overflow = TextOverflow.Ellipsis,
                MaxLines = 2
            };

            void MoveTo(string column)
            {
                item.Column = column;
                tasks.Value = new List<TaskItem>(tasks.Value);
            }

            if (item.Column == "Backlog")
            {
                AddAction("Start", new SKColor(70, 70, 70), () => MoveTo("In Progress"));
            }
            else if (item.Column == "In Progress")
            {
                AddAction("Back", new SKColor(70, 70, 70), () => MoveTo("Backlog"));
                AddAction("Done", new SKColor(70, 110, 85), () => MoveTo("Done"));
            }
            else
            {
                AddAction("Reopen", new SKColor(70, 70, 70), () => MoveTo("In Progress"));
            }

            AddChild(_catBadge);
            AddChild(_deleteBtn);
            AddChild(_title);

            Events.OnMouseEnter += _ =>
            {
                if (!EffectiveInteractive) return;
                Style.Border.Color = new SKColor(170, 170, 170);
                InvalidatePaint();
            };
            Events.OnMouseLeave += _ =>
            {
                Style.Border.Color = item.Column == "Done"
                    ? new SKColor(58, 58, 58)
                    : new SKColor(82, 82, 82);
                InvalidatePaint();
            };
        }

        private void AddAction(string label, SKColor color, Action onClick)
        {
            var btn = new Button(label, color, enable3DEffect: false);
            btn.Style.Text.Size = 11f;
            btn.Clicked += onClick;
            _actions.Add(btn);
            AddChild(btn);
        }

        protected override void LayoutChildren()
        {
            float x = Transform.Computed.X;
            float y = Transform.Computed.Y;
            float w = Math.Max(1f, Transform.Width);
            float h = Math.Max(1f, Transform.Height);

            float catW = Math.Clamp(_catBadge.Text.Length * 7.2f + 12f, 48f, 86f);
            _catBadge.Transform.SetAbsoluteFrame(x + 10f, y + 8f, catW, 18f);

            const float del = 22f;
            _deleteBtn.Transform.SetAbsoluteFrame(x + w - 8f - del, y + 6f, del, del);

            _title.Transform.SetAbsoluteFrame(x + 10f, y + 30f, Math.Max(20f, w - 20f), 32f);

            float btnY = y + h - 30f;
            float btnX = x + 10f;
            for (int i = 0; i < _actions.Count; i++)
            {
                float bw = i == 0 && _actions.Count == 1 ? 72f : 58f;
                if (i == 1) bw = 58f;
                _actions[i].Transform.SetAbsoluteFrame(btnX, btnY, bw, 22f);
                btnX += bw + 6f;
            }
        }
    }

    public sealed class ColumnScrollContainer : ScrollContainer
    {
        public ColumnScrollContainer()
        {
            OverflowX = OverflowMode.Clip;
            OverflowY = OverflowMode.Scroll;
            ScrollbarVisibilityX = ScrollbarVisibility.Hidden;
        }

        protected override void LayoutChildren()
        {
            base.LayoutChildren();

            float w = Transform.Computed.Width;
            float cardW = Math.Max(120f, w - 16f);
            float cardH = 92f;
            float cardGap = 8f;
            float currentY = 8f;

            var children = Children;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible)
                    continue;
                child.Transform.SetLocalFrame(8f, currentY, cardW, cardH);
                currentY += cardH + cardGap;
            }

            SetContentSize(w, currentY + 4f);
        }
    }
}
