using System;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Sample embeddable plugin demonstrating <see cref="PluginRoot"/> with an independent <see cref="DesignCanvas"/> (380 × 120 units),
/// Strategy S2 synthetic resize reflow, and interactive hit-testing inside a host slot.
/// </summary>
public class SampleBoardStatsPlugin : PluginRoot
{
    private readonly Func<(int total, int inProgress, int done)>? _statsProvider;
    private readonly Action? _quickTaskAction;

    private readonly Container _cardBackground;
    private readonly VisualElement _titleLabel;
    private readonly VisualElement _badgeLabel;
    private readonly VisualElement _statsSummaryLabel;
    private readonly Button _actionBtn;

    public SampleBoardStatsPlugin(
        Func<(int total, int inProgress, int done)>? statsProvider = null,
        Action? quickTaskAction = null)
        : base(380f, 120f)
    {
        PluginId = "blossom.samples.board-stats";
        PluginName = "Board Analytics Plugin";
        Version = new Version(1, 0, 0);

        _statsProvider = statsProvider;
        _quickTaskAction = quickTaskAction;

        Name = "SampleBoardStatsPlugin_Root";

        // Background container anchored to fill the entire plugin canvas
        _cardBackground = new Container(new SKColor(28, 28, 28), 8f)
        {
            Name = "Plugin_Background",
            Transform = new Transform(0, 0, 380f, 120f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
            }
        };
        _cardBackground.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(60, 60, 60),
            Roundness = 8f
        };

        // Header Title
        _titleLabel = new VisualElement
        {
            Name = "Plugin_Title",
            Text = "PLUGIN: Board Analytics",
            IsClickthrough = true,
            Transform = new Transform(12f, 10f, 220f, 22f)
            {
                Anchor = Anchor.Left | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(220, 220, 220),
                    Size = 12f,
                    Weight = 700
                }
            }
        };

        // Status badge (anchored to slot top-right; reflows with host size)
        _badgeLabel = new VisualElement
        {
            Name = "Plugin_Badge",
            Text = "ANCHORED",
            IsClickthrough = true,
            Transform = new Transform(270f, 10f, 98f, 20f)
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

        // Stats summary label
        _statsSummaryLabel = new VisualElement
        {
            Name = "Plugin_StatsSummary",
            Text = "Total: 0 | In Progress: 0 | Done: 0",
            IsClickthrough = true,
            Transform = new Transform(12f, 42f, 356f, 26f)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            },
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(180, 180, 180),
                    Size = 13f,
                    Weight = 500
                }
            }
        };

        // Action button proving hit-testing and event bubbling inside plugin
        _actionBtn = new Button("+ Quick Task", new SKColor(55, 65, 81))
        {
            Name = "Plugin_ActionBtn",
            Transform = new Transform(240f, 76f, 128f, 32f)
            {
                Anchor = Anchor.Right | Anchor.Bottom
            }
        };
        _actionBtn.Clicked += () =>
        {
            _quickTaskAction?.Invoke();
            UpdateStats();
        };

        AddChild(_cardBackground);
        _cardBackground.AddChild(_titleLabel);
        _cardBackground.AddChild(_badgeLabel);
        _cardBackground.AddChild(_statsSummaryLabel);
        _cardBackground.AddChild(_actionBtn);

        UpdateStats();
    }

    /// <summary>
    /// Refreshes live statistics from the connected provider.
    /// </summary>
    public void UpdateStats()
    {
        if (_statsProvider != null)
        {
            var (total, inProgress, done) = _statsProvider();
            _statsSummaryLabel.Text = $"Tasks: {total}   |   Active: {inProgress}   |   Done: {done}";
        }
        else
        {
            _statsSummaryLabel.Text = "No stats provider connected.";
        }
        InvalidatePaint();
    }
}
