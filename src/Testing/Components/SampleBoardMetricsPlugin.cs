using System;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Wide component plugin authored in isolation against a custom <see cref="DesignCanvas"/> (860 × 420 units).
/// Demonstrates Strategy S2 anchor reflow, KPI cards, anchored flow distribution bars, and live host communication.
/// </summary>
public class SampleBoardMetricsPlugin : PluginRoot
{
    private readonly Func<(int total, int inProgress, int done)>? _statsProvider;
    private readonly Action? _quickTaskAction;

    // Fallback counts for isolation mode
    private int _mockTotal = 12;
    private int _mockInProgress = 4;
    private int _mockDone = 6;

    private readonly Container _cardBackground;
    private readonly VisualElement _titleLabel;
    private readonly VisualElement _subtitleLabel;
    private readonly VisualElement _badgeLabel;

    // KPI panel elements
    private readonly Container _kpiContainer;
    private readonly VisualElement _kpiTitle;
    private readonly VisualElement _kpiTotalValue;
    private readonly VisualElement _kpiTotalLabel;
    private readonly VisualElement _kpiActiveValue;
    private readonly VisualElement _kpiActiveLabel;
    private readonly VisualElement _kpiDoneValue;
    private readonly VisualElement _kpiDoneLabel;
    private readonly VisualElement _kpiRateValue;
    private readonly VisualElement _kpiRateLabel;

    // Flow tracks panel elements
    private readonly Container _flowContainer;
    private readonly VisualElement _flowTitle;
    private readonly Container _backlogTrackBg;
    private readonly Container _backlogTrackBar;
    private readonly VisualElement _backlogLabel;
    private readonly Container _progressTrackBg;
    private readonly Container _progressTrackBar;
    private readonly VisualElement _progressLabel;
    private readonly Container _doneTrackBg;
    private readonly Container _doneTrackBar;
    private readonly VisualElement _doneLabel;
    private readonly VisualElement _flowFooterLabel;

    // Footer actions
    private readonly Button _quickTaskBtn;
    private readonly VisualElement _footerStatusLabel;

    public SampleBoardMetricsPlugin(
        Func<(int total, int inProgress, int done)>? statsProvider = null,
        Action? quickTaskAction = null)
        : base(860f, 420f)
    {
        PluginId = "blossom.samples.board-metrics";
        PluginName = "Wide Board Flow & Metrics";
        Version = new Version(1, 0, 0);

        _statsProvider = statsProvider;
        _quickTaskAction = quickTaskAction;

        Name = "SampleBoardMetricsPlugin_Root";

        // Root background container (anchored to fill the full plugin frame)
        _cardBackground = new Container(new SKColor(24, 26, 32), 10f)
        {
            Name = "MetricsPlugin_Background",
            Transform = new Transform(0, 0, 860f, 420f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        _cardBackground.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(50, 56, 70),
            Roundness = 10f
        };

        // Header Title (Anchor Left | Top)
        _titleLabel = new VisualElement
        {
            Name = "MetricsPlugin_Title",
            Text = "PLUGIN: Wide Flow & Velocity Analytics",
            IsClickthrough = true,
            Transform = new Transform(16f, 12f, 450f, 22f)
            {
                Anchor = Anchor.Left | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(240, 240, 245),
                    Size = 14f,
                    Weight = 700
                }
            }
        };

        // Subtitle / canvas metadata (Anchor Left | Top)
        _subtitleLabel = new VisualElement
        {
            Name = "MetricsPlugin_Subtitle",
            Text = "Authored at 860 × 420 Ru • Strategy S2 Synthetic Reflow",
            IsClickthrough = true,
            Transform = new Transform(16f, 34f, 450f, 18f)
            {
                Anchor = Anchor.Left | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(140, 148, 165),
                    Size = 11f,
                    Weight = 500
                }
            }
        };

