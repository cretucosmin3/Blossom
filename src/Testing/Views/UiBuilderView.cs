using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core;
using Blossom.Core.Delegates.Common;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Silk.NET.Input;
using SkiaSharp;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views;

public class UiBuilderView : View
{
    private ViewsSidebar _sidebar = null!;
    private PropertiesPanel _inspector = null!;
    private VisualElement _workspaceContainer = null!;
    private BuilderCanvas _canvas = null!;
    
    private readonly List<VisualElement> _artboards = new();
    private readonly List<string> _viewNames = new();
    private int _currentViewIndex = 0;

    private VisualElement? _selectedElement;
    private VisualElement _selectionOutline = null!;

    private ComponentPaletteModal _paletteModal = null!;

    // Drag states
    private bool _isDraggingElement;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartElementPos;

    public UiBuilderView() : base("Blossom UI Builder")
    {
        BackColor = new SKColor(11, 14, 22); // Consistent midnight slate background
    }

    public override void Init()
    {
        float leftWidth = 260f;
        float rightWidth = 300f;
        float workspaceWidth = Width - (leftWidth + rightWidth);

        // 1. LEFT SIDEBAR (Views & Screens)
        _sidebar = new ViewsSidebar
        {
            Transform = new Transform(0, 0, leftWidth, Height)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                FixedWidth = true,
                FixedHeight = false
            }
        };
        AddElement(_sidebar);

