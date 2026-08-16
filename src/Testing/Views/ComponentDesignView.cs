using System;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Testing.Components;
using SkiaSharp;

namespace Blossom.Testing.Views;

/// <summary>
/// Tooling-lite Component Design View providing an isolated authoring workspace with its own <see cref="DesignCanvas"/>.
/// Demonstrates designing wide components (e.g. 860 × 420 units), testing anchor reflow against simulated slot sizes,
/// and attaching authored plugins into the main Kanban host via API.
/// </summary>
public class ComponentDesignView : View
{
    private readonly KanbanView? _kanbanView;

    private Container _root = null!;
    private Container _headerContainer = null!;
    private VisualElement _titleElement = null!;
    private VisualElement _subtitleElement = null!;
    private Button _backBtn = null!;
    private Button _useInBoardBtn = null!;

    private Container _toolbarContainer = null!;
    private Button _slotSizeBtn = null!;
    private Button _pluginTypeBtn = null!;
    private Button _quickTaskBtn = null!;

    private Container _stageContainer = null!;
    private VisualElement _stageInfoLabel = null!;
    private Container _stageSlot = null!;

    // Authored plugins
    private SampleBoardMetricsPlugin _metricsPlugin = null!;
    private SampleBoardStatsPlugin _statsPlugin = null!;
    private PluginRoot _activePlugin = null!;

    private int _slotSizePresetIndex = 0; // 0: 860x420 (Native), 1: 640x260 (Medium), 2: 380x120 (Compact)
    private readonly (float width, float height, string label)[] _slotPresets = new[]
    {
        (860f, 420f, "Slot: 860 × 420 (Design Canvas)"),
        (640f, 260f, "Slot: 640 × 260 (Medium Reflow)"),
        (380f, 120f, "Slot: 380 × 120 (Compact Reflow)")
    };

    public ComponentDesignView(KanbanView? kanbanView = null) : base("Component Designer")
    {
        _kanbanView = kanbanView;
        BackColor = new SKColor(18, 20, 26);
        Canvas = new DesignCanvas(1000f, 620f);
    }

    public override void Init()
    {
        _root = new Container(new SKColor(18, 20, 26), 0f)
        {
            Name = "Designer_Root",
            Transform = new Transform(0, 0, Width, Height)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        _root.Style.Border.Width = 0;
        _root.Style.Shadow = null!;

        // 1. Header Bar (Anchor Left | Right | Top)
        _headerContainer = new Container(new SKColor(28, 31, 40), 8f)
        {
            Name = "Designer_Header",
            Transform = new Transform(16f, 12f, 968f, 54f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            }
        };
        _headerContainer.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(45, 50, 65),
            Roundness = 8f
        };

        _titleElement = new VisualElement
        {
            Name = "Designer_Title",
            Text = "BLOSSOM COMPONENT DESIGNER",
            IsClickthrough = true,
            Transform = new Transform(16f, 10f, 340f, 20f)
            {
                Anchor = Anchor.Left | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = SKColors.White, Size = 15f, Weight = 700 }
            }
        };

