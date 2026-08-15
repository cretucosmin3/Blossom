using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using SkiaSharp;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views
{
    public class PhotoEditorView : View
    {
        public Action? OnSwitchToDashboard;
        public Action? OnSwitchToNeon;
        public Action? OnSwitchToPaint;
        public Action? OnSwitchToKanban;
        public Action? OnSwitchTo3D;
        public Action? OnSwitchToGlass;

        private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();
        private readonly Dictionary<string, SKBitmap> _imageCache = new();

        private PhotoEditorParams _params = PhotoEditorParams.Default;
        private SKBitmap[] _samplePhotos = new SKBitmap[5];
        private string[] _sampleNames = new[] { "🏔 Mountain", "🌃 Neon City", "👤 Portrait", "🌅 Sunset", "🌲 Forest" };
        private readonly string[] _onlineUrls = new[]
        {
            "https://picsum.photos/id/1015/1280/720",
            "https://picsum.photos/id/1059/1280/720",
            "https://picsum.photos/id/1025/1280/720",
            "https://picsum.photos/id/1018/1280/720",
            "https://picsum.photos/id/1043/1280/720"
        };
        private int _currentPhotoIndex = 0;
        private PhotoViewportElement? _viewport;

        private VisualElement? _presetLabel;
        private Button? _splitBtn;
        private bool _isSplitView = false;

        public PhotoEditorView() : base("Photo Editor")
        {
            BackColor = new SKColor(15, 19, 28); // Dark Lightroom Darkroom aesthetic
        }

        public override void Init()
        {
            // 1. Generate Procedural High-Res Sample Photos
            GenerateSamplePhotos();

            float leftSidebarWidth = 220f;
            float rightSidebarWidth = 340f;
            float topBarHeight = 55f;

            // --- 1. LEFT NAVIGATION SIDEBAR (Blossom OS Nav) ---
            var leftSidebar = new VisualElement
            {
                Name = "PE_LeftSidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(20, 24, 36),
                    Border = new BorderStyle { Width = 1, Color = new SKColor(35, 42, 60) },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(120), SpreadX = 6, OffsetX = 2 }
                },
                Transform = new Transform(0, 0, leftSidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true
                }
            };
            AddElement(leftSidebar);

            // Brand Header
            var brand = new VisualElement
            {
                Name = "PE_Brand",
                Text = "📷 LIGHTROOM GPU",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 16, Weight = 800, Alignment = TextAlign.Center, Padding = 12 }
                },
                Transform = new Transform(0, 15, leftSidebarWidth, 40)
            };
            leftSidebar.AddChild(brand);

            // Navigation items
            string[] menuItems = { "Overview", "Neon Showcase", "Neon Paint", "Task Board", "3D Showcase", "Glass Showcase", "Photo Editor" };
            float menuY = 60f;
            for (int i = 0; i < menuItems.Length; i++)
            {
                int idx = i;
                bool isActive = (idx == 6);
                var btn = new SidebarButton(menuItems[i], isActive)
                {
                    Transform = { X = 12, Y = menuY, Width = leftSidebarWidth - 24 }
                };

                btn.OnClick = () =>
                {
                    if (idx == 0) OnSwitchToDashboard?.Invoke();
                    else if (idx == 1) OnSwitchToNeon?.Invoke();
                    else if (idx == 2) OnSwitchToPaint?.Invoke();
                    else if (idx == 3) OnSwitchToKanban?.Invoke();
                    else if (idx == 4) OnSwitchTo3D?.Invoke();
                    else if (idx == 5) OnSwitchToGlass?.Invoke();
                };

                leftSidebar.AddChild(btn);
                menuY += 45f;
            }

            // Photo Selection Section in Left Sidebar
            var photoHeader = new VisualElement
            {
                Name = "PE_PhotoHeader",
                Text = "SAMPLE PICTURES",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 15 }
                },
                Transform = new Transform(0, menuY + 10, leftSidebarWidth, 25)
            };
            leftSidebar.AddChild(photoHeader);

            float photoY = menuY + 36f;
            for (int i = 0; i < _sampleNames.Length; i++)
            {
                int photoIdx = i;
                var photoBtn = new Button(_sampleNames[i], new SKColor(30, 41, 59))
                {
                    Transform = new Transform(12, photoY, leftSidebarWidth - 24, 30)
                    {
                        FixedWidth = true,
                        FixedHeight = true
                    }
                };
                photoBtn.Style.Text.Color = new SKColor(226, 232, 240);
                photoBtn.Style.Text.Size = 11;
                photoBtn.Style.Border.Roundness = 5;
                photoBtn.OnClick = () =>
                {
                    _currentPhotoIndex = photoIdx;
                    if (_viewport != null)
                    {
                        _viewport.SetSourceBitmap(_samplePhotos[_currentPhotoIndex]);
                    }
                    if (_presetLabel != null) _presetLabel.Text = $"Active: {_sampleNames[photoIdx]} (Instant Local HD)";
                    RenderRequired = true;
                };
                leftSidebar.AddChild(photoBtn);
                photoY += 34f;
            }

            // Custom Web URL Section
            var webHeader = new VisualElement
            {
                Name = "PE_WebHeader",
                Text = "CUSTOM IMAGE URL",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 15 }
                },
                Transform = new Transform(0, photoY + 5, leftSidebarWidth, 22)
            };
            leftSidebar.AddChild(webHeader);

            var urlInput = new InputField("https://...", "https://picsum.photos/1280/720")
            {
                Transform = new Transform(12, photoY + 28, leftSidebarWidth - 24, 28)
            };
            leftSidebar.AddChild(urlInput);

            var fetchBtn = new Button("🌐 Fetch Web Photo", new SKColor(56, 189, 248))
            {
                Transform = new Transform(12, photoY + 60, leftSidebarWidth - 24, 32)
                {
                    FixedWidth = true,
                    FixedHeight = true
                }
            };
            fetchBtn.Style.Text.Color = new SKColor(15, 23, 42);
            fetchBtn.Style.Text.Weight = 700;
            fetchBtn.Style.Text.Size = 11;
            fetchBtn.Style.Border.Roundness = 5;
            fetchBtn.OnClick = () =>
            {
                if (!string.IsNullOrWhiteSpace(urlInput.Value))
                {
                    LoadOnlineImage(urlInput.Value, "Web Image");
                }
            };
            leftSidebar.AddChild(fetchBtn);


            // --- 2. TOP TOOLBAR & PRESETS BAR ---
            var topBar = new VisualElement
            {
                Name = "PE_TopBar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(22, 27, 40),
                    Border = new BorderStyle { Width = 1, Color = new SKColor(38, 46, 66) }
                },
                Transform = new Transform(leftSidebarWidth, 0, Width - leftSidebarWidth - rightSidebarWidth, topBarHeight)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            AddElement(topBar);

            // Title / Status Label
            _presetLabel = new VisualElement
            {
                Name = "PE_PresetLabel",
                Text = "Preset: Standard (GPU Shader Active)",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(226, 232, 240), Size = 13, Weight = 600, Alignment = TextAlign.Left, Padding = 15 }
                },
                Transform = new Transform(10, 12, 260, 30)
            };
            topBar.AddChild(_presetLabel);

            // Top Action Buttons (Reset, Compare, Randomize)
            float actionX = 280f;

            var resetBtn = new Button("⏮ Reset All", new SKColor(220, 38, 38))
            {
                Transform = new Transform(actionX, 10, 95, 34) { FixedWidth = true, FixedHeight = true }
            };
            resetBtn.Style.Text.Size = 11;
            resetBtn.Style.Border.Roundness = 6;
            resetBtn.OnClick = () => ResetAllParameters();
            topBar.AddChild(resetBtn);
            actionX += 105f;

            _splitBtn = new Button("☯ Compare Split", new SKColor(79, 70, 229))
            {
                Transform = new Transform(actionX, 10, 115, 34) { FixedWidth = true, FixedHeight = true }
            };
            _splitBtn.Style.Text.Size = 11;
            _splitBtn.Style.Border.Roundness = 6;
            _splitBtn.OnClick = () => ToggleSplitView();
            topBar.AddChild(_splitBtn);
            actionX += 125f;

            var randBtn = new Button("🎲 Random Look", new SKColor(16, 185, 129))
            {
                Transform = new Transform(actionX, 10, 115, 34) { FixedWidth = true, FixedHeight = true }
            };
            randBtn.Style.Text.Size = 11;
            randBtn.Style.Border.Roundness = 6;
            randBtn.OnClick = () => ApplyRandomLook();
            topBar.AddChild(randBtn);


            // --- 3. PRESET QUICK STRIP (Middle Sub-Bar) ---
            float presetBarY = topBarHeight;
            float presetBarH = 40f;
            var presetBar = new VisualElement
            {
                Name = "PE_PresetBar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(18, 22, 33),
                    Border = new BorderStyle { Width = 1, Color = new SKColor(32, 39, 56) }
                },
                Transform = new Transform(leftSidebarWidth, presetBarY, Width - leftSidebarWidth - rightSidebarWidth, presetBarH)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            AddElement(presetBar);

            string[] presets = { "Default", "Vivid", "Cinematic", "Vintage 35mm", "Cyberpunk", "Noir B&W", "Sunset", "Arctic" };
            float prX = 15f;
            for (int i = 0; i < presets.Length; i++)
            {
                string presetName = presets[i];
                var pBtn = new Button(presetName, new SKColor(30, 41, 59))
                {
                    Transform = new Transform(prX, 5, 88, 30) { FixedWidth = true, FixedHeight = true }
                };
                pBtn.Style.Text.Size = 11;
                pBtn.Style.Text.Color = new SKColor(203, 213, 225);
                pBtn.Style.Border.Roundness = 5;
                pBtn.OnClick = () => ApplyPreset(presetName);
                presetBar.AddChild(pBtn);
                prX += 93f;
            }


            // --- 4. CENTER VIEWPORT (PHOTO CANVAS) ---
            float viewportY = topBarHeight + presetBarH;
            float viewportW = Width - leftSidebarWidth - rightSidebarWidth;
            float viewportH = Height - viewportY;

            _viewport = new PhotoViewportElement(_samplePhotos[_currentPhotoIndex])
            {
                Name = "PE_Viewport",
                Transform = new Transform(leftSidebarWidth, viewportY, viewportW, viewportH)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            _viewport.GetShaderParams = () =>
            {
                var p = _params;
                p.Time = SKSLShaderTimeTracker.ElapsedSeconds;
                return p;
            };
            AddElement(_viewport);


            // --- 5. RIGHT LIGHTROOM ADJUSTMENTS SIDEBAR ---
            var rightSidebar = new VisualElement
            {
                Name = "PE_RightSidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(20, 24, 36),
                    Border = new BorderStyle { Width = 1, Color = new SKColor(35, 42, 60) },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(120), SpreadX = 6, OffsetX = -2 }
                },
                Transform = new Transform(Width - rightSidebarWidth, 0, rightSidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Right,
                    FixedWidth = true
                }
            };
            AddElement(rightSidebar);

            // Right Sidebar Header
            var adjustHeader = new VisualElement
            {
                Name = "PE_AdjustHeader",
                Text = "⚙️ GPU SHADER ADJUSTMENTS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 13, Weight = 800, Alignment = TextAlign.Center, Padding = 15 }
                },
                Transform = new Transform(0, 10, rightSidebarWidth, 40)
            };
            rightSidebar.AddChild(adjustHeader);

            // Create Slider Controls Stack
            float curY = 50f;

            // SECTION 1: LIGHT & EXPOSURE
            curY = AddSectionHeader(rightSidebar, "☀️ LIGHT & EXPOSURE", curY, rightSidebarWidth);
            
            curY = AddLightroomSlider(rightSidebar, "Exposure", -2f, 2f, _params.Exposure, (v) => { _params.Exposure = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "EV");
            curY = AddLightroomSlider(rightSidebar, "Contrast", -1f, 1f, _params.Contrast, (v) => { _params.Contrast = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Brightness", -1f, 1f, _params.Brightness, (v) => { _params.Brightness = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Highlights", -1f, 1f, _params.Highlights, (v) => { _params.Highlights = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Shadows", -1f, 1f, _params.Shadows, (v) => { _params.Shadows = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");

            // SECTION 2: COLOR & WHITE BALANCE
            curY = AddSectionHeader(rightSidebar, "🎨 COLOR & TEMP", curY, rightSidebarWidth);

            curY = AddLightroomSlider(rightSidebar, "Temperature", -1f, 1f, _params.Temperature, (v) => { _params.Temperature = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "K");
            curY = AddLightroomSlider(rightSidebar, "Tint", -1f, 1f, _params.Tint, (v) => { _params.Tint = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "");
            curY = AddLightroomSlider(rightSidebar, "Saturation", 0f, 2f, _params.Saturation, (v) => { _params.Saturation = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "x");
            curY = AddLightroomSlider(rightSidebar, "Hue Shift", -3.14f, 3.14f, _params.Hue, (v) => { _params.Hue = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "rad");

            // SECTION 3: EFFECTS & TEXTURE
            curY = AddSectionHeader(rightSidebar, "✨ EFFECTS & SHARPNESS", curY, rightSidebarWidth);

            curY = AddLightroomSlider(rightSidebar, "Vignette", 0f, 1f, _params.Vignette, (v) => { _params.Vignette = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Sepia Tone", 0f, 1f, _params.Sepia, (v) => { _params.Sepia = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Film Grain", 0f, 1f, _params.Grain, (v) => { _params.Grain = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Sharpening", 0f, 1f, _params.Sharpen, (v) => { _params.Sharpen = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");

            // SECTION 4: RGB CHANNEL MIXER
            curY = AddSectionHeader(rightSidebar, "🎛 RGB CHANNEL BOOST", curY, rightSidebarWidth);

            curY = AddLightroomSlider(rightSidebar, "Red Channel", -1f, 1f, _params.RedBoost, (v) => { _params.RedBoost = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Green Channel", -1f, 1f, _params.GreenBoost, (v) => { _params.GreenBoost = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
            curY = AddLightroomSlider(rightSidebar, "Blue Channel", -1f, 1f, _params.BlueBoost, (v) => { _params.BlueBoost = v; NotifyShaderChanged(); }, curY, rightSidebarWidth, "%");
        }

        private float AddSectionHeader(VisualElement parent, string title, float y, float parentW)
        {
            var header = new VisualElement
            {
                Name = $"PE_Section_{title}",
                Text = title,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 10, Weight = 800, Alignment = TextAlign.Left, Padding = 15 }
                },
                Transform = new Transform(0, y, parentW, 22)
            };
            parent.AddChild(header);
            return y + 24f;
        }

        private float AddLightroomSlider(VisualElement parent, string labelText, float min, float max, float initialVal, Action<float> onChanged, float y, float parentW, string unit)
        {
            float labelW = 110f;
            float valW = 50f;
            float sliderW = parentW - labelW - valW - 30f;

            var container = new VisualElement
            {
                Name = $"PE_SliderRow_{labelText}",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(15, y, parentW - 30f, 32)
            };

            var label = new VisualElement
            {
                Name = $"PE_Label_{labelText}",
                Text = labelText,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(203, 213, 225), Size = 11, Weight = 500, Alignment = TextAlign.Left, Padding = 0 }
                },
                Transform = new Transform(0, 6, labelW, 20)
            };
            container.AddChild(label);

            var valueVal = new VisualElement
            {
                Name = $"PE_Val_{labelText}",
                Text = FormatVal(initialVal, unit),
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 11, Weight = 700, Alignment = TextAlign.Right, Padding = 0 }
                },
                Transform = new Transform(labelW + sliderW, 6, valW, 20)
            };
            container.AddChild(valueVal);

            var slider = new Slider(min, max, initialVal)
            {
                Transform = new Transform(labelW, 2, sliderW, 24)
            };
            slider.OnValueChanged = (val) =>
            {
                valueVal.Text = FormatVal(val, unit);
                onChanged(val);
            };

            container.AddChild(slider);
            parent.AddChild(container);

            return y + 34f;
        }

        private string FormatVal(float v, string unit)
        {
            if (unit == "EV") return (v >= 0 ? "+" : "") + v.ToString("F2") + " EV";
            if (unit == "%") return ((int)(v * 100)).ToString() + "%";
            if (unit == "x") return v.ToString("F2") + "x";
            if (unit == "K") return (v * 100).ToString("F0") + "K";
            return v.ToString("F2");
        }

        private void NotifyShaderChanged()
        {
            if (_viewport != null)
            {
                _viewport.IsDirty = true;
                _viewport.ScheduleRender();
            }
            RenderRequired = true;
        }

        private void ResetAllParameters()
        {
            _params = PhotoEditorParams.Default;
            _isSplitView = false;
            if (_presetLabel != null) _presetLabel.Text = "Preset: Standard (Reset)";
            NotifyShaderChanged();
        }

        private void ToggleSplitView()
        {
            _isSplitView = !_isSplitView;
            _params.SplitPos = _isSplitView ? 0.5f : 0f;
            if (_splitBtn != null)
            {
                _splitBtn.Style.BackColor = _isSplitView ? new SKColor(234, 88, 12) : new SKColor(79, 70, 229);
            }
            NotifyShaderChanged();
        }

        private void ApplyPreset(string name)
        {
            _params = PhotoEditorParams.Default;
            switch (name)
            {
                case "Vivid":
                    _params.Saturation = 1.45f;
                    _params.Contrast = 0.25f;
                    _params.Highlights = 0.15f;
                    _params.Sharpen = 0.35f;
                    break;
                case "Cinematic":
                    _params.Exposure = -0.1f;
                    _params.Contrast = 0.3f;
                    _params.Temperature = 0.2f; // Warm tones
                    _params.Tint = -0.15f;      // Teal shadow bias
                    _params.Shadows = -0.2f;
                    _params.Vignette = 0.4f;
                    break;
                case "Vintage 35mm":
                    _params.Sepia = 0.35f;
                    _params.Temperature = 0.3f;
                    _params.Grain = 0.4f;
                    _params.Vignette = 0.5f;
                    _params.Contrast = 0.15f;
                    break;
                case "Cyberpunk":
                    _params.Temperature = -0.4f; // Cold blue base
                    _params.Tint = 0.5f;         // Magenta tint
                    _params.RedBoost = 0.4f;
                    _params.BlueBoost = 0.5f;
                    _params.Contrast = 0.35f;
                    _params.Saturation = 1.3f;
                    break;
                case "Noir B&W":
                    _params.Saturation = 0f;
                    _params.Contrast = 0.5f;
                    _params.Shadows = -0.3f;
                    _params.Grain = 0.3f;
                    _params.Vignette = 0.45f;
                    break;
                case "Sunset":
                    _params.Temperature = 0.6f;  // Golden amber
                    _params.RedBoost = 0.3f;
                    _params.Exposure = 0.15f;
                    _params.Saturation = 1.25f;
                    break;
                case "Arctic":
                    _params.Temperature = -0.6f; // Ice blue
                    _params.BlueBoost = 0.4f;
                    _params.Highlights = 0.25f;
                    _params.Sharpen = 0.5f;
                    break;
            }
            if (_presetLabel != null) _presetLabel.Text = $"Preset: {name}";
            NotifyShaderChanged();
        }

        private void ApplyRandomLook()
        {
            var rnd = new Random();
            _params.Exposure = (float)(rnd.NextDouble() * 1.6 - 0.8);
            _params.Contrast = (float)(rnd.NextDouble() * 0.8 - 0.4);
            _params.Saturation = (float)(rnd.NextDouble() * 1.5 + 0.3);
            _params.Temperature = (float)(rnd.NextDouble() * 1.4 - 0.7);
            _params.Tint = (float)(rnd.NextDouble() * 1.0 - 0.5);
            _params.Vignette = (float)(rnd.NextDouble() * 0.6);
            _params.Grain = (float)(rnd.NextDouble() * 0.3);
            _params.Hue = (float)(rnd.NextDouble() * 1.2 - 0.6);
            if (_presetLabel != null) _presetLabel.Text = "Preset: 🎲 Random GPU Look";
            NotifyShaderChanged();
        }

        private async void LoadOnlineImage(string url, string name)
        {
            if (_imageCache.TryGetValue(url, out var cachedBmp))
            {
                if (_viewport != null) _viewport.SetSourceBitmap(cachedBmp);
                if (_presetLabel != null) _presetLabel.Text = $"Active: {name} (Cached {cachedBmp.Width}x{cachedBmp.Height})";
                RenderRequired = true;
                return;
            }

            if (_presetLabel != null) _presetLabel.Text = $"🌐 Downloading {name}...";
            RenderRequired = true;

            try
            {
                byte[] data = await _httpClient.GetByteArrayAsync(url);
                using var stream = new System.IO.MemoryStream(data);
                var bmp = SKBitmap.Decode(stream);

                if (bmp != null)
                {
                    _imageCache[url] = bmp;
                    if (_viewport != null)
                    {
                        _viewport.SetSourceBitmap(bmp);
                    }
                    if (_presetLabel != null) _presetLabel.Text = $"Active: {name} ({bmp.Width}x{bmp.Height})";
                    RenderRequired = true;
                }
                else
                {
                    if (_presetLabel != null) _presetLabel.Text = $"❌ Could not decode picture from URL";
                    RenderRequired = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ONLINE IMAGE ERROR] {ex.Message}");
                if (_viewport != null && _currentPhotoIndex >= 0 && _currentPhotoIndex < _samplePhotos.Length)
                {
                    _viewport.SetSourceBitmap(_samplePhotos[_currentPhotoIndex]);
                }
                if (_presetLabel != null) _presetLabel.Text = $"Active: {name} (Instant Fallback)";
                RenderRequired = true;
            }
        }

        #region Procedural Sample Photos
        private void GenerateSamplePhotos()
        {
            int w = 1280;
            int h = 720;

            // 1. Mountain Peak
            _samplePhotos[0] = CreateBitmap(w, h, (canvas) =>
            {
                // Sunrise Sky Gradient
                using var skyPaint = new SKPaint();
                skyPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(0, h * 0.55f),
                    new[] { new SKColor(20, 25, 55), new SKColor(245, 130, 85), new SKColor(255, 205, 120) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h * 0.55f, skyPaint);

                // Sun disc
                using var sunPaint = new SKPaint { Color = new SKColor(255, 245, 220), IsAntialias = true };
                canvas.DrawCircle(w * 0.5f, h * 0.38f, 50f, sunPaint);

                // Mountain Peaks
                using var mtPaint = new SKPaint { Color = new SKColor(35, 42, 65), IsAntialias = true };
                var mtPath = new SKPath();
                mtPath.MoveTo(0, h * 0.55f);
                mtPath.LineTo(w * 0.25f, h * 0.22f);
                mtPath.LineTo(w * 0.45f, h * 0.40f);
                mtPath.LineTo(w * 0.65f, h * 0.18f);
                mtPath.LineTo(w * 0.85f, h * 0.45f);
                mtPath.LineTo(w, h * 0.55f);
                mtPath.Close();
                canvas.DrawPath(mtPath, mtPaint);

                // Snow Cap Accents
                using var snowPaint = new SKPaint { Color = new SKColor(240, 245, 255), IsAntialias = true };
                var snowPath = new SKPath();
                snowPath.MoveTo(w * 0.65f, h * 0.18f);
                snowPath.LineTo(w * 0.61f, h * 0.26f);
                snowPath.LineTo(w * 0.69f, h * 0.27f);
                snowPath.Close();
                canvas.DrawPath(snowPath, snowPaint);

                // Lake Water & Reflection
                using var lakePaint = new SKPaint();
                lakePaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, h * 0.55f), new SKPoint(0, h),
                    new[] { new SKColor(25, 45, 75), new SKColor(10, 20, 40) }, null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, h * 0.55f, w, h * 0.45f, lakePaint);
            });

            // 2. Cyberpunk Neon City
            _samplePhotos[1] = CreateBitmap(w, h, (canvas) =>
            {
                canvas.Clear(new SKColor(10, 12, 22));

                var rnd = new Random(42);
                for (int i = 0; i < 28; i++)
                {
                    float bx = i * (w / 25f);
                    float bw = rnd.Next(35, 70);
                    float bh = rnd.Next(200, 520);
                    var bColor = new SKColor((byte)rnd.Next(20, 35), (byte)rnd.Next(25, 45), (byte)rnd.Next(40, 65));

                    using var bPaint = new SKPaint { Color = bColor };
                    canvas.DrawRect(bx, h - bh, bw, bh, bPaint);

                    // Neon Window Lights
                    using var winPaint = new SKPaint();
                    SKColor neonCol = (i % 2 == 0) ? new SKColor(56, 189, 248) : new SKColor(236, 72, 153);
                    winPaint.Color = neonCol;
                    for (float wy = h - bh + 20; wy < h - 40; wy += 25)
                    {
                        for (float wx = bx + 6; wx < bx + bw - 10; wx += 14)
                        {
                            if (rnd.NextDouble() > 0.3) canvas.DrawRect(wx, wy, 8, 12, winPaint);
                        }
                    }
                }

                // Wet Road Reflection Laser Line
                using var laserPaint = new SKPaint { Color = new SKColor(236, 72, 153, 180), StrokeWidth = 6, IsAntialias = true };
                canvas.DrawLine(0, h - 30, w, h - 30, laserPaint);
            });

            // 3. Studio Portrait
            _samplePhotos[2] = CreateBitmap(w, h, (canvas) =>
            {
                // Studio Backdrop Gradient
                using var bgPaint = new SKPaint();
                bgPaint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(w * 0.5f, h * 0.4f), 450f,
                    new[] { new SKColor(45, 55, 80), new SKColor(15, 18, 28) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h, bgPaint);

                // Silhouette Head & Shoulders
                using var pPaint = new SKPaint { Color = new SKColor(25, 28, 38), IsAntialias = true };
                canvas.DrawCircle(w * 0.5f, h * 0.35f, 110f, pPaint);

                var bodyPath = new SKPath();
                bodyPath.MoveTo(w * 0.5f - 220f, h);
                bodyPath.QuadTo(w * 0.5f - 140f, h * 0.52f, w * 0.5f - 70f, h * 0.46f);
                bodyPath.LineTo(w * 0.5f + 70f, h * 0.46f);
                bodyPath.QuadTo(w * 0.5f + 140f, h * 0.52f, w * 0.5f + 220f, h);
                bodyPath.Close();
                canvas.DrawPath(bodyPath, pPaint);

                // Amber Rim Lighting Edge
                using var rimPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 8f,
                    Color = new SKColor(251, 146, 60),
                    IsAntialias = true
                };
                canvas.DrawCircle(w * 0.5f, h * 0.35f, 110f, rimPaint);
            });

            // 4. Ocean Sunset
            _samplePhotos[3] = CreateBitmap(w, h, (canvas) =>
            {
                using var skyPaint = new SKPaint();
                skyPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(0, h * 0.5f),
                    new[] { new SKColor(76, 29, 149), new SKColor(219, 39, 119), new SKColor(245, 158, 11) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h * 0.5f, skyPaint);

                // Glowing Sun
                using var sunPaint = new SKPaint { Color = new SKColor(254, 240, 138), IsAntialias = true };
                canvas.DrawCircle(w * 0.5f, h * 0.48f, 65f, sunPaint);

                // Ocean Water Waves
                using var oceanPaint = new SKPaint();
                oceanPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, h * 0.5f), new SKPoint(0, h),
                    new[] { new SKColor(180, 83, 9), new SKColor(30, 27, 75) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, h * 0.5f, w, h * 0.5f, oceanPaint);
            });

            // 5. Autumn Forest
            _samplePhotos[4] = CreateBitmap(w, h, (canvas) =>
            {
                canvas.Clear(new SKColor(30, 41, 32));

                using var canopyPaint = new SKPaint();
                canopyPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(0, h * 0.6f),
                    new[] { new SKColor(217, 119, 6), new SKColor(180, 83, 9), new SKColor(74, 222, 128) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h * 0.6f, canopyPaint);

                // Winding Dirt Trail
                using var pathPaint = new SKPaint { Color = new SKColor(120, 80, 45), IsAntialias = true };
                var path = new SKPath();
                path.MoveTo(w * 0.45f, h * 0.55f);
                path.QuadTo(w * 0.52f, h * 0.75f, w * 0.35f, h);
                path.LineTo(w * 0.65f, h);
                path.QuadTo(w * 0.58f, h * 0.75f, w * 0.55f, h * 0.55f);
                path.Close();
                canvas.DrawPath(path, pathPaint);
            });
        }

        private SKBitmap CreateBitmap(int width, int height, Action<SKCanvas> drawAction)
        {
            var bmp = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bmp);
            drawAction(canvas);
            return bmp;
        }
        #endregion

        public override void OnDeactivated()
        {
            base.OnDeactivated();
        }
    }

    #region Photo Viewport Element
    public class PhotoViewportElement : VisualElement
    {
        private SKBitmap _sourceBitmap;
        public Func<PhotoEditorParams>? GetShaderParams { get; set; }

        public PhotoViewportElement(SKBitmap initialSource)
        {
            _sourceBitmap = initialSource;
            Style = new ElementStyle { BackColor = new SKColor(12, 15, 23) };
        }

        public void SetSourceBitmap(SKBitmap bmp)
        {
            _sourceBitmap = bmp;
            IsDirty = true;
            ScheduleRender();
        }

        public override void RecordDrawCommands(CommandLedger ledger)
        {
            var cmds = new List<DrawCommand>();

            float elemW = Transform.Computed.Width;
            float elemH = Transform.Computed.Height;

            if (_sourceBitmap != null && elemW > 10 && elemH > 10)
            {
                // Fit photo inside viewport bounds with aspect ratio preserved
                float aspect = (float)_sourceBitmap.Width / _sourceBitmap.Height;
                float drawW = elemW;
                float drawH = elemW / aspect;

                if (drawH > elemH)
                {
                    drawH = elemH;
                    drawW = elemH * aspect;
                }

                float drawX = (elemW - drawW) / 2f;
                float drawY = (elemH - drawH) / 2f;

                var destRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
                var paramsObj = GetShaderParams?.Invoke() ?? PhotoEditorParams.Default;

                cmds.Add(new DrawPhotoShaderCommand(_sourceBitmap, destRect, paramsObj));
                cmds.Add(new DrawHistogramCommand(destRect, _sourceBitmap, paramsObj));
            }

            ledger.Record(Name, cmds);
        }
    }

    // Custom DrawCommand for rendering the GPU SKSL Shader Photo Effect
    public class DrawPhotoShaderCommand : DrawCommand
    {
        private readonly SKBitmap _bitmap;
        private readonly SKRect _dest;
        private readonly PhotoEditorParams _params;

        public DrawPhotoShaderCommand(SKBitmap bitmap, SKRect dest, PhotoEditorParams p)
        {
            _bitmap = bitmap;
            _dest = dest;
            _params = p;
        }

        public override void Execute(SKCanvas canvas)
        {
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.ClipRect(_dest);

                // Create input bitmap shader transformed to fit dest rect
                float scaleX = _dest.Width / (float)_bitmap.Width;
                float scaleY = _dest.Height / (float)_bitmap.Height;
                var matrix = SKMatrix.CreateTranslation(_dest.Left, _dest.Top);
                SKMatrix.PreConcat(ref matrix, SKMatrix.CreateScale(scaleX, scaleY));

                using var baseShader = _bitmap.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, matrix);
                using var gpuShader = SKSLShaderManager.CreatePhotoEditorShader(baseShader, _dest.Width, _dest.Height, _params);

                using var paint = new SKPaint { Shader = gpuShader, IsAntialias = true };
                canvas.DrawRect(_dest, paint);
            }
        }

        public override void Dispose() { }
    }

    // Custom DrawCommand for rendering real-time photo metadata and RGB histogram
    public class DrawHistogramCommand : DrawCommand
    {
        private readonly SKRect _dest;
        private readonly PhotoEditorParams _params;

        public DrawHistogramCommand(SKRect dest, SKBitmap bitmap, PhotoEditorParams p)
        {
            _dest = dest;
            _params = p;
        }

        public override void Execute(SKCanvas canvas)
        {
            // Bottom Info Bar
            float barH = 32f;
            float barY = _dest.Bottom - barH;
            var barRect = new SKRect(_dest.Left, barY, _dest.Right, _dest.Bottom);

            using var bgPaint = new SKPaint { Color = new SKColor(15, 19, 28, 200) };
            canvas.DrawRect(barRect, bgPaint);

            using var textPaint = new SKPaint
            {
                Color = new SKColor(203, 213, 225),
                TextSize = 11f,
                IsAntialias = true
            };
            canvas.DrawText($"RAW | 1280x720 | GPU SKSL Active | Exp: {(_params.Exposure >= 0 ? "+" : "")}{_params.Exposure:F2} EV | Sat: {_params.Saturation:F2}x", _dest.Left + 12f, barY + 20f, textPaint);

            // Mini RGB Histogram graph in bottom-right of viewport
            float histW = 120f;
            float histH = 22f;
            float histX = _dest.Right - histW - 12f;
            float histY = barY + 5f;

            using var histBg = new SKPaint { Color = new SKColor(30, 41, 59, 220) };
            canvas.DrawRect(histX, histY, histW, histH, histBg);

            // Draw illustrative RGB curve bars
            using var rPaint = new SKPaint { Color = new SKColor(239, 68, 68, 180), StrokeWidth = 1.5f };
            using var gPaint = new SKPaint { Color = new SKColor(34, 197, 94, 180), StrokeWidth = 1.5f };
            using var bPaint = new SKPaint { Color = new SKColor(59, 130, 246, 180), StrokeWidth = 1.5f };

            float step = histW / 20f;
            for (int i = 0; i < 20; i++)
            {
                float x = histX + i * step;
                float rH = (float)(Math.Sin(i * 0.4 + _params.Exposure) * 0.4 + 0.5) * histH;
                float gH = (float)(Math.Cos(i * 0.3 + _params.Saturation) * 0.4 + 0.5) * histH;
                float bH = (float)(Math.Sin(i * 0.5 + _params.Temperature) * 0.4 + 0.5) * histH;

                canvas.DrawLine(x, histY + histH, x, histY + histH - rH, rPaint);
                canvas.DrawLine(x + 1, histY + histH, x + 1, histY + histH - gH, gPaint);
                canvas.DrawLine(x + 2, histY + histH, x + 2, histY + histH - bH, bPaint);
            }
        }

        public override void Dispose() { }
    }
    #endregion
}
