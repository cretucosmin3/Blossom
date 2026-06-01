using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Core.Input;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views
{
    public class DashboardView : View
    {
        public Action? OnSwitchView;
        public Action? OnSwitchToNeon;
        public Action? OnSwitchToPaint;
        public Action? OnSwitchToKanban;

        public DashboardView() : base("Web Dashboard")
        {
            // Set the main background color to Slate 900
            BackColor = new SKColor(15, 23, 42);
        }

        public override void Init()
        {
            float sidebarWidth = 260f;

            // --- 1. SIDEBAR ---
            var sidebar = new VisualElement
            {
                Name = "Sidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59), // Slate 800
                    Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(60), SpreadX = 6, SpreadY = 0 }
                },
                Transform = new Transform(0, 0, sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false
                }
            };
            AddElement(sidebar);

            // Brand Label
            var brand = new VisualElement
            {
                Name = "Sidebar_Brand",
                Text = "⚡ BLOSSOM OS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 26, Weight = 800, Alignment = TextAlign.Center, Padding = 25 }
                },
                Transform = new Transform(0, 0, sidebarWidth, 80)
            };
            sidebar.AddChild(brand);

            // Navigation menu items
            string[] menuItems = { "Overview", "Neon Showcase", "Neon Paint", "Task Board" };
            float menuY = 100f;
            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                var btn = new Button(item, i == 0 ? new SKColor(255, 255, 255, 15) : SKColors.Transparent)
                {
                    Transform = { X = 20, Y = menuY, Width = sidebarWidth - 40, Height = 45 }
                };
                btn.Style.Text.Alignment = TextAlign.Left;
                btn.Style.Text.Padding = 20;
                btn.Style.Border.Roundness = 8;

                int idx = i;
                btn.OnClick = () =>
                {
                    if (idx == 1) OnSwitchToNeon?.Invoke();
                    else if (idx == 2) OnSwitchToPaint?.Invoke();
                    else if (idx == 3) OnSwitchToKanban?.Invoke();
                };

                sidebar.AddChild(btn);
                menuY += 55f;
            }

            // A Special Sidebar Switch View Button
            var switchBtn = new Button("Switch to Neon UI ➜", new SKColor(79, 70, 229))
            {
                Name = "Sidebar_SwitchBtn",
                Transform = { X = 20, Y = menuY + 20, Width = sidebarWidth - 40, Height = 45 }
            };
            switchBtn.Style.Text.Color = SKColors.White;
            switchBtn.Style.Border.Roundness = 8;
            switchBtn.OnClick = () => OnSwitchToNeon?.Invoke();
            sidebar.AddChild(switchBtn);


            // --- 2. MAIN CONTENT AREA ---
            var mainContent = new VisualElement
            {
                Name = "MainContent",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(sidebarWidth, 0, Width - sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right | Anchor.Bottom
                }
            };
            AddElement(mainContent);

            // Header Banner
            var header = new VisualElement
            {
                Name = "Header",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, 0, Width - sidebarWidth, 80) 
                { 
                    FixedHeight = true, 
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right 
                }
            };
            mainContent.AddChild(header);

            header.AddChild(new VisualElement
            {
                Name = "Header_Welcome",
                Text = "Creative Dashboard",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 24, Weight = 700, Padding = 25 } },
                Transform = new Transform(0, 0, 400, 80)
            });

            header.AddChild(new VisualElement
            {
                Name = "Header_Date",
                Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy"),
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.Gray, Size = 14, Alignment = TextAlign.Right, Padding = 25 } },
                Transform = new Transform(0, 0, Width - sidebarWidth, 80) 
                { 
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right 
                }
            });

            // --- 2.5 SCROLLABLE CONTENT VIEWPORT ---
            var scrollArea = new ScrollContainer
            {
                Name = "DashboardScrollArea",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, 80, Width - sidebarWidth, Height - 80)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            mainContent.AddChild(scrollArea);


            // --- 3. HERO CALL-TO-ACTION CARD ---
            var hero = new VisualElement
            {
                Name = "Hero",
                Style = new ElementStyle 
                { 
                    BackColor = new SKColor(79, 70, 229), // Indigo 600
                    Border = new BorderStyle { Roundness = 16 },
                    Shadow = new ShadowStyle { Color = new SKColor(79, 70, 229).WithAlpha(100), SpreadY = 10, OffsetY = 5 }
                },
                Transform = new Transform(30, 10, Width - sidebarWidth - 60, 160)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            scrollArea.AddChild(hero);

            hero.AddChild(new VisualElement
            {
                Name = "Hero_Title",
                Text = "Unlock premium dashboard rendering.",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 26, Weight = 700, Padding = 30 } },
                Transform = new Transform(0, 15, 600, 50)
            });

            hero.AddChild(new VisualElement
            {
                Name = "Hero_Subtitle",
                Text = "Experience up to 120 FPS hardware accelerated Silk.NET visual elements.",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(224, 231, 255), Size = 15, Padding = 30 } },
                Transform = new Transform(0, 55, 700, 40)
            });

            var ctaBtn = new Button("Launch Neon Mode", SKColors.White)
            {
                Name = "Hero_Btn",
                Transform = { X = 30, Y = 105, Width = 170, Height = 38 },
                OnClick = () => OnSwitchView?.Invoke()
            };
            ctaBtn.Style.Text.Color = new SKColor(79, 70, 229);
            ctaBtn.Style.Border.Roundness = 8;
            hero.AddChild(ctaBtn);


            // --- 4. METRICS / STATS GRID ---
            float cardY = 190f;
            float totalAvailableWidth = Width - sidebarWidth - 80f;
            float cardWidth = totalAvailableWidth / 3f;
            float cardGap = 10f;

            var revenueCard = new DashboardCard("Total Revenue", "$74,250", new SKColor(16, 185, 129)) 
            { 
                Name = "Card_Revenue", 
                Transform = { X = 30, Y = cardY, Width = cardWidth, Anchor = Anchor.Top } 
            };
            scrollArea.AddChild(revenueCard);

            var usersCard = new DashboardCard("Active Users", "12,482", new SKColor(56, 189, 248)) 
            { 
                Name = "Card_Users", 
                Transform = { X = 30 + cardWidth + cardGap, Y = cardY, Width = cardWidth, Anchor = Anchor.Top } 
            };
            scrollArea.AddChild(usersCard);

            var loadCard = new DashboardCard("Frame Rate", "144 FPS", new SKColor(244, 63, 94)) 
            { 
                Name = "Card_Load", 
                Transform = { X = 30 + (cardWidth + cardGap) * 2, Y = cardY, Width = cardWidth, Anchor = Anchor.Top } 
            };
            scrollArea.AddChild(loadCard);


            // --- 5. DATA CHARTS & PROGRESS ---
            float contentBottomY = 340f;
            float goalsWidth = 260f;

            // Simple Traffic Bar Chart
            var data = new float[] { 120, 180, 95, 230, 150, 270, 190 };
            var chart = new SimpleBarChart("Weekly Sales Traffic", data, new SKColor(139, 92, 246))
            {
                Name = "MainChart",
                Transform = new Transform(30, contentBottomY, Width - sidebarWidth - goalsWidth - 100f, 320f)
                {
                    Anchor = Anchor.Left | Anchor.Right | Anchor.Top
                }
            };
            scrollArea.AddChild(chart);

            // Goals Progress Panel
            var goalsPanel = new VisualElement
            {
                Name = "GoalsPanel",
                Style = new ElementStyle 
                { 
                    BackColor = new SKColor(30, 41, 59), 
                    Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 15) }
                },
                Transform = new Transform(Width - sidebarWidth - goalsWidth - 40f, contentBottomY, goalsWidth, 320f)
                {
                    Anchor = Anchor.Right | Anchor.Top
                }
            };
            scrollArea.AddChild(goalsPanel);

            goalsPanel.AddChild(new VisualElement
            {
                Name = "Goals_Title",
                Text = "Key Performance Goals",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 16, Weight = 600, Padding = 20 } },
                Transform = new Transform(0, 0, goalsWidth, 40) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Sales Goal Progress
            goalsPanel.AddChild(new VisualElement 
            { 
                Name = "G1_Label", 
                Text = "Sales Targets (85%)", 
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.LightGray, Size = 12, Padding = 20 } }, 
                Transform = new Transform(0, 40, goalsWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            goalsPanel.AddChild(new ProgressBar(0.85f, new SKColor(16, 185, 129))
            {
                Name = "Goal1_Bar",
                Transform = new Transform(20, 65, goalsWidth - 40, 8) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Signup Goal Progress
            goalsPanel.AddChild(new VisualElement 
            { 
                Name = "G2_Label", 
                Text = "New Signups (58%)", 
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.LightGray, Size = 12, Padding = 20 } }, 
                Transform = new Transform(0, 95, goalsWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            goalsPanel.AddChild(new ProgressBar(0.58f, new SKColor(56, 189, 248))
            {
                Name = "Goal2_Bar",
                Transform = new Transform(20, 120, goalsWidth - 40, 8) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Server Goal Progress
            goalsPanel.AddChild(new VisualElement 
            { 
                Name = "G3_Label", 
                Text = "Render Cache Hit Rate (92%)", 
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.LightGray, Size = 12, Padding = 20 } }, 
                Transform = new Transform(0, 150, goalsWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            goalsPanel.AddChild(new ProgressBar(0.92f, new SKColor(244, 63, 94))
            {
                Name = "Goal3_Bar",
                Transform = new Transform(20, 175, goalsWidth - 40, 8) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Bottom Action
            var exportBtn = new Button("Export Analytics", new SKColor(71, 85, 105))
            {
                Name = "Goal_ExportBtn",
                Transform = new Transform(20, 230, goalsWidth - 40, 40) { Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right }
            };
            goalsPanel.AddChild(exportBtn);
        }
    }
}
