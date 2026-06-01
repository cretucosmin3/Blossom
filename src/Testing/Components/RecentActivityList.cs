using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class RecentActivityList : VisualElement
    {
        public RecentActivityList()
        {
            Name = "RecentActivity";
            Style = new ElementStyle
            {
                BackColor = SKColors.White,
                 Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(10), SpreadX = 5, SpreadY = 5, OffsetX = 0, OffsetY = 2 },
                 Border = new BorderStyle { Roundness = 10, Width = 1, Color = SKColors.LightGray.WithAlpha(50) }
            };
            Transform = new Transform(0, 0, 300, 400); // Default, resize by parent

            var title = new VisualElement
            {
                Name = "Title",
                Text = "Recent Activity",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Size = 16, Color = SKColors.Gray, Padding = 20, Alignment = TextAlign.TopLeft, Weight = 700 },
                    BackColor = SKColors.Transparent,
                    Border = new BorderStyle { Width = 1, Color = SKColors.LightGray.WithAlpha(30), RoundnessTopLeft=10, RoundnessTopRight=10 } // Bottom separator
                },
                Transform = new Transform { X = 0, Y = 0, Height = 50, Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            AddChild(title);

            AddActivityItem("New Order #1024", "2 mins ago", 0);
            AddActivityItem("Server Rebooted", "15 mins ago", 1);
            AddActivityItem("New User Registered", "1 hr ago", 2);
            AddActivityItem("Database Backup", "3 hrs ago", 3);
            AddActivityItem("Payment Failed", "5 hrs ago", 4);
        }

        private void AddActivityItem(string text, string time, int index)
        {
            float itemHeight = 60;
            float startY = 60;

            var item = new VisualElement
            {
                Name = $"Activity_{index}",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, startY + (index * itemHeight), 300, itemHeight)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };

            var mainText = new VisualElement
            {
                Name = $"Activity_{index}_Text",
                Text = text,
                Style = new ElementStyle { Text = new TextStyle { Size = 14, Color = SKColors.Black, Padding = 15, Alignment=TextAlign.TopLeft } },
                Transform = new Transform { Height=30, Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            item.AddChild(mainText);

            var timeText = new VisualElement
            {
                Name = $"Activity_{index}_Time",
                Text = time,
                Style = new ElementStyle { Text = new TextStyle { Size = 12, Color = SKColors.Gray, Padding = 15, Alignment = TextAlign.BottomLeft } },
                Transform = new Transform { Y=30, Height = 30, Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            item.AddChild(timeText);
            
            // Separator
             var sep = new VisualElement
            {
                Name = $"Activity_{index}_Separator",
                Style = new ElementStyle { BackColor = SKColors.LightGray.WithAlpha(50) },
                Transform = new Transform { Y = 59, Height = 1, Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right }
            };
            item.AddChild(sep);


            AddChild(item);
        }
    }
}
