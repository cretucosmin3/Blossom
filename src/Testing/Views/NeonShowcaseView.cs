using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views
{
    public class NeonShowcaseView : View
    {
        public Action? OnSwitchView;
        public Action? OnSwitchToPaint;
        public Action? OnSwitchToKanban;

        public NeonShowcaseView() : base("Neon Showcase")
        {
            // Dark Cyberpunk Background
            BackColor = SKColors.Black;
        }

        public override void Init()
        {
            // Title Header
            var title = new VisualElement
            {
                Name = "NeonTitle",
                Text = "NEON SHOWCASE",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(255, 0, 128),
                        Size = 38,
                        Weight = 800,
                        Alignment = TextAlign.Center,
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor(255, 0, 128, 180),
                            OffsetX = 0,
                            OffsetY = 0,
                            SpreadX = 15,
                            SpreadY = 15
                        }
                    }
                },
                Transform = new Transform(0, 50, Width, 80)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(title);

            // Subtitle Description
            var subtitle = new VisualElement
            {
                Name = "NeonSubtitle",
                Text = "Hover over the buttons below to trigger high-speed trim-path and glow flicker animations.",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(0, 240, 255, 180),
                        Size = 16,
                        Weight = 400,
                        Alignment = TextAlign.Center,
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor(0, 240, 255, 100),
                            OffsetX = 0,
                            OffsetY = 0,
                            SpreadX = 8,
                            SpreadY = 8
                        }
                    }
                },
                Transform = new Transform(0, 130, Width, 40)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(subtitle);


            // Grid/Rows of Neon Buttons
            float startY = 220f;
            float btnWidth = 240f;
            float btnHeight = 60f;
            float gapX = 40f;
            float gapY = 40f;

            // Row 1: Magenta & Cyan
            var btnPink = new NeonButton("NEON PINK", new SKColor(255, 0, 110), btnWidth, btnHeight)
            {
                Transform = { X = (Width / 2f) - btnWidth - (gapX / 2f), Y = startY, Anchor = Anchor.Top }
            };
            AddElement(btnPink);

            var btnCyan = new NeonButton("CYBER BLUE", new SKColor(0, 240, 255), btnWidth, btnHeight)
            {
                Transform = { X = (Width / 2f) + (gapX / 2f), Y = startY, Anchor = Anchor.Top }
            };
            AddElement(btnCyan);

            // Row 2: Lime Green & Orange Amber
            var btnGreen = new NeonButton("LIME GLOW", new SKColor(57, 255, 20), btnWidth, btnHeight)
            {
                Transform = { X = (Width / 2f) - btnWidth - (gapX / 2f), Y = startY + btnHeight + gapY, Anchor = Anchor.Top }
            };
            AddElement(btnGreen);

            var btnAmber = new NeonButton("AMBER LIGHT", new SKColor(255, 170, 0), btnWidth, btnHeight)
            {
                Transform = { X = (Width / 2f) + (gapX / 2f), Y = startY + btnHeight + gapY, Anchor = Anchor.Top }
            };
            AddElement(btnAmber);


            // Switch back to Dashboard, Paint Canvas, or Task Board
            float backBtnWidth = 240f;
            float backBtnHeight = 50f;
            float centerGap = 20f;
            float totalNavWidth = (3f * backBtnWidth) + (2f * centerGap);
            float startNavX = (Width / 2f) - (totalNavWidth / 2f);

            var backBtn = new NeonButton("➜ DASHBOARD", new SKColor(139, 92, 246), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX, Y = startY + (btnHeight + gapY) * 2 + 30, Anchor = Anchor.Top },
                OnClick = () => OnSwitchView?.Invoke()
            };
            AddElement(backBtn);

            var paintBtn = new NeonButton("➜ NEON PAINT", new SKColor(255, 0, 110), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX + backBtnWidth + centerGap, Y = startY + (btnHeight + gapY) * 2 + 30, Anchor = Anchor.Top },
                OnClick = () => OnSwitchToPaint?.Invoke()
            };
            AddElement(paintBtn);

            var kanbanBtn = new NeonButton("➜ TASK BOARD", new SKColor(0, 240, 255), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX + (backBtnWidth + centerGap) * 2f, Y = startY + (btnHeight + gapY) * 2 + 30, Anchor = Anchor.Top },
                OnClick = () => OnSwitchToKanban?.Invoke()
            };
            AddElement(kanbanBtn);
        }
    }
}