        // Status badge (Anchor Right | Top — reflows with slot)
        _badgeLabel = new VisualElement
        {
            Name = "MetricsPlugin_Badge",
            Text = "ANCHORED",
            IsClickthrough = true,
            Transform = new Transform(720f, 14f, 124f, 24f)
            {
                Anchor = Anchor.Right | Anchor.Top
            },
            Style = new ElementStyle
            {
                BackColor = new SKColor(16, 80, 48),
                Border = new BorderStyle
                {
                    Width = 1,
                    Color = new SKColor(34, 197, 94),
                    Roundness = 4f
                },
                Text = new TextStyle
                {
                    Color = new SKColor(187, 247, 208),
                    Size = 10f,
                    Weight = 700,
                    Alignment = TextAlign.Center
                }
            }
        };

        // Left KPI Panel (Transform: 16, 60, 230, 284; Anchor: Left | Top | Bottom)
        _kpiContainer = new Container(new SKColor(30, 33, 42), 8f)
        {
            Name = "MetricsPlugin_KpiPanel",
            Transform = new Transform(16f, 60f, 230f, 284f)
            {
                Anchor = Anchor.Left | Anchor.Top | Anchor.Bottom
            }
        };
        _kpiContainer.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(45, 50, 64),
            Roundness = 8f
        };

        _kpiTitle = new VisualElement
        {
            Name = "Kpi_Title",
            Text = "KEY INDICATORS",
            IsClickthrough = true,
            Transform = new Transform(12f, 10f, 200f, 18f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(160, 168, 185), Size = 10f, Weight = 700 }
            }
        };

        _kpiTotalValue = new VisualElement
        {
            Name = "Kpi_TotalVal",
            Text = "12",
            IsClickthrough = true,
            Transform = new Transform(12f, 32f, 96f, 36f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = SKColors.White, Size = 24f, Weight = 800 }
            }
        };
        _kpiTotalLabel = new VisualElement
        {
            Name = "Kpi_TotalLbl",
            Text = "Total Items",
            IsClickthrough = true,
            Transform = new Transform(12f, 70f, 96f, 16f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(130, 138, 155), Size = 11f, Weight = 500 }
            }
        };

        _kpiActiveValue = new VisualElement
        {
            Name = "Kpi_ActiveVal",
            Text = "4",
            IsClickthrough = true,
            Transform = new Transform(120f, 32f, 96f, 36f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(96, 165, 250), Size = 24f, Weight = 800 }
            }
        };
        _kpiActiveLabel = new VisualElement
        {
            Name = "Kpi_ActiveLbl",
            Text = "In Progress",
            IsClickthrough = true,
            Transform = new Transform(120f, 70f, 96f, 16f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(130, 138, 155), Size = 11f, Weight = 500 }
            }
        };

        _kpiDoneValue = new VisualElement
        {
            Name = "Kpi_DoneVal",
            Text = "6",
            IsClickthrough = true,
            Transform = new Transform(12f, 100f, 96f, 36f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(74, 222, 128), Size = 24f, Weight = 800 }
            }
        };
        _kpiDoneLabel = new VisualElement
        {
            Name = "Kpi_DoneLbl",
            Text = "Completed",
            IsClickthrough = true,
            Transform = new Transform(12f, 138f, 96f, 16f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(130, 138, 155), Size = 11f, Weight = 500 }
            }
        };

        _kpiRateValue = new VisualElement
        {
            Name = "Kpi_RateVal",
            Text = "50%",
            IsClickthrough = true,
            Transform = new Transform(120f, 100f, 96f, 36f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(244, 114, 182), Size = 24f, Weight = 800 }
            }
        };
        _kpiRateLabel = new VisualElement
        {
            Name = "Kpi_RateLbl",
            Text = "Completion %",
            IsClickthrough = true,
            Transform = new Transform(120f, 138f, 96f, 16f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(130, 138, 155), Size = 11f, Weight = 500 }
            }
        };

        _kpiContainer.AddChild(_kpiTitle);
        _kpiContainer.AddChild(_kpiTotalValue);
        _kpiContainer.AddChild(_kpiTotalLabel);
        _kpiContainer.AddChild(_kpiActiveValue);
        _kpiContainer.AddChild(_kpiActiveLabel);
        _kpiContainer.AddChild(_kpiDoneValue);
        _kpiContainer.AddChild(_kpiDoneLabel);
        _kpiContainer.AddChild(_kpiRateValue);
        _kpiContainer.AddChild(_kpiRateLabel);

        // Right Flow Panel (Transform: 258, 60, 586, 284; Anchor: Left | Right | Top | Bottom)
        _flowContainer = new Container(new SKColor(30, 33, 42), 8f)
        {
            Name = "MetricsPlugin_FlowPanel",
            Transform = new Transform(258f, 60f, 586f, 284f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        _flowContainer.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(45, 50, 64),
            Roundness = 8f
        };

        _flowTitle = new VisualElement
        {
            Name = "Flow_Title",
            Text = "COLUMN DISTRIBUTION & FLOW CAPACITY",
            IsClickthrough = true,
            Transform = new Transform(14f, 10f, 350f, 18f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(160, 168, 185), Size = 10f, Weight = 700 }
            }
        };

        // Track 1: Backlog
        _backlogLabel = new VisualElement
        {
            Name = "Flow_BacklogLbl",
            Text = "Backlog (2 items • 17%)",
            IsClickthrough = true,
            Transform = new Transform(14f, 36f, 400f, 18f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(200, 205, 215), Size = 11f, Weight = 600 }
            }
        };
        _backlogTrackBg = new Container(new SKColor(20, 22, 28), 4f)
        {
            Name = "Flow_BacklogBg",
            Transform = new Transform(14f, 58f, 558f, 16f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            }
        };
        _backlogTrackBar = new Container(new SKColor(148, 163, 184), 4f)
        {
            Name = "Flow_BacklogBar",
            Transform = new Transform(0, 0, 95f, 16f)
        };
        _backlogTrackBg.AddChild(_backlogTrackBar);

        // Track 2: In Progress
        _progressLabel = new VisualElement
        {
            Name = "Flow_ProgressLbl",
            Text = "In Progress (4 items • 33%)",
            IsClickthrough = true,
            Transform = new Transform(14f, 86f, 400f, 18f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(147, 197, 253), Size = 11f, Weight = 600 }
            }
        };
        _progressTrackBg = new Container(new SKColor(20, 22, 28), 4f)
        {
            Name = "Flow_ProgressBg",
            Transform = new Transform(14f, 108f, 558f, 16f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            }
        };
        _progressTrackBar = new Container(new SKColor(59, 130, 246), 4f)
        {
            Name = "Flow_ProgressBar",
            Transform = new Transform(0, 0, 184f, 16f)
        };
        _progressTrackBg.AddChild(_progressTrackBar);

        // Track 3: Done
        _doneLabel = new VisualElement
        {
            Name = "Flow_DoneLbl",
            Text = "Done (6 items • 50%)",
            IsClickthrough = true,
            Transform = new Transform(14f, 136f, 400f, 18f) { Anchor = Anchor.Left | Anchor.Top },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(134, 239, 172), Size = 11f, Weight = 600 }
            }
        };
        _doneTrackBg = new Container(new SKColor(20, 22, 28), 4f)
        {
            Name = "Flow_DoneBg",
            Transform = new Transform(14f, 158f, 558f, 16f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            }
        };
        _doneTrackBar = new Container(new SKColor(34, 197, 94), 4f)
        {
            Name = "Flow_DoneBar",
            Transform = new Transform(0, 0, 279f, 16f)
        };
        _doneTrackBg.AddChild(_doneTrackBar);

        _flowFooterLabel = new VisualElement
        {
            Name = "Flow_FooterLbl",
            Text = "Strategy S2 anchor reflow dynamically adapts to host slot width.",
            IsClickthrough = true,
            Transform = new Transform(14f, 240f, 558f, 20f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Bottom
            },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(110, 118, 135), Size = 11f, Weight = 400 }
            }
        };

        _flowContainer.AddChild(_flowTitle);
        _flowContainer.AddChild(_backlogLabel);
        _flowContainer.AddChild(_backlogTrackBg);
        _flowContainer.AddChild(_progressLabel);
        _flowContainer.AddChild(_progressTrackBg);
        _flowContainer.AddChild(_doneLabel);
        _flowContainer.AddChild(_doneTrackBg);
        _flowContainer.AddChild(_flowFooterLabel);

        // Footer Actions (Anchor Left | Right | Bottom)
        _quickTaskBtn = new Button("+ Quick Task", new SKColor(79, 70, 229))
        {
            Name = "MetricsPlugin_QuickTaskBtn",
            Transform = new Transform(704f, 356f, 140f, 36f)
            {
                Anchor = Anchor.Right | Anchor.Bottom
            }
        };
        _quickTaskBtn.Clicked += () =>
        {
            if (_quickTaskAction != null)
            {
                _quickTaskAction.Invoke();
            }
            else
            {
                _mockTotal++;
                _mockInProgress++;
            }
            UpdateStats();
        };

        _footerStatusLabel = new VisualElement
        {
            Name = "MetricsPlugin_StatusLbl",
            Text = "Live Host Bridge Connected • Design Space: 860 × 420",
            IsClickthrough = true,
            Transform = new Transform(170f, 364f, 520f, 20f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Bottom
            },
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(130, 140, 160), Size = 11f, Weight = 500, Alignment = TextAlign.Center }
            }
        };

        AddChild(_cardBackground);
        _cardBackground.AddChild(_titleLabel);
        _cardBackground.AddChild(_subtitleLabel);
        _cardBackground.AddChild(_badgeLabel);
        _cardBackground.AddChild(_kpiContainer);
        _cardBackground.AddChild(_flowContainer);
        _cardBackground.AddChild(_footerStatusLabel);
        _cardBackground.AddChild(_quickTaskBtn);

        UpdateStats();
    }

    /// <summary>
    /// Recalculates metrics and flow distribution bars from live host data or fallback mock data.
    /// </summary>
    public void UpdateStats()
    {
        int total, inProgress, done;
        if (_statsProvider != null)
        {
            (total, inProgress, done) = _statsProvider();
            _footerStatusLabel.Text = "Live Host Bridge Connected • Kanban Board Host";
        }
        else
        {
            total = _mockTotal;
            inProgress = _mockInProgress;
            done = _mockDone;
            _footerStatusLabel.Text = "Isolation Mode Active • Mock Workspace Data";
        }

        int backlog = Math.Max(0, total - (inProgress + done));
        float completionRate = total > 0 ? (float)done / total * 100f : 0f;

        _kpiTotalValue.Text = total.ToString();
        _kpiActiveValue.Text = inProgress.ToString();
        _kpiDoneValue.Text = done.ToString();
        _kpiRateValue.Text = $"{(int)completionRate}%";

        float trackW = _flowContainer.Transform.Computed.Width > 0 
            ? Math.Max(100f, _flowContainer.Transform.Computed.Width - 28f)
            : 558f;

        float bPct = total > 0 ? (float)backlog / total : 0.2f;
        float pPct = total > 0 ? (float)inProgress / total : 0.3f;
        float dPct = total > 0 ? (float)done / total : 0.5f;

        _backlogLabel.Text = $"Backlog ({backlog} items • {(int)(bPct * 100)}%)";
        _backlogTrackBar.Transform.Width = Math.Max(6f, trackW * bPct);

        _progressLabel.Text = $"In Progress ({inProgress} items • {(int)(pPct * 100)}%)";
        _progressTrackBar.Transform.Width = Math.Max(6f, trackW * pPct);

        _doneLabel.Text = $"Done ({done} items • {(int)(dPct * 100)}%)";
        _doneTrackBar.Transform.Width = Math.Max(6f, trackW * dPct);

        InvalidatePaint();
    }
}
