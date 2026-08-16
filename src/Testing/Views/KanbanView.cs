using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual;
using Blossom.Testing.Components;
using Blossom.Testing.Models;
using SkiaSharp;

namespace Blossom.Testing.Views;

public class KanbanView : View
{
    private readonly List<TodoItem> _items = new();
    private TodoItem? _editingItem;
    private bool _rebuildQueued;

    // Visual settings (tweaked from Settings modal)
    private bool _isCompact;
    private bool _showShadows = true;
    private bool _dimCompleted = true;
    private float _cardHeight = 64f;
    private float _cardGap = 8f;
    private float _columnGap = 14f;
    private float _cardRoundness = 6f;
    private float _panelRoundness = 10f;

    private TodoCard? _draggingCard;
    private float _dragGrabOffsetX;
    private float _dragGrabOffsetY;

    private KanbanBoardRoot _boardRoot = null!;

    private Container _headerContainer = null!;
    private VisualElement _titleElement = null!;
    private VisualElement _subtitleElement = null!;
    private Button _designerBtn = null!;
    private Button _settingsBtn = null!;
    private Button _addTaskBtn = null!;

    private readonly string[] _columnNames = { "Backlog", "In Progress", "Done" };
    private readonly Container[] _colContainers = new Container[3];
    private readonly Container[] _colHeaderContainers = new Container[3];
    private readonly VisualElement[] _colTitleElements = new VisualElement[3];
    private readonly VisualElement[] _colBadgeElements = new VisualElement[3];
    private readonly ScrollContainer[] _colScrollContainers = new ScrollContainer[3];
    private readonly VisualElement[] _emptyPlaceholders = new VisualElement[3];

    private Modal _addModal = null!;
    private InputField _addTitleInput = null!;

    private Modal _editModal = null!;
    private InputField _editTitleInput = null!;
    private Button _editDeleteBtn = null!;

    private Modal _settingsModal = null!;
    private Switch _settingsCompact = null!;
    private Switch _settingsShadows = null!;
    private Switch _settingsDimCompleted = null!;
    private Slider _settingsCardHeight = null!;
    private Slider _settingsCardGap = null!;
    private Slider _settingsColumnGap = null!;
    private Slider _settingsCardRoundness = null!;
    private Slider _settingsPanelRoundness = null!;

    /// <summary>
    /// Optional host strip under the header for plugins attached from the Component Designer.
    /// Hidden until a plugin is attached — not part of Board Settings.
    /// </summary>
    private Container _pluginSlot = null!;
    private PluginRoot? _currentPlugin;

    // Cached column frames for drop hit-testing (absolute)
    private readonly SKRect[] _columnHitRects = new SKRect[3];

    public KanbanView() : base("Blossom Tasks")
    {
        BackColor = new SKColor(23, 23, 23);
        // App fills the window; root anchors reflow with client size.
        Canvas = new DesignCanvas(1000f, 1000f);
    }

    public override void Init()
    {
        SeedDefaultTasks();

        _boardRoot = new KanbanBoardRoot(this)
        {
            Name = "Kanban_BoardRoot",
            Transform = new Transform(0, 0, Width, Height)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };

        BuildHeader();
        BuildPluginHostStrip();
        BuildColumns();
        BuildModals();

        AddElement(_boardRoot);
        RebuildBoard();
    }

    private void BuildPluginHostStrip()
    {
        // Host slot for designer → board attach. Starts collapsed (not in Settings).
        _pluginSlot = new Container(SKColors.Transparent, 0f)
        {
            Name = "Kanban_PluginSlot",
            Visible = false,
            Transform = new Transform(0, 0, 100f, 0f)
        };
        _pluginSlot.Style.BackColor = SKColors.Transparent;
        _pluginSlot.Style.Border.Width = 0;
        _pluginSlot.Style.Shadow = null!;
        _boardRoot.AddChild(_pluginSlot);
    }

    private void SeedDefaultTasks()
    {
        if (_items.Count > 0) return;

        _items.Add(new TodoItem { Title = "Design token palette", Column = TodoColumn.Backlog });
        _items.Add(new TodoItem { Title = "Keyboard navigation support", Column = TodoColumn.Backlog });
        _items.Add(new TodoItem { Title = "Implement drag-and-drop mechanics", Column = TodoColumn.InProgress });
        _items.Add(new TodoItem { Title = "Virtual scrolling performance", Column = TodoColumn.InProgress });
        _items.Add(new TodoItem { Title = "Scissor clipping pipeline", Column = TodoColumn.Done, IsDone = true });
        _items.Add(new TodoItem { Title = "Pointer capture foundation", Column = TodoColumn.Done, IsDone = true });
    }

