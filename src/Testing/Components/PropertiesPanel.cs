using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class PropertiesPanel : VisualElement
{
    private VisualElement? _inspectedElement;
    private readonly VisualElement _title;
    private readonly VisualElement _typeLabel;
    private readonly VisualElement _contentContainer;

    public PropertiesPanel()
    {
        Name = "PropertiesPanel";

        Style = new ElementStyle
        {
            BackColor = new SKColor(16, 20, 30, 240), // Sleek glassmorphic panel
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
            Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(120), SpreadX = 8, SpreadY = 0, OffsetX = -2 }
        };

        Transform = new Transform(0, 0, 300, 800)
        {
            Anchor = Anchor.Top | Anchor.Bottom | Anchor.Right,
            FixedWidth = true,
            FixedHeight = false
        };

        // Title Header
        _title = new VisualElement
        {
            Name = "InspectorTitle",
            Text = "⚡ INSPECTOR",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(236, 72, 153), Size = 16, Weight = 800, Alignment = TextAlign.Left, Padding = 15 }
            },
            Transform = new Transform(0, 10, 300, 35)
        };
        AddChild(_title);

        _typeLabel = new VisualElement
        {
            Name = "InspectorTypeLabel",
            Text = "Select an element to inspect",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 11, Weight = 600, Alignment = TextAlign.Left, Padding = 15 }
            },
            Transform = new Transform(0, 40, 300, 25)
        };
        AddChild(_typeLabel);

        // Scrollable content container for properties
        _contentContainer = new VisualElement
        {
            Name = "InspectorContent",
            Style = new ElementStyle
            {
                BackColor = SKColors.Transparent,
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
            },
            Transform = new Transform(0, 70, 300, 730)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
            }
        };
        AddChild(_contentContainer);
    }

    public void InspectElement(VisualElement? element)
    {
        // Call child.Dispose() to prevent memory leaks and clean up from ElementsMap
        var currentChildren = _contentContainer.Children.ToArray();
        foreach (var child in currentChildren)
        {
            _contentContainer.RemoveChild(child);
            child.Dispose();
        }

        _inspectedElement = element;

        if (element == null)
        {
            _typeLabel.Text = "Select an element to inspect";
            return;
        }

        _typeLabel.Text = $"Selected: {element.Name} ({element.GetType().Name})";

        // Discover properties
        var properties = new List<PropertyMeta>();

        // Gather from Element itself
        GatherProperties(element, element, properties);
        
        // Gather from Transform
        GatherProperties(element.Transform, element, properties);

        // Gather from Style
        if (element.Style != null)
        {
            GatherProperties(element.Style, element, properties);
            if (element.Style.Text != null) GatherProperties(element.Style.Text, element, properties);
            if (element.Style.Border != null) GatherProperties(element.Style.Border, element, properties);
        }

        // Layout them out
        float currentY = 10f;
        float itemHeight = 45f;
        float paddingX = 15f;
        float inputWidth = 270f;

        // Group by category
        var groups = properties.GroupBy(p => p.Attr.Category).OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            // Category Header
            var header = new VisualElement
            {
                Name = $"Header_{group.Key}_{Guid.NewGuid().ToString().Substring(0, 4)}",
                Text = group.Key.ToUpper(),
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 10, Weight = 800, Alignment = TextAlign.Left }
                },
                Transform = new Transform(paddingX, currentY, inputWidth, 20)
            };
            _contentContainer.AddChild(header);
            currentY += 20f;

            foreach (var meta in group)
            {
                // Property label
                var propLabel = new VisualElement
                {
                    Name = $"Label_{meta.Prop.Name}_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Text = meta.Attr.Label,
                    Style = new ElementStyle
                    {
                        Text = new TextStyle { Color = new SKColor(226, 232, 240), Size = 11, Weight = 600, Alignment = TextAlign.Left }
                    },
                    Transform = new Transform(paddingX, currentY, inputWidth, 18)
                };
                _contentContainer.AddChild(propLabel);
                currentY += 18f;

                // Value editor control
                var propType = meta.Prop.PropertyType;

                if (propType == typeof(float) || propType == typeof(int))
                {
                    float min = meta.Attr.Min;
                    float max = meta.Attr.Max;
                    if (max <= min) { min = 0f; max = 1000f; } // Defaults

                    float val = Convert.ToSingle(meta.Prop.GetValue(meta.Target));

                    var slider = new Slider(min, max, val)
                    {
                        Transform = new Transform(paddingX, currentY, inputWidth, 16)
                    };

                    slider.OnValueChanged += (newValue) =>
                    {
                        meta.Prop.SetValue(meta.Target, Convert.ChangeType(newValue, propType));
                        meta.SourceElement.ScheduleRender();
                    };

                    _contentContainer.AddChild(slider);
                    currentY += 25f;
                }
                else if (propType == typeof(bool))
                {
                    bool val = (bool)meta.Prop.GetValue(meta.Target)!;

                    var toggle = new Switch("", val)
                    {
                        Transform = new Transform(paddingX, currentY, inputWidth, 24)
                    };

                    toggle.OnToggled += (newValue) =>
                    {
                        meta.Prop.SetValue(meta.Target, newValue);
                        meta.SourceElement.ScheduleRender();
                    };

                    _contentContainer.AddChild(toggle);
                    currentY += 30f;
                }
                else if (propType == typeof(string))
                {
                    string val = (string)meta.Prop.GetValue(meta.Target)! ?? "";

                    var input = new InputField("Enter text...", val)
                    {
                        Transform = new Transform(paddingX, currentY, inputWidth, 24)
                    };

                    input.OnValueChanged += (newValue) =>
                    {
                        meta.Prop.SetValue(meta.Target, newValue);
                        meta.SourceElement.ScheduleRender();
                    };

                    _contentContainer.AddChild(input);
                    currentY += 30f;
                }
                else if (propType == typeof(SKColor))
                {
                    SKColor val = (SKColor)meta.Prop.GetValue(meta.Target)!;

                    // Row of R, G, B sliders
                    float sliderW = (inputWidth - 10f) / 3f;

                    var rSlider = new Slider(0f, 255f, val.Red) { Transform = new Transform(paddingX, currentY, sliderW, 16) };
                    var gSlider = new Slider(0f, 255f, val.Green) { Transform = new Transform(paddingX + sliderW + 5f, currentY, sliderW, 16) };
                    var bSlider = new Slider(0f, 255f, val.Blue) { Transform = new Transform(paddingX + 2f * (sliderW + 5f), currentY, sliderW, 16) };

                    Action updateColor = () =>
                    {
                        SKColor newColor = new SKColor((byte)rSlider.Value, (byte)gSlider.Value, (byte)bSlider.Value, 255);
                        meta.Prop.SetValue(meta.Target, newColor);
                        meta.SourceElement.ScheduleRender();
                    };

                    rSlider.OnValueChanged += (v) => updateColor();
                    gSlider.OnValueChanged += (v) => updateColor();
                    bSlider.OnValueChanged += (v) => updateColor();

                    _contentContainer.AddChild(rSlider);
                    _contentContainer.AddChild(gSlider);
                    _contentContainer.AddChild(bSlider);
                    currentY += 25f;
                }
                else
                {
                    // Fallback empty spacer
                    currentY += 5f;
                }

                currentY += 10f; // Gap between properties
            }

            currentY += 15f; // Gap between categories
        }
    }

    private void GatherProperties(object target, VisualElement source, List<PropertyMeta> list)
    {
        var props = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<BuilderPropertyAttribute>();
            if (attr != null)
            {
                list.Add(new PropertyMeta
                {
                    Target = target,
                    SourceElement = source,
                    Prop = prop,
                    Attr = attr
                });
            }
        }
    }

    private class PropertyMeta
    {
        public object Target { get; set; } = null!;
        public VisualElement SourceElement { get; set; } = null!;
        public PropertyInfo Prop { get; set; } = null!;
        public BuilderPropertyAttribute Attr { get; set; } = null!;
    }
}