        // 2. RIGHT INSPECTOR (Property Editors)
        _inspector = new PropertiesPanel
        {
            Transform = new Transform(Width - rightWidth, 0, rightWidth, Height)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Right,
                FixedWidth = true,
                FixedHeight = false
            }
        };
        AddElement(_inspector);

        // 3. WORKSPACE CONTAINER (Viewport clipping)
        _workspaceContainer = new VisualElement
        {
            Name = "WorkspaceContainer",
            IsClipping = true, // Force viewport bounds clipping
            Style = new ElementStyle
            {
                BackColor = new SKColor(18, 22, 28), // Dark slate editor viewport
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
            },
            Transform = new Transform(leftWidth, 0, workspaceWidth, Height)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right,
                FixedWidth = false,
                FixedHeight = false
            }
        };
        AddElement(_workspaceContainer);

        // 4. INFINITE GRID CANVAS
        _canvas = new BuilderCanvas();
        _workspaceContainer.AddChild(_canvas);

        // 5. SELECTION OUTLINE OVERLAY
        _selectionOutline = new VisualElement
        {
            Name = "SelectionOutline",
            IsClickthrough = true,
            ZIndex = 9999,
            Style = new ElementStyle
            {
                BackColor = SKColors.Transparent,
                Border = new BorderStyle
                {
                    Width = 2f,
                    Color = new SKColor(56, 189, 248), // Cyberpunk Cyan highlight
                    Roundness = 4f
                },
                BorderEffect = BorderEffectType.MarchingAnts, // Blossom-native vector effects!
                BorderEffectSpeed = 3f,
                BorderEffectAmount = 8f
            },
            Transform = new Transform(0, 0, 0, 0)
        };
        _selectionOutline.Visible = false;
        _canvas.AddChild(_selectionOutline);

        // 6. COMPONENT PALETTE TAB MODAL
        _paletteModal = new ComponentPaletteModal();
        _paletteModal.Visible = false;
        
        // Center modal in workspace container
        _paletteModal.Transform.X = leftWidth + (workspaceWidth - _paletteModal.Transform.Width) / 2f;
        _paletteModal.Transform.Y = (Height - _paletteModal.Transform.Height) / 2f;
        _paletteModal.Transform.Anchor = Anchor.None;
        _paletteModal.Transform.FixedWidth = true;
        _paletteModal.Transform.FixedHeight = true;
        AddElement(_paletteModal);

        // Seed default designed views
        SeedDefaultViews();

        // Register view select hooks
        _sidebar.OnAddViewRequested += AddNewView;
        _sidebar.OnViewSelected += SwitchView;
        _sidebar.UpdateViewsList(_viewNames, _currentViewIndex);

        // Select the active view artboard by default
        SwitchView(0);

        // Spawner callback
        _paletteModal.OnComponentSelected += SpawnComponent;

        // View Mouse Drag & Select Listeners
        Events.OnMouseDown += (s, e) =>
        {
            // Ignore if clicked on sidebar or inspector panel
            if (e.Global.X < leftWidth || e.Global.X > Width - rightWidth)
                return;

            if (_paletteModal.Visible)
            {
                // Close palette if clicked outside modal
                if (e.Global.X < _paletteModal.Transform.X || e.Global.X > _paletteModal.Transform.X + _paletteModal.Transform.Width ||
                    e.Global.Y < _paletteModal.Transform.Y || e.Global.Y > _paletteModal.Transform.Y + _paletteModal.Transform.Height)
                {
                    HidePalette();
                }
                return;
            }

            var clicked = Elements.FirstFromPoint(e.Global.X, e.Global.Y);
            if (clicked != null)
            {
                // Check if click belongs to active artboard
                var activeArtboard = _artboards[_currentViewIndex];
                bool isDescendant = false;
                var parent = clicked;
                while (parent != null)
                {
                    if (parent == activeArtboard)
                    {
                        isDescendant = true;
                        break;
                    }
                    parent = parent.Parent;
                }

                if (isDescendant && clicked != activeArtboard)
                {
                    // Find top-level element directly under active artboard
                    var topElement = clicked;
                    while (topElement.Parent != null && topElement.Parent != activeArtboard)
                    {
                        topElement = topElement.Parent;
                    }

                    SelectElement(topElement);

                    // Initialize element dragging
                    if (e.Button == 0) // Left click dragging
                    {
                        _isDraggingElement = true;
                        _dragStartMouse = e.Global;
                        _dragStartElementPos = new Vector2(topElement.Transform.X, topElement.Transform.Y);
                    }
                }
                else if (clicked == _canvas || clicked == activeArtboard)
                {
                    SelectElement(null); // Clicked background/canvas
                }
            }
        };

        Events.OnMouseMove += (s, e) =>
        {
            if (_isDraggingElement && _selectedElement != null)
            {
                Vector2 deltaMouse = e.Global - _dragStartMouse;
                
                // Compenses for zoom scale so translation drag speed is uniform
                Vector2 localDelta = deltaMouse / _canvas.Zoom;

                _selectedElement.Transform.X = _dragStartElementPos.X + localDelta.X;
                _selectedElement.Transform.Y = _dragStartElementPos.Y + localDelta.Y;

                UpdateSelectionOutline();
            }
        };

        Events.OnMouseUp += (s, e) =>
        {
            if (_isDraggingElement && _selectedElement != null)
            {
                _inspector.InspectElement(_selectedElement); // Refresh inspector once drag ends
            }
            _isDraggingElement = false;
        };

        // Hook Tab key to toggle modal overlay
        Events.OnKeyUp += (key) =>
        {
            Key inputKey = (Key)key;
            if (inputKey == Key.Tab)
            {
                TogglePalette();
            }
            else if (inputKey == Key.Escape)
            {
                HidePalette();
            }
        };
    }

    private void SeedDefaultViews()
    {
        // View 1: Main Dashboard
        var artboard1 = CreateArtboard("Dashboard Screen", 1100f, 1200f);
        
        // Add title card
        var card = new VisualElement
        {
            Name = "SystemCard",
            Style = new ElementStyle
            {
                BackColor = new SKColor(30, 41, 59, 180), // transparent slate
                Border = new BorderStyle { Width = 1, Color = new SKColor(71, 85, 105), Roundness = 12f }
            },
            Transform = new Transform(40, 50, 720, 180)
        };
        
        var cardTitle = new VisualElement
        {
            Name = "CardTitle",
            Text = "✨ PERFORMANCE METRICS",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 16, Weight = 800, Alignment = TextAlign.Left, Padding = 15 }
            },
            Transform = new Transform(0, 0, 720, 45)
        };
        card.AddChild(cardTitle);

        var classicBtn = new Components.Button("RELOAD CONFIG", new SKColor(236, 72, 153))
        {
            Transform = new Transform(40, 70, 180, 40)
        };
        card.AddChild(classicBtn);

        var pBar = new ProgressBar(0.72f, new SKColor(56, 189, 248))
        {
            Transform = new Transform(40, 130, 640, 20)
        };
        card.AddChild(pBar);

        artboard1.AddChild(card);

        // Add checkbox option
        var chk = new Checkbox("Enable System Diagnostics", true)
        {
            Transform = new Transform(40, 260, 250, 30)
        };
        artboard1.AddChild(chk);

        // Add slider range
        var slideLabel = new VisualElement
        {
            Name = "SlideLabel",
            Text = "Diagnostics Speed",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 12, Weight = 600, Alignment = TextAlign.Left }
            },
            Transform = new Transform(40, 310, 200, 20)
        };
        artboard1.AddChild(slideLabel);

        var slide = new Slider(0f, 100f, 65f)
        {
            Transform = new Transform(40, 335, 300, 30)
        };
        artboard1.AddChild(slide);

        // View 2: Settings View
        var artboard2 = CreateArtboard("System Settings", 1100f, 1200f);

        var titleLabel = new VisualElement
        {
            Name = "SettingsTitle",
            Text = "🛠️ CORE SETTINGS OPTIONS",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = SKColors.White, Size = 18, Weight = 800, Alignment = TextAlign.Left }
            },
            Transform = new Transform(50, 50, 700, 40)
        };
        artboard2.AddChild(titleLabel);

        var sw1 = new Switch("High-Fidelity Cyberpunk Shaders", true)
        {
            Transform = new Transform(50, 120, 300, 30)
        };
        artboard2.AddChild(sw1);

        var sw2 = new Switch("Background SVGs & Textures", false)
        {
            Transform = new Transform(50, 170, 300, 30)
        };
        artboard2.AddChild(sw2);

        var inputField = new InputField("Enter view name...", "System Configuration")
        {
            Transform = new Transform(50, 240, 300, 36)
        };
        artboard2.AddChild(inputField);

        _artboards.Add(artboard1);
        _viewNames.Add("Dashboard Screen");

        _artboards.Add(artboard2);
        _viewNames.Add("Settings Screen");
    }

    private VisualElement CreateArtboard(string name, float x, float y)
    {
        var artboard = new VisualElement
        {
            Name = name.Replace(" ", ""),
            Style = new ElementStyle
            {
                BackColor = new SKColor(16, 20, 30), // Artboard background
                Border = new BorderStyle { Width = 1.5f, Color = new SKColor(56, 189, 248, 120), Roundness = 8f },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(100), SpreadX = 10, SpreadY = 10 }
            },
            Transform = new Transform(x, y, 800, 600)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };

        // Artboard header label
        var label = new VisualElement
        {
            Name = $"{artboard.Name}_HeaderLabel",
            Text = $"ARTBOARD: {name.ToUpper()}",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 11, Weight = 800 }
            },
            Transform = new Transform(0, -25, 300, 20)
        };
        artboard.AddChild(label);

        return artboard;
    }

    private void AddNewView()
    {
        string name = $"Screen { _viewNames.Count + 1}";
        var artboard = CreateArtboard(name, 1100f, 1200f);
        
        _artboards.Add(artboard);
        _viewNames.Add(name);

        _sidebar.UpdateViewsList(_viewNames, _currentViewIndex);
        SwitchView(_viewNames.Count - 1);
    }

    private void SwitchView(int index)
    {
        if (index < 0 || index >= _artboards.Count) return;

        // Clear canvas designed view child
        var activeArtboard = _artboards[_currentViewIndex];
        _canvas.RemoveChild(activeArtboard);

        _currentViewIndex = index;
        activeArtboard = _artboards[_currentViewIndex];
        
        // Add as child to grid canvas
        _canvas.AddChild(activeArtboard);

        // Center canvas on the active artboard layout space
        _canvas.PanOffset = new Vector2(
            260f + (Width - 560f - 800f) / 2f,
            (Height - 600f) / 2f
        );
        _canvas.Zoom = 1.0f;

        SelectElement(null);
        _sidebar.UpdateViewsList(_viewNames, _currentViewIndex);
    }

    private void SelectElement(VisualElement? element)
    {
        _selectedElement = element;
        _inspector.InspectElement(_selectedElement);

        UpdateSelectionOutline();
    }

    private void UpdateSelectionOutline()
    {
        if (_selectedElement == null)
        {
            _selectionOutline.Visible = false;
            return;
        }

        // Coordinates relative to canvas
        _selectionOutline.Transform.X = _selectedElement.Transform.X - _canvas.Transform.X;
        _selectionOutline.Transform.Y = _selectedElement.Transform.Y - _canvas.Transform.Y;
        _selectionOutline.Transform.Width = _selectedElement.Transform.Width;
        _selectionOutline.Transform.Height = _selectedElement.Transform.Height;

        _selectionOutline.Visible = true;
    }

    private void SpawnComponent(string typeName)
    {
        HidePalette();

        var activeArtboard = _artboards[_currentViewIndex];
        VisualElement spawned = null!;

        float centerX = (activeArtboard.Transform.Width - 150f) / 2f;
        float centerY = (activeArtboard.Transform.Height - 40f) / 2f;

        switch (typeName)
        {
            case "Container Box":
                spawned = new VisualElement
                {
                    Name = $"Box_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Style = new ElementStyle
                    {
                        BackColor = new SKColor(30, 41, 59),
                        Border = new BorderStyle { Width = 1, Color = new SKColor(71, 85, 105), Roundness = 8f }
                    },
                    Transform = new Transform(centerX, centerY, 150, 100)
                };
                break;
            case "Label Text":
                spawned = new VisualElement
                {
                    Name = $"Label_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Text = "Label Text",
                    Style = new ElementStyle
                    {
                        Text = new TextStyle { Color = SKColors.White, Size = 14, Weight = 600, Alignment = TextAlign.Left }
                    },
                    Transform = new Transform(centerX, centerY, 150, 24)
                };
                break;
            case "Classic Button":
                spawned = new Components.Button("BUTTON", new SKColor(59, 130, 246))
                {
                    Transform = new Transform(centerX, centerY, 130, 36)
                };
                break;
            case "Neon Matrix Button":
                spawned = new NeonButton("NEON ACTIVATE", new SKColor(236, 72, 153))
                {
                    Transform = new Transform(centerX, centerY, 180, 40)
                };
                break;
            case "Progress Bar":
                spawned = new ProgressBar(0.5f, new SKColor(34, 197, 94))
                {
                    Transform = new Transform(centerX, centerY, 200, 20)
                };
                break;
            case "Slider Range":
                spawned = new Slider(0f, 100f, 50f)
                {
                    Transform = new Transform(centerX, centerY, 200, 24)
                };
                break;
            case "Checkbox State":
                spawned = new Checkbox("New Checkbox Option")
                {
                    Transform = new Transform(centerX, centerY, 180, 24)
                };
                break;
            case "Switch Toggle":
                spawned = new Switch("New Slide Switch")
                {
                    Transform = new Transform(centerX, centerY, 180, 24)
                };
                break;
        }

        if (spawned != null)
        {
            // Apply Halftone reveal transition fade on startup!
            spawned.Style.TransitionType = TransitionEffectType.HalftoneDots;
            spawned.Style.TransitionProgress = 0f;

            activeArtboard.AddChild(spawned);
            SelectElement(spawned);

            // Animate transition reveal progress over time
            float progress = 0f;
            ForVoid? animate = null;
            animate = () =>
            {
                float dt = Blossom.Core.Visual.SKSLShaderTimeTracker.DeltaTime;
                progress = Math.Min(1f, progress + dt * 2.5f); // 400ms transition
                spawned.Style.TransitionProgress = progress;
                if (progress >= 1f)
                {
                    spawned.Style.TransitionType = TransitionEffectType.None; // remove shader
                    Loop -= animate!; // deregister loop
                }
            };
            Loop += animate;
        }
    }

    private void TogglePalette()
    {
        if (_paletteModal.Visible)
            HidePalette();
        else
            ShowPalette();
    }

    private void ShowPalette()
    {
        _paletteModal.Visible = true;
        SelectElement(null); // Deselect
        _paletteModal.GetFocus();
        RenderRequired = true;
    }

    private void HidePalette()
    {
        _paletteModal.Visible = false;
        FocusedElement = null!;
        RenderRequired = true;
    }
}