    private void BuildHeader()
    {
        _headerContainer = new Container(new SKColor(38, 38, 38), _panelRoundness)
        {
            Name = "Kanban_Header"
        };

        _titleElement = Label("Kanban_Title", "Blossom Tasks", SKColors.White, 22, 700);
        _subtitleElement = Label("Kanban_Subtitle", "Todo board", new SKColor(163, 163, 163), 12, 500);

        _designerBtn = new Button("🎨 Designer", new SKColor(79, 70, 229)) { Name = "Kanban_Designer" };
        _designerBtn.Clicked += OpenComponentDesigner;

        _settingsBtn = new Button("Settings", new SKColor(70, 70, 70)) { Name = "Kanban_Settings" };
        _settingsBtn.Clicked += OpenSettingsModal;

        _addTaskBtn = new Button("+ Add Task", new SKColor(100, 100, 100)) { Name = "Kanban_Add" };
        _addTaskBtn.Clicked += OpenAddModal;

        _headerContainer.AddChild(_titleElement);
        _headerContainer.AddChild(_subtitleElement);
        _headerContainer.AddChild(_designerBtn);
        _headerContainer.AddChild(_settingsBtn);
        _headerContainer.AddChild(_addTaskBtn);
        _boardRoot.AddChild(_headerContainer);
    }

