using System;
using System.Threading;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class NeonButton : VisualElement
    {
        private Thread? _animationThread;
        private readonly SKColor _accentColor;
        private readonly float _normalShadowSpread;
        private readonly float _hoverShadowSpread;
        private int _animationSession = 0;
        private readonly string _targetText;

        public Action? OnClick;

        public NeonButton(string text, SKColor accentColor, float width = 220, float height = 55)
        {
            Name = $"NeonButton_{text}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            Text = text;
            _targetText = text;
            _accentColor = accentColor;
            IsClipping = false;

            _normalShadowSpread = 8f;
            _hoverShadowSpread = 25f;

            // Initialize Style
            Style = new ElementStyle
            {
                BackColor = new SKColor(10, 10, 15, 255), // Very dark background
                Text = new TextStyle
                {
                    Color = new SKColor(accentColor.Red, accentColor.Green, accentColor.Blue, 100), // Start dimmed
                    Size = 24,
                    Weight = 700,
                    Alignment = TextAlign.Center,
                    Shadow = new ShadowStyle
                    {
                        Color = new SKColor(accentColor.Red, accentColor.Green, accentColor.Blue, 150),
                        OffsetX = 0,
                        OffsetY = 2,
                        SpreadX = 5,
                        SpreadY = 5
                    }
                },
                Border = new BorderStyle
                {
                    Color = new SKColor(accentColor.Red, accentColor.Green, accentColor.Blue, 80), // Dimmed border
                    Width = 3,
                    Roundness = 6,
                },
                Shadow = new ShadowStyle
                {
                    Color = new SKColor(accentColor.Red, accentColor.Green, accentColor.Blue, 80),
                    OffsetX = 0,
                    OffsetY = 0,
                    SpreadX = _normalShadowSpread,
                    SpreadY = _normalShadowSpread,
                }
            };

            Transform = new Transform(0, 0, width, height);

            // Register Mouse Events
            Events.OnMouseEnter += (s) =>
            {
                StartHoverInAnimation();
            };

            Events.OnMouseLeave += (s) =>
            {
                StartHoverOutAnimation();
            };

            Events.OnMouseUp += (s, e) =>
            {
                OnClick?.Invoke();
            };
        }

        private string GetHackedText(string original, float progress)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$#@%&?*-=+[]";
            var sb = new System.Text.StringBuilder(original.Length);
            int revealCount = (int)(original.Length * progress);

            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] == ' ')
                {
                    sb.Append(' ');
                }
                else if (i < revealCount)
                {
                    sb.Append(original[i]);
                }
                else
                {
                    sb.Append(chars[Random.Shared.Next(chars.Length)]);
                }
            }
            return sb.ToString();
        }

        private void StartHoverInAnimation()
        {
            int session = Interlocked.Increment(ref _animationSession);

            _animationThread = new Thread(() =>
            {
                Style.Border.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 150);

                Action flashBorder = () =>
                {
                    var randomStart = Random.Shared.NextSingle();
                    var randomEnd = Random.Shared.NextSingle();
                    
                    // Apply trim effect to border
                    Style.Border.PathEffect?.Dispose();
                    Style.Border.PathEffect = SKPathEffect.CreateTrim(randomStart, randomEnd);

                    Style.Shadow.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, (byte)(255 * randomStart));
                    Style.Text.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, (byte)(255 * randomEnd));

                    if (randomStart < 0.1f) randomStart = 0.1f;
                    if (randomEnd < 0.1f) randomEnd = 0.1f;

                    Style.Shadow.SpreadX = _hoverShadowSpread * randomStart;
                    Style.Shadow.SpreadY = _hoverShadowSpread * randomEnd;

                    Style.Text.Shadow.SpreadX = 15 * randomStart;
                    Style.Text.Shadow.SpreadY = 15 * randomEnd;
                };

                int steps = 20;
                for (int step = 0; step <= steps; step++)
                {
                    if (session != _animationSession) return;

                    float progress = (float)step / steps;

                    Style.Text.PathEffect?.Dispose();
                    Style.Text.PathEffect = SKPathEffect.CreateTrim(0f, progress);

                    Text = GetHackedText(_targetText, progress);

                    byte alpha = (byte)(255 * progress < 180 ? 180 : 255 * progress);
                    Style.Text.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, alpha);

                    // Flickering border effect
                    if (Random.Shared.NextSingle() > 0.5f)
                        flashBorder();

                    Thread.Sleep(12);
                }

                if (session != _animationSession) return;

                // Final Hover State
                Text = _targetText;
                Style.Border.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 255);
                Style.Shadow.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 255);
                Style.Text.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 255);

                Style.Shadow.SpreadX = _hoverShadowSpread;
                Style.Shadow.SpreadY = _hoverShadowSpread;

                Style.Text.Shadow.SpreadX = 15;
                Style.Text.Shadow.SpreadY = 15;

                Style.Border.PathEffect?.Dispose();
                Style.Border.PathEffect = null;
                Style.Text.PathEffect?.Dispose();
                Style.Text.PathEffect = null;
            });
            _animationThread.Start();
        }

        private void StartHoverOutAnimation()
        {
            int session = Interlocked.Increment(ref _animationSession);

            _animationThread = new Thread(() =>
            {
                Text = _targetText;
                Style.Border.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 80);
                Style.Shadow.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 80);

                int steps = 15;
                for (int step = steps; step >= 0; step--)
                {
                    if (session != _animationSession) return;

                    float progress = (float)step / steps;

                    Style.Text.PathEffect?.Dispose();
                    Style.Text.PathEffect = SKPathEffect.CreateTrim(0f, Math.Max(0f, progress));

                    Thread.Sleep(12);
                }

                if (session != _animationSession) return;

                Style.Text.PathEffect?.Dispose();
                Style.Text.PathEffect = null;
                Style.Border.PathEffect?.Dispose();
                Style.Border.PathEffect = null;

                Style.Text.Color = new SKColor(_accentColor.Red, _accentColor.Green, _accentColor.Blue, 100);

                Style.Shadow.SpreadX = _normalShadowSpread;
                Style.Shadow.SpreadY = _normalShadowSpread;
            });
            _animationThread.Start();
        }
    }
}