        _subtitleElement = new VisualElement
        {
            Name = "Designer_Subtitle",
            Text = "Isolation Workspace • Design Canvas: 860 × 420 Ru • Anchor reflow",
            IsClickthrough = true,
            Transform = new Transform(16f, 30f, 480f, 16f)
            {
                Anchor = Anchor.Left | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(140, 148, 165), Size = 11f, Weight = 500 }
            }
        };

        _useInBoardBtn = new Button("⚡ Use in Board (Embed)", new SKColor(16, 185, 129))
        {
            Name = "Designer_UseInBoardBtn",
            Transform = new Transform(640f, 10f, 180f, 34f)
            {
                Anchor = Anchor.Right | Anchor.Top
            }
        };
        _useInBoardBtn.Clicked += OnUseInBoardClicked;

        _backBtn = new Button("← Back to Kanban", new SKColor(55, 65, 81))
        {
            Name = "Designer_BackBtn",
            Transform = new Transform(830f, 10f, 126f, 34f)
            {
                Anchor = Anchor.Right | Anchor.Top
            }
        };
        _backBtn.Clicked += OnBackClicked;

        _headerContainer.AddChild(_titleElement);
        _headerContainer.AddChild(_subtitleElement);
        _headerContainer.AddChild(_useInBoardBtn);
        _headerContainer.AddChild(_backBtn);

        // 2. Toolbar / Controls (Anchor Left | Right | Top)
        _toolbarContainer = new Container(new SKColor(24, 26, 34), 6f)
        {
            Name = "Designer_Toolbar",
            Transform = new Transform(16f, 72f, 968f, 42f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            }
        };
        _toolbarContainer.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(40, 44, 58),
            Roundness = 6f
        };

        _slotSizeBtn = new Button(_slotPresets[0].label, new SKColor(45, 55, 72))
        {
            Name = "Designer_SlotSizeBtn",
            Transform = new Transform(10f, 6f, 250f, 30f)
            {
                Anchor = Anchor.Left | Anchor.Top
            }
        };
        _slotSizeBtn.Clicked += CycleSlotSizePreset;

        _pluginTypeBtn = new Button("Widget: Wide Metrics (860x420)", new SKColor(79, 70, 229))
        {
            Name = "Designer_PluginTypeBtn",
            Transform = new Transform(270f, 6f, 240f, 30f)
            {
                Anchor = Anchor.Left | Anchor.Top
            }
        };
        _pluginTypeBtn.Clicked += ToggleActivePluginType;

        _quickTaskBtn = new Button("+ Add Sample Task", new SKColor(55, 65, 81))
        {
            Name = "Designer_QuickTaskBtn",
            Transform = new Transform(520f, 6f, 150f, 30f)
            {
                Anchor = Anchor.Left | Anchor.Top
            }
        };
        _quickTaskBtn.Clicked += () =>
        {
            if (_kanbanView != null)
            {
                _kanbanView.AddSampleTaskFromPlugin();
            }
            RefreshPluginStats();
        };

        _toolbarContainer.AddChild(_slotSizeBtn);
        _toolbarContainer.AddChild(_pluginTypeBtn);
        _toolbarContainer.AddChild(_quickTaskBtn);

        // 3. Stage Container (Anchor Left | Right | Top | Bottom)
        _stageContainer = new Container(new SKColor(22, 24, 30), 8f)
        {
            Name = "Designer_StageContainer",
            Transform = new Transform(16f, 120f, 968f, 484f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        _stageContainer.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(38, 42, 54),
            Roundness = 8f
        };

        _stageInfoLabel = new VisualElement
        {
            Name = "Designer_StageInfo",
            Text = "STAGE CANVAS • Slot Size: 860 × 420 Ru • Click slot presets above to test anchor reflow live",
            IsClickthrough = true,
            Transform = new Transform(16f, 10f, 700f, 18f)
            {
                Anchor = Anchor.Left | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(120, 128, 145), Size = 11f, Weight = 500 }
            }
        };

        // Simulated Host Slot on the stage
        _stageSlot = new Container(SKColors.Transparent, 0f)
        {
            Name = "Designer_StageSlot",
            Transform = new Transform(16f, 36f, 860f, 420f)
            {
                Anchor = Anchor.Left | Anchor.Top
            }
        };
        _stageSlot.Style.BackColor = SKColors.Transparent;
        _stageSlot.Style.Border.Width = 0;
        _stageSlot.Style.Shadow = null!;

        // Instantiate authored plugins
        Func<(int total, int inProgress, int done)> statsProvider = () =>
            _kanbanView != null ? _kanbanView.GetBoardStats() : (12, 4, 6);

        Action taskAction = () =>
        {
            _kanbanView?.AddSampleTaskFromPlugin();
            RefreshPluginStats();
        };

        _metricsPlugin = new SampleBoardMetricsPlugin(statsProvider, taskAction);
        _statsPlugin = new SampleBoardStatsPlugin(statsProvider, taskAction);

        _activePlugin = _metricsPlugin;
        _stageSlot.Embed(_activePlugin);

        _stageContainer.AddChild(_stageInfoLabel);
        _stageContainer.AddChild(_stageSlot);

        _root.AddChild(_headerContainer);
        _root.AddChild(_toolbarContainer);
        _root.AddChild(_stageContainer);

        AddElement(_root);
        UpdateToolbarControls();
    }

    public override void OnActivated()
    {
        base.OnActivated();
        RefreshPluginStats();
    }

    private void RefreshPluginStats()
    {
        _metricsPlugin?.UpdateStats();
        _statsPlugin?.UpdateStats();
    }

    private void CycleSlotSizePreset()
    {
        _slotSizePresetIndex = (_slotSizePresetIndex + 1) % _slotPresets.Length;
        var preset = _slotPresets[_slotSizePresetIndex];

        _slotSizeBtn.Text = preset.label;
        _stageSlot.Transform.SetLocalFrame(16f, 36f, preset.width, preset.height);
        _stageSlot.ForceLayoutSubtree();

        _stageInfoLabel.Text = $"STAGE CANVAS • {preset.label} • Anchor reflow";
        PluginEmbed.UpdateLayout(_activePlugin, _stageSlot, preset.width, preset.height);
        ForceLayoutEvaluation();
    }

    private void ToggleActivePluginType()
    {
        PluginEmbed.Detach(_activePlugin);

        if (_activePlugin == _metricsPlugin)
        {
            _activePlugin = _statsPlugin;
            _pluginTypeBtn.Text = "Widget: Board Analytics (380x120)";
        }
        else
        {
            _activePlugin = _metricsPlugin;
            _pluginTypeBtn.Text = "Widget: Wide Metrics (860x420)";
        }

        // Components always reflow via anchors into the host slot.
        _stageSlot.Embed(_activePlugin);
        UpdateToolbarControls();
        PluginEmbed.UpdateLayout(_activePlugin, _stageSlot);
        ForceLayoutEvaluation();
    }

    private void UpdateToolbarControls()
    {
        _stageInfoLabel.Text = $"STAGE CANVAS • {_slotPresets[_slotSizePresetIndex].label} • Anchor reflow";
    }

    private void OnUseInBoardClicked()
    {
        if (_kanbanView != null)
        {
            // API-first registration/attachment into host Kanban
            _kanbanView.AttachPlugin(_activePlugin);
            Application?.SetActiveView(_kanbanView);
        }
        else if (Application != null)
        {
            Application.SetActiveView("Blossom Tasks");
        }
    }

    private void OnBackClicked()
    {
        if (_kanbanView != null)
        {
            Application?.SetActiveView(_kanbanView);
        }
        else if (Application != null)
        {
            Application.SetActiveView("Blossom Tasks");
        }
    }
}