    private void BuildColumns()
    {
        for (int i = 0; i < 3; i++)
        {
            string colName = _columnNames[i];

            _colContainers[i] = new Container(new SKColor(38, 38, 38), 10f)
            {
                Name = $"Col_{colName}"
            };

            _colHeaderContainers[i] = new Container(SKColors.Transparent, 0f)
            {
                Name = $"ColHeader_{colName}"
            };
            _colHeaderContainers[i].Style.BackColor = SKColors.Transparent;
            _colHeaderContainers[i].Style.Border.Width = 0;
            _colHeaderContainers[i].Style.Shadow = null!;

            _colTitleElements[i] = Label($"ColTitle_{colName}", colName, new SKColor(235, 235, 235), 14, 700);

            _colBadgeElements[i] = new VisualElement
            {
                Name = $"ColBadge_{colName}",
                Text = "0",
                IsClickthrough = true,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(58, 58, 58),
                    Border = new BorderStyle
                    {
                        Width = 1,
                        Color = new SKColor(82, 82, 82),
                        Roundness = 10
                    },
                    Text = new TextStyle
                    {
                        Color = new SKColor(200, 200, 200),
                        Size = 11,
                        Weight = 600,
                        Alignment = TextAlign.Center
                    }
                }
            };

            _colScrollContainers[i] = new ScrollContainer
            {
                Name = $"ColScroll_{colName}",
                ScrollbarThickness = 6f,
                ScrollbarRadius = 3f,
                ScrollbarThumbColor = new SKColor(120, 120, 120, 160),
                ScrollbarThumbHoverColor = new SKColor(163, 163, 163, 220),
                Style = new ElementStyle
                {
                    BackColor = new SKColor(23, 23, 23, 80),
                    Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
                }
            };

            _emptyPlaceholders[i] = Label($"Empty_{colName}", "No tasks yet", new SKColor(120, 120, 120), 13, 400);
            _emptyPlaceholders[i].Visible = false;

            _colHeaderContainers[i].AddChild(_colTitleElements[i]);
            _colHeaderContainers[i].AddChild(_colBadgeElements[i]);
            _colContainers[i].AddChild(_colHeaderContainers[i]);
            _colContainers[i].AddChild(_colScrollContainers[i]);
            _colScrollContainers[i].AddChild(_emptyPlaceholders[i]);
            _boardRoot.AddChild(_colContainers[i]);
        }
    }

    private void BuildModals()
    {
        _addModal = new Modal("Add New Task")
        {
            Name = "Kanban_AddModal",
            CardWidth = 440f,
            CardHeight = 200f
        };
        _addModal.PrimaryButton.Label = "Add Task";
        _addTitleInput = new InputField("What needs to be done?");
        _addTitleInput.Submitted += _ => ConfirmAddTask();
        _addTitleInput.Escaped += () => _addModal.Close();
        _addModal.AddContent(_addTitleInput);
        _addModal.Confirmed += ConfirmAddTask;

        _editModal = new Modal("Edit Task")
        {
            Name = "Kanban_EditModal",
            CardWidth = 440f,
            CardHeight = 240f
        };
        _editModal.PrimaryButton.Label = "Save";
        _editTitleInput = new InputField("Task title...");
        _editTitleInput.Submitted += _ => ConfirmEditTask();
        _editTitleInput.Escaped += () => _editModal.Close();
        _editDeleteBtn = new Button("Delete", new SKColor(225, 29, 72));
        _editDeleteBtn.Clicked += () =>
        {
            if (_editingItem == null) return;
            _items.Remove(_editingItem);
            _editingItem = null;
            _editModal.Close();
            QueueRebuild();
        };
        _editModal.AddContent(_editTitleInput);
        _editModal.AddContent(_editDeleteBtn);
        _editModal.Confirmed += ConfirmEditTask;

        // Settings modal — board visual tweaks only (no plugin demos / designer chrome)
        _settingsModal = new Modal("Board Settings")
        {
            Name = "Kanban_SettingsModal",
            CardWidth = 440f,
            CardHeight = 440f
        };
        _settingsModal.PrimaryButton.Label = "Done";
        _settingsModal.SecondaryButton.Visible = false; // single dismiss action

        _settingsCompact = new Switch("Compact cards", _isCompact);
        _settingsCompact.Changed += v =>
        {
            _isCompact = v;
            if (_isCompact)
                _settingsCardHeight.Value = 44f;
            else if (_settingsCardHeight.Value < 50f)
                _settingsCardHeight.Value = 64f;
            ApplyVisualSettings(rebuild: true);
        };

        _settingsShadows = new Switch("Card shadows", _showShadows);
        _settingsShadows.Changed += v =>
        {
            _showShadows = v;
            ApplyVisualSettings(rebuild: true);
        };

        _settingsDimCompleted = new Switch("Dim completed cards", _dimCompleted);
        _settingsDimCompleted.Changed += v =>
        {
            _dimCompleted = v;
            ApplyVisualSettings(rebuild: true);
        };

        _settingsCardHeight = new Slider("Card height", 36f, 90f, _cardHeight)
        {
            ValueFormat = "{0:0} px"
        };
        _settingsCardHeight.Changed += v =>
        {
            _cardHeight = v;
            _isCompact = v <= 48f;
            _settingsCompact.IsOn = _isCompact;
            ApplyVisualSettings(rebuild: true);
        };

        _settingsCardGap = new Slider("Card spacing", 2f, 24f, _cardGap)
        {
            ValueFormat = "{0:0} px"
        };
        _settingsCardGap.Changed += v =>
        {
            _cardGap = v;
            ApplyVisualSettings(rebuild: false);
        };

        _settingsColumnGap = new Slider("Column gap", 6f, 32f, _columnGap)
        {
            ValueFormat = "{0:0} px"
        };
        _settingsColumnGap.Changed += v =>
        {
            _columnGap = v;
            ApplyVisualSettings(rebuild: false);
        };

        _settingsCardRoundness = new Slider("Card corners", 0f, 20f, _cardRoundness)
        {
            ValueFormat = "{0:0}"
        };
        _settingsCardRoundness.Changed += v =>
        {
            _cardRoundness = v;
            ApplyVisualSettings(rebuild: true);
        };

        _settingsPanelRoundness = new Slider("Panel corners", 0f, 24f, _panelRoundness)
        {
            ValueFormat = "{0:0}"
        };
        _settingsPanelRoundness.Changed += v =>
        {
            _panelRoundness = v;
            ApplyVisualSettings(rebuild: false);
        };

        foreach (var el in new VisualElement[]
                 {
                     _settingsCompact, _settingsShadows, _settingsDimCompleted,
                     _settingsCardHeight, _settingsCardGap, _settingsColumnGap,
                     _settingsCardRoundness, _settingsPanelRoundness
                 })
        {
            el.Transform.Height = el is Slider ? 44f : 28f;
            _settingsModal.AddContent(el);
        }

        _settingsModal.Confirmed += () => _settingsModal.Close();

        _boardRoot.AddChild(_addModal);
        _boardRoot.AddChild(_editModal);
        _boardRoot.AddChild(_settingsModal);
    }

    public void OpenComponentDesigner()
    {
        Application?.SetActiveView("Component Designer");
    }

    /// <summary>
    /// Attaches an authored plugin into the board host strip under the header (from Component Designer).
    /// </summary>
    public void AttachPlugin(PluginRoot plugin)
    {
        if (plugin == null || _pluginSlot == null) return;

        if (_currentPlugin != null)
            PluginEmbed.Detach(_currentPlugin);

        _currentPlugin = plugin;
        _pluginSlot.Visible = true;

        float slotH = plugin is SampleBoardMetricsPlugin ? 160f : 120f;
        _pluginSlot.Transform.Height = slotH;
        _pluginSlot.Embed(_currentPlugin);

        if (_currentPlugin is SampleBoardMetricsPlugin metricsPlugin)
            metricsPlugin.UpdateStats();
        else if (_currentPlugin is SampleBoardStatsPlugin statsPlugin)
            statsPlugin.UpdateStats();

        ForceLayoutEvaluation();
    }

    public (int total, int inProgress, int done) GetBoardStats()
    {
        int total = _items.Count;
        int inProgress = _items.Count(x => x.Column == TodoColumn.InProgress);
        int done = _items.Count(x => x.Column == TodoColumn.Done || x.IsDone);
        return (total, inProgress, done);
    }

    public void AddSampleTaskFromPlugin()
    {
        _items.Add(new TodoItem
        {
            Title = $"Plugin Task #{_items.Count + 1}",
            Column = TodoColumn.Backlog
        });
        QueueRebuild();
    }

    private void OpenSettingsModal()
    {
        if (_draggingCard != null) return;
        // Sync controls with current values (without firing loops via property sets carefully)
        _settingsCompact.IsOn = _isCompact;
        _settingsShadows.IsOn = _showShadows;
        _settingsDimCompleted.IsOn = _dimCompleted;
        _settingsCardHeight.Value = _cardHeight;
        _settingsCardGap.Value = _cardGap;
        _settingsColumnGap.Value = _columnGap;
        _settingsCardRoundness.Value = _cardRoundness;
        _settingsPanelRoundness.Value = _panelRoundness;

        _settingsModal.Show();
        _boardRoot.InvalidateLayout();
    }

    private void ApplyVisualSettings(bool rebuild)
    {
        // Panels / header corners
        _headerContainer.Style.Border.Roundness = _panelRoundness;
        for (int i = 0; i < 3; i++)
            _colContainers[i].Style.Border.Roundness = _panelRoundness;

        if (rebuild)
            QueueRebuild();
        else
        {
            _boardRoot.InvalidateLayout();
            _boardRoot.ForceLayoutSubtree();
            FullRenderRequired = true;
            RenderRequired = true;
        }
    }

    private static VisualElement Label(string name, string text, SKColor color, float size, int weight) =>
        new()
        {
            Name = name,
            Text = text,
            IsClickthrough = true,
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = color,
                    Size = size,
                    Weight = weight,
                    Alignment = TextAlign.Left
                }
            }
        };

    private void OpenAddModal()
    {
        _addTitleInput.Value = "";
        _addModal.Show();
        _boardRoot.InvalidateLayout();
        SetActiveKeyboardElement(_addTitleInput);
    }

    private void ConfirmAddTask()
    {
        var title = _addTitleInput.Value?.Trim();
        if (!string.IsNullOrEmpty(title))
        {
            _items.Add(new TodoItem { Title = title, Column = TodoColumn.Backlog });
            _addTitleInput.Value = "";
            QueueRebuild();
        }
        _addModal.Close();
    }

    private void OpenEditModal(TodoItem item)
    {
        if (_draggingCard != null) return;
        _editingItem = item;
        _editTitleInput.Value = item.Title;
        _editModal.Show();
        _boardRoot.InvalidateLayout();
        SetActiveKeyboardElement(_editTitleInput);
    }

    private void ConfirmEditTask()
    {
        var title = _editTitleInput.Value?.Trim();
        if (!string.IsNullOrEmpty(title) && _editingItem != null)
        {
            _editingItem.Title = title;
            QueueRebuild();
        }
        _editModal.Close();
    }

    /// <summary>Defer rebuild so we never destroy elements mid-event (crash source).</summary>
    private void QueueRebuild()
    {
        if (_rebuildQueued) return;
        _rebuildQueued = true;
        Browser.Post(() =>
        {
            _rebuildQueued = false;
            RebuildBoard();
        });
    }

    private void OnCardDragStarted(TodoCard card, System.Numerics.Vector2 globalPos, float grabOffsetX, float grabOffsetY)
    {
        if (_addModal.IsOpen || _editModal.IsOpen || _settingsModal.IsOpen) return;

        _draggingCard = card;
        _dragGrabOffsetX = grabOffsetX;
        _dragGrabOffsetY = grabOffsetY;

        // Lift out of scroll clip
        card.Parent?.RemoveChild(card);
        _boardRoot.AddChild(card);

        card.ZIndex = 500;
        card.CapturePointer();
        card.Style.Border.Color = new SKColor(170, 170, 170);
        card.Style.Shadow = new ShadowStyle
        {
            Color = SKColors.Black.WithAlpha(140),
            SpreadX = 4,
            SpreadY = 8,
            OffsetX = 0,
            OffsetY = 8
        };

        // Capture previous subtree bounds, move, re-layout children immediately, then paint both areas
        MoveDraggedCard(card, globalPos);
    }

    private void OnCardDragMoved(TodoCard card, System.Numerics.Vector2 globalPos)
    {
        if (_draggingCard != card) return;
        MoveDraggedCard(card, globalPos);
    }

    private void MoveDraggedCard(TodoCard card, System.Numerics.Vector2 globalPos)
    {
        float w = Math.Max(1f, card.Transform.Width);
        float h = Math.Max(1f, card.Transform.Height);
        card.Transform.SetAbsoluteFrame(
            globalPos.X - _dragGrabOffsetX,
            globalPos.Y - _dragGrabOffsetY,
            w,
            h);

        // Children use absolute frames in LayoutChildren — re-run so content tracks the shell
        card.ForceLayoutSubtree();
        card.MarkVisibilityClippingDirty();

        // Partial dirty-rects leave XP-style trails over sibling cards. While dragging,
        // repaint the whole view so every layer under the floating card is restored.
        FullRenderRequired = true;
        RenderRequired = true;
        try { Silk.NET.GLFW.GlfwProvider.GLFW.Value.PostEmptyEvent(); } catch { }
    }

    private void OnCardDragDropped(TodoCard card, System.Numerics.Vector2 dropPos)
    {
        card.ReleasePointer();
        card.ZIndex = 0;

        TodoColumn target = card.Item.Column;
        for (int i = 0; i < 3; i++)
        {
            if (_columnHitRects[i].Contains(dropPos.X, dropPos.Y))
            {
                target = (TodoColumn)i;
                break;
            }
        }

        card.Item.Column = target;
        card.Item.IsDone = target == TodoColumn.Done;

        // Detach drag visual before rebuild (must not leave orphan on board root)
        if (card.Parent == _boardRoot)
            _boardRoot.RemoveChild(card);

        _draggingCard = null;
        QueueRebuild();
    }

    public void RebuildBoard()
    {
        float[] savedScrollY = new float[3];
        for (int i = 0; i < 3; i++)
            savedScrollY[i] = _colScrollContainers[i]?.ScrollY ?? 0f;

        // Safety: clear any leftover drag card
        if (_draggingCard != null)
        {
            try
            {
                _draggingCard.ReleasePointer();
                if (_draggingCard.Parent == _boardRoot)
                    _boardRoot.RemoveChild(_draggingCard);
            }
            catch { /* ignore teardown races */ }
            _draggingCard = null;
        }

        for (int i = 0; i < 3; i++)
        {
            var col = (TodoColumn)i;
            var colItems = _items.Where(t => t.Column == col).ToList();
            _colBadgeElements[i].Text = colItems.Count.ToString();

            // Remove previous cards only (keep empty placeholder child)
            var scroll = _colScrollContainers[i];
            var toRemove = scroll.Children.Where(c => c is TodoCard).ToList();
            foreach (var c in toRemove)
                scroll.RemoveChild(c);

            _emptyPlaceholders[i].Visible = colItems.Count == 0;

            foreach (var item in colItems)
            {
                var card = new TodoCard(item);
                ApplyCardVisuals(card);
                card.StatusChanged += OnCardStatusChanged;
                card.EditRequested += OpenEditModal;
                card.DeleteRequested += OnCardDeleted;
                card.DragStarted += OnCardDragStarted;
                card.DragMoved += OnCardDragMoved;
                card.DragDropped += OnCardDragDropped;
                scroll.AddChild(card);
            }
        }

        _boardRoot.InvalidateLayout();
        ForceLayoutEvaluation();

        for (int i = 0; i < 3; i++)
            _colScrollContainers[i].ScrollY = savedScrollY[i];

        if (_currentPlugin is SampleBoardStatsPlugin stats) stats.UpdateStats();
        else if (_currentPlugin is SampleBoardMetricsPlugin metrics) metrics.UpdateStats();
    }

    private void OnCardStatusChanged(TodoItem todo)
    {
        if (todo.IsDone)
            todo.Column = TodoColumn.Done;
        else if (todo.Column == TodoColumn.Done)
            todo.Column = TodoColumn.Backlog;
        QueueRebuild();
    }

    private void OnCardDeleted(TodoItem todo)
    {
        _items.Remove(todo);
        QueueRebuild();
    }

    private void ApplyCardVisuals(TodoCard card)
    {
        card.Style.Border.Roundness = _cardRoundness;
        if (_showShadows)
        {
            card.Style.Shadow = new ShadowStyle
            {
                Color = SKColors.Black.WithAlpha(40),
                SpreadX = 0,
                SpreadY = 1,
                OffsetX = 0,
                OffsetY = 1
            };
        }
        else
        {
            card.Style.Shadow = null!;
        }

        if (_dimCompleted && card.Item.IsDone)
        {
            card.Style.BackColor = new SKColor(38, 38, 38);
            card.Opacity = 0.72f;
        }
        else
        {
            card.Opacity = 1f;
            if (!card.Item.IsDone)
                card.Style.BackColor = new SKColor(58, 58, 58);
        }

        card.UpdateVisualState();
        // Re-apply after UpdateVisualState (it resets some colors)
        if (_dimCompleted && card.Item.IsDone)
            card.Style.BackColor = new SKColor(38, 38, 38);
        if (!_showShadows)
            card.Style.Shadow = null!;
        card.Style.Border.Roundness = _cardRoundness;
    }

    internal void PerformBoardLayout(float originX, float originY, float viewW, float viewH)
    {
        if (viewW < 32 || viewH < 32) return;

        const float padX = 20f;
        const float padY = 16f;
        const float headerH = 64f;
        float colGap = _columnGap;
        const float colPad = 12f;
        const float colHeaderH = 36f;

        float headerX = originX + padX;
        float headerY = originY + padY;
        float headerW = Math.Max(200f, viewW - padX * 2);

        _headerContainer.Transform.SetAbsoluteFrame(headerX, headerY, headerW, headerH);
        _titleElement.Transform.SetAbsoluteFrame(headerX + 18f, headerY + 12f, 220f, 26f);
        _subtitleElement.Transform.SetAbsoluteFrame(headerX + 18f, headerY + 38f, 220f, 18f);

        float addW = 110f, addH = 36f;
        float addX = headerX + headerW - 16f - addW;
        float addY = headerY + (headerH - addH) / 2f;
        _addTaskBtn.Transform.SetAbsoluteFrame(addX, addY, addW, addH);

        float setW = 88f, setH = 36f;
        float setX = addX - 10f - setW;
        _settingsBtn.Transform.SetAbsoluteFrame(setX, addY, setW, setH);

        float desW = 104f, desH = 36f;
        float desX = setX - 10f - desW;
        _designerBtn.Transform.SetAbsoluteFrame(desX, addY, desW, desH);

        // Optional plugin strip under header (only when a plugin is attached from Designer)
        float pluginGap = 0f;
        float pluginH = 0f;
        if (_pluginSlot != null && _pluginSlot.Visible && _currentPlugin != null)
        {
            pluginGap = 10f;
            pluginH = Math.Max(80f, _pluginSlot.Transform.Height);
            _pluginSlot.Transform.SetAbsoluteFrame(headerX, headerY + headerH + pluginGap, headerW, pluginH);
            PluginEmbed.UpdateLayout(_currentPlugin, _pluginSlot, headerW, pluginH);
        }
        else if (_pluginSlot != null)
        {
            _pluginSlot.Transform.SetAbsoluteFrame(headerX, headerY + headerH, headerW, 0f);
        }

        float colTop = headerY + headerH + pluginGap + pluginH + 14f;
        float colH = Math.Max(120f, originY + viewH - padY - colTop);
        float colW = Math.Max(120f, (headerW - colGap * 2) / 3f);

        float cardH = _cardHeight;
        float cardGap = _cardGap;

        for (int i = 0; i < 3; i++)
        {
            float colX = headerX + i * (colW + colGap);
            _columnHitRects[i] = new SKRect(colX, colTop, colX + colW, colTop + colH);

            _colContainers[i].Transform.SetAbsoluteFrame(colX, colTop, colW, colH);

            float hx = colX + colPad;
            float hy = colTop + 10f;
            float hw = colW - colPad * 2;
            _colHeaderContainers[i].Transform.SetAbsoluteFrame(hx, hy, hw, colHeaderH);
            _colTitleElements[i].Transform.SetAbsoluteFrame(hx, hy + 6f, Math.Max(40f, hw - 40f), 24f);
            _colBadgeElements[i].Transform.SetAbsoluteFrame(hx + hw - 32f, hy + 6f, 28f, 22f);

            float scrollX = colX + 8f;
            float scrollY = colTop + 10f + colHeaderH + 4f;
            float scrollW = Math.Max(40f, colW - 16f);
            float scrollH = Math.Max(40f, colH - (10f + colHeaderH + 4f + 10f));
            _colScrollContainers[i].Transform.SetAbsoluteFrame(scrollX, scrollY, scrollW, scrollH);

            // Content layout in parent-local coords (scroll applies in Evaluate)
            float contentY = 6f;
            float cardW = Math.Max(40f, scrollW - 14f);

            if (_emptyPlaceholders[i].Visible)
            {
                _emptyPlaceholders[i].Transform.SetLocalFrame(4f, 24f, cardW, 28f);
            }

            int cardIndex = 0;
            foreach (var child in _colScrollContainers[i].Children)
            {
                if (child is not TodoCard card || card == _draggingCard) continue;
                card.Transform.SetLocalFrame(4f, contentY + cardIndex * (cardH + cardGap), cardW, cardH);
                card.ForceLayoutSubtree();
                cardIndex++;
            }
        }

        // Modal overlays full board
        _addModal.Transform.SetAbsoluteFrame(originX, originY, viewW, viewH);
        _editModal.Transform.SetAbsoluteFrame(originX, originY, viewW, viewH);
        _settingsModal.Transform.SetAbsoluteFrame(originX, originY, viewW, viewH);

        // Position modal content fields once content container has a frame (modal LayoutChildren runs after)
        LayoutModalFields(_addModal, _addTitleInput, null);
        LayoutModalFields(_editModal, _editTitleInput, _editDeleteBtn);
    }

    private static void LayoutModalFields(Modal modal, InputField input, Button? deleteBtn)
    {
        // Modal.LayoutChildren runs as part of the same tree pass after this returns only if
        // modal is layout-dirty after we set its frame. Force field placement relative to content box
        // in Modal via its own LayoutChildren; here ensure input size hints.
        float contentW = Math.Max(100f, modal.CardWidth - 48f);
        input.Transform.Width = contentW;
        input.Transform.Height = 38f;
        if (deleteBtn != null)
        {
            deleteBtn.Transform.Width = 100f;
            deleteBtn.Transform.Height = 34f;
        }
    }

    private class KanbanBoardRoot : Container
    {
        private readonly KanbanView _view;

        public KanbanBoardRoot(KanbanView view) : base(new SKColor(23, 23, 23), 0f)
        {
            _view = view;
            Style.BackColor = new SKColor(23, 23, 23);
            Style.Border.Width = 0;
            Style.Shadow = null!;
        }

        protected override void LayoutChildren()
        {
            _view.PerformBoardLayout(
                Transform.Computed.X,
                Transform.Computed.Y,
                Transform.Computed.Width,
                Transform.Computed.Height);
        }
    }
}
