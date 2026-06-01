using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class DashboardCard : VisualElement
    {
        private readonly SKColor StandardColor = new SKColor(255, 255, 255, 10); // Glass effect
        private readonly SKColor HoverColor = new SKColor(255, 255, 255, 20);

        public DashboardCard(string title, string value, SKColor accentColor)
        {
            // IMPORTANT: The parent (DashboardView) expects to set the Name property via object initializer.
            // However, the constructor runs BEFORE the object initializer.
            // So 'Name' will be the default (null or empty) here if we don't set it.
            // But we can't know the unique ID the user wants until after.
            // So we must rely on the user passing a unique ID or use a UUID.
            // BUT, the error log says "A component with name Title already exists".
            // This implies the previous run successfully created one, but the second one failed.
            // To be safe, we will use a GUID if Name is not already set unique enough, 
            // OR we simply append a static counter.
            // BETTER: Use the Title to generate a likely unique base, but really we should
            // do this lazily or require an ID in constructor. 
            // Let's stick to using the title but with a clearer prefix, 
            // AND update child names *after* the Name might be set? 
            // No, 'AddChild' puts it in the map immediately?
            // Actually 'AddChild' calls 'Tree.AddElement'.
            
            // To support "new DashboardCard { Name = 'Unique' }", we have a problem:
            // The constructor runs, AddChild runs *with default names*, THEN Name is updated.
            // Updating Name doesn't update children.
            
            // FIX: We will construct children but NOT add them in the constructor if possible?
            // No, standard pattern.
            // Let's use a static counter to ensure uniqueness internally.
            
            Name = $"Card_{title}_{Guid.NewGuid().ToString().Substring(0, 4)}"; 

            Style = new ElementStyle
            {
                BackColor = StandardColor,
                Border = new BorderStyle { Roundness = 12, Width = 1, Color = new SKColor(255, 255, 255, 30) },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(40), SpreadX = 0, SpreadY = 5, OffsetX = 0, OffsetY = 5 }
            };
            
            Transform = new Transform(0, 0, 300, 140)
            {
                FixedWidth = false,
                FixedHeight = true
            };
 
            // Title
            AddChild(new VisualElement
            {
                Name = $"{Name}_Title",
                Text = title,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.LightGray, Size = 14, Weight = 400, Padding = 20 }
                },
                Transform = new Transform(0, 0, 300, 40) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
 
            // Value
            AddChild(new VisualElement
            {
                Name = $"{Name}_Value",
                Text = value,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 32, Weight = 700, Padding = 20 }
                },
                Transform = new Transform(0, 40, 300, 50) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            
            // Accent Line
            AddChild(new VisualElement
            {
                Name = $"{Name}_Accent",
                Style = new ElementStyle { BackColor = accentColor, Border = new BorderStyle { Roundness = 10 } },
                Transform = new Transform(20, 110, 40, 4) { FixedWidth = true, FixedHeight = true, Anchor = Anchor.Bottom | Anchor.Left }
            });

            // Hover Effect
            Events.OnMouseEnter += (s) => Style.BackColor = HoverColor;
            Events.OnMouseLeave += (s) => Style.BackColor = StandardColor;
        }
    }
}
