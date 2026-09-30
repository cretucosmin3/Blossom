using System;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing;
using Blossom.Testing.Components;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Tabs;

public class ControlsTab : Split
{
    public ControlsTab()
    {
        Name = "Studio_ControlsTab";
        Orientation = Orientation.Horizontal;
        Ratio = 0.38f;
        BarSize = 6f;
        MinPaneSize = 220f;
        Bar.UseStyle("split-bar");
        Padding = new Thickness(16);
        Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        this.UseStyle("app");

        var textContent = CreateSignal("Blossom Studio");
        var roundness = CreateSignal(14f);
        var opacityVal = CreateSignal(1.0f);
        var isDarkMode = CreateSignal(true);
        var isGlowActive = CreateSignal(true);
        var isBlurActive = CreateSignal(true);
        var antialiasShader = CreateSignal(true);
        var animateGrid = CreateSignal(false);
        var gridSpeed = CreateSignal(1f);
        var clickCount = CreateSignal(0);
        var hitCount = CreateSignal(0);
        var rotY = CreateSignal(22f);
        var rotX = CreateSignal(-10f);
        var rotZ = CreateSignal(6f);
        var perspective = CreateSignal(900f);
        var scale = CreateSignal(1.0f);
        var accepted = CreateSignal(true);

        var charCount = CreateMemo(() => textContent.Value.Length);
        var validation = CreateMemo(() =>
        {
            var t = textContent.Value.Trim();
            if (t.Length == 0) return "Title is required";
            if (t.Length > 40) return "Keep the title under 40 characters";
            return "";
        });
        var isValid = CreateMemo(() => validation.Value.Length == 0);

        var formCard = new StackScroll { Name = "Controls_Form" };
        formCard.UseStyle("surface");

        var formStack = new Stack
        {
            Orientation = Orientation.Vertical,
            Gap = 6,
            Padding = new Thickness(16, 14)
        };

        var formTitle = SectionLabel("CONTROLS & BINDINGS");
        var inputLabel = BodyLabel("Card heading");
        var inputField = new InputField("Type a title...", textContent.Value);
        inputField.UseStyle("field");
        inputField.Changed += val => textContent.Value = val ?? "";
        inputField.Bind(_ =>
        {
            var next = textContent.Value;
            if (inputField.Value != next)
                inputField.Value = next;
        });

        var inputMeta = new VisualElement();
        inputMeta.UseStyle("body");
        inputMeta.BindText(() => $"{charCount.Value} / 40 characters");

        var inputError = new VisualElement();
        inputError.UseStyle("body");
        inputError.Bind(el =>
        {
            el.Style.Text.Color = DemoThemes.Current.Colour("danger");
            el.Style.Text.Size = 11f;
            el.Style.Text.Weight = 600;
        });
        inputError.BindText(() => validation.Value);
        inputError.BindVisible(() => !isValid.Value);

        formStack.AddChild(formTitle);
        formStack.AddChild(inputLabel);
        formStack.AddChild(inputField);
        formStack.AddChild(inputMeta);
        formStack.AddChild(inputError);
        formStack.AddChild(SectionSpacer());
        formStack.AddChild(SectionLabel("3D TRANSFORM"));
        formStack.AddChild(BindLabel(() => $"Yaw  {rotY.Value:F0} deg"));
        formStack.AddChild(BindSlider(rotY, -40f, 40f));
        formStack.AddChild(BindLabel(() => $"Pitch  {rotX.Value:F0} deg"));
        formStack.AddChild(BindSlider(rotX, -30f, 30f));
        formStack.AddChild(BindLabel(() => $"Perspective  {perspective.Value:F0}"));
        formStack.AddChild(BindSlider(perspective, 500f, 1600f));
        formStack.AddChild(BindLabel(() => $"Scale  {scale.Value:F2}"));
        formStack.AddChild(BindSlider(scale, 0.75f, 1.25f));
        formStack.AddChild(BindLabel(() => $"Roll  {rotZ.Value:F0} deg"));
        formStack.AddChild(BindSlider(rotZ, -20f, 20f));
        formStack.AddChild(SectionSpacer());
        formStack.AddChild(SectionLabel("SURFACE"));
        formStack.AddChild(BindLabel(() => $"Corner radius  {roundness.Value:F0}px"));
        formStack.AddChild(BindSlider(roundness, 0f, 32f));
        formStack.AddChild(BindLabel(() => $"Opacity  {opacityVal.Value * 100f:F0}%"));
        formStack.AddChild(BindSlider(opacityVal, 0.35f, 1.0f));
        formStack.AddChild(BindSwitch("Dark surface", isDarkMode));
        formStack.AddChild(BindSwitch("Accent glow", isGlowActive));
        formStack.AddChild(BindSwitch("Frosted glass", isBlurActive));
        formStack.AddChild(BindSwitch("Antialias shader", antialiasShader));

        var animateGridBtn = new Button("Animate grid", enable3DEffect: false);
        animateGridBtn.Clicked += () => animateGrid.Update(v => !v);
        animateGridBtn.Bind(b =>
        {
            var th = DemoThemes.Current;
            bool on = animateGrid.Value;
            b.Label = on ? "Stop grid motion" : "Animate grid";
            b.NormalColor = on ? th.Colour("accent") : th.Colour("ghost");
            b.Style.Text.Color = on ? th.Colour("on-accent") : th.Colour("text");
            b.Style.Border.Width = on ? 0 : th.Number("stroke", 1f);
            b.Style.Border.Color = th.Colour("border");
            b.Style.Border.Roundness = th.Number("radius");
        });
        formStack.AddChild(animateGridBtn);
        formStack.AddChild(BindLabel(() => $"Grid speed  {gridSpeed.Value:0.0}x"));
        var gridSpeedSlider = BindSlider(gridSpeed, 0.1f, 120f);
        gridSpeedSlider.ValueFormat = "{0:0.0}x";
        formStack.AddChild(gridSpeedSlider);
        formStack.AddChild(SectionSpacer());
        formStack.AddChild(BindLabel(() => $"Actions  ({clickCount.Value} clicks, {hitCount.Value} 3D hits)"));

        var primaryBtn = new Button("Primary", enable3DEffect: true);
        var depthBtn = new Button("3D press", enable3DEffect: true);
        var dangerBtn = new Button("Danger", enable3DEffect: true);
        DemoThemes.TintButton(primaryBtn, "accent");
        DemoThemes.TintButton(depthBtn, "ghost", "text");
        DemoThemes.TintButton(dangerBtn, "danger");
        primaryBtn.Clicked += () => clickCount.Update(c => c + 1);
        depthBtn.Clicked += () => clickCount.Update(c => c + 1);
        dangerBtn.Clicked += () => clickCount.Update(c => c + 1);

        var btnRow = new Stack
        {
            Orientation = Orientation.Horizontal,
            Gap = 8,
            Align = LayoutAlign.Stretch
        };
        btnRow.MinHeight = 32;
        btnRow.Transform.Height = 32;
        btnRow.AddChild(primaryBtn);
        btnRow.AddChild(depthBtn);
        btnRow.AddChild(dangerBtn);
        Stack.SetGrow(primaryBtn, 1f);
        Stack.SetGrow(depthBtn, 1f);
        Stack.SetGrow(dangerBtn, 1f);
        formStack.AddChild(btnRow);

        var checkbox = new Checkbox("Accept live updates", accepted.Value);
        checkbox.Changed += v => accepted.Value = v;
        checkbox.Bind(_ =>
        {
            if (checkbox.IsChecked != accepted.Value)
                checkbox.IsChecked = accepted.Value;
        });
        formStack.AddChild(checkbox);

        var batchResetBtn = new Button("Batch reset", enable3DEffect: false);
        DemoThemes.TintButton(batchResetBtn, "ghost", "text");
        batchResetBtn.Clicked += () =>
        {
            Batch(() =>
            {
                clickCount.Value = 0;
                hitCount.Value = 0;
                textContent.Value = "Blossom Studio";
                roundness.Value = 14f;
                opacityVal.Value = 1.0f;
                isDarkMode.Value = true;
                isGlowActive.Value = true;
                isBlurActive.Value = true;
                antialiasShader.Value = true;
                animateGrid.Value = false;
                gridSpeed.Value = 1f;
                rotY.Value = 22f;
                rotX.Value = -10f;
                rotZ.Value = 6f;
                perspective.Value = 900f;
                scale.Value = 1.0f;
                accepted.Value = true;
            });
        };
        formStack.AddChild(batchResetBtn);
        formCard.AddChild(formStack);

        var stageCard = new Stack
        {
            Name = "Controls_Stage",
            Orientation = Orientation.Vertical,
            Gap = 10,
            Padding = new Thickness(16)
        };
        stageCard.UseStyle("surface");

        var stageTitle = SectionLabel("LIVE 3D STAGE");

        var stageWell = new StageWell { Name = "Controls_StageWell" };
        stageWell.UseStyle("well");
        stageWell.Style.BackgroundShader = BackgroundShaderType.SynthwaveGrid;
        stageWell.Style.ShaderRenderMode = EffectRenderMode.OnDemand;
        stageWell.IsAntialias = true;
        stageWell.OverflowX = OverflowMode.Clip;
        stageWell.OverflowY = OverflowMode.Clip;
        stageWell.Bind(w =>
        {
            var th = DemoThemes.Current;
            w.Style.BackgroundShaderColor = th.Colour("accent").WithAlpha(80);
            w.Style.ShaderRenderMode = animateGrid.Value
                ? EffectRenderMode.Continuous
                : EffectRenderMode.OnDemand;
            w.IsAntialias = antialiasShader.Value;
            w.Style.ShaderSpeed = gridSpeed.Value;
            w.InvalidatePaint();
        });

        var liveCard = new Stack
        {
            Name = "Controls_Live3DCard",
            Orientation = Orientation.Vertical,
            Gap = 8,
            Padding = new Thickness(18)
        };
        DemoLayout.Panel(liveCard, new SKColor(45, 45, 52), 14f);
        liveCard.Style.Shadow = new ShadowStyle
        {
            Color = new SKColor(79, 70, 229, 100),
            SpreadY = 8,
            OffsetY = 10
        };
        liveCard.Transform.TransformOriginX = 0.5f;
        liveCard.Transform.TransformOriginY = 0.5f;
        liveCard.Bind(c =>
        {
            var th = DemoThemes.Current;
            c.Opacity = opacityVal.Value;
            c.Style.BackColor = isDarkMode.Value ? th.Colour("surface") : th.Colour("field");
            c.Style.Border.Width = isGlowActive.Value ? 2 : 1;
            c.Style.Border.Color = isGlowActive.Value ? th.Colour("accent") : th.Colour("border");
            c.Style.Border.Roundness = roundness.Value;
            c.Style.Shadow.Color = isGlowActive.Value ? th.Colour("accent").WithAlpha(100) : SKColors.Black.WithAlpha(60);
            c.Style.Shadow.SpreadY = isGlowActive.Value ? 8 : 3;
            c.Style.Shadow.OffsetY = isGlowActive.Value ? 10 : 4;
            c.Style.BackdropBlur = isBlurActive.Value ? 10f : 0f;
            c.Transform.RotationY = rotY.Value;
            c.Transform.RotationX = rotX.Value;
            c.Transform.RotationZ = rotZ.Value;
            c.Transform.Perspective = perspective.Value;
            c.Transform.ScaleX = scale.Value;
            c.Transform.ScaleY = scale.Value;
            c.InvalidatePaint();
        });

        var previewHeading = new VisualElement();
        previewHeading.UseStyle("display");
        previewHeading.BindText(() => textContent.Value);

        var previewSub = new VisualElement();
        previewSub.UseStyle("body");
        previewSub.BindText(() => isValid.Value
            ? $"{charCount.Value} chars   ·   yaw {rotY.Value:F0}   ·   pitch {rotX.Value:F0}   ·   roll {rotZ.Value:F0}"
            : validation.Value);

        var hitBtn = new Button("Hit in 3D", enable3DEffect: true);
        hitBtn.Clicked += () => hitCount.Update(c => c + 1);
        hitBtn.MaxWidth = 140;
        hitBtn.MinWidth = 110;
        DemoThemes.TintButton(hitBtn, "accent");

        var hitLabel = new VisualElement();
        hitLabel.UseStyle("status");
        hitLabel.BindText(() => hitCount.Value == 0
            ? "Inverse hit-test: click the button on the rotated card"
            : $"3D hits registered: {hitCount.Value}");

        liveCard.AddChild(previewHeading);
        liveCard.AddChild(previewSub);
        liveCard.AddChild(hitBtn);
        liveCard.AddChild(hitLabel);
        Stack.SetGrow(hitLabel, 1f);

        var frosted = new Stack
        {
            Orientation = Orientation.Horizontal,
            Align = LayoutAlign.Center,
            Padding = new Thickness(8, 4)
        };
        DemoLayout.Panel(frosted, new SKColor(20, 20, 26, 140), 8f, new SKColor(255, 255, 255, 28));
        frosted.Style.Shadow = null!;
        frosted.Bind(c =>
        {
            c.Style.BackdropBlur = isBlurActive.Value ? 14f : 0f;
            c.Visible = true;
            c.InvalidatePaint();
        });
        var frostedText = new VisualElement { Text = "Frosted overlay", IsClickthrough = true };
        frostedText.Style.Text = new TextStyle { Color = SKColors.White, Size = 11f, Weight = 700, Alignment = TextAlign.Center };
        frosted.AddChild(frostedText);

        var inspectorBox = new StackScroll { Name = "Controls_Inspector" };
        inspectorBox.MinHeight = 148;
        inspectorBox.Transform.Height = 148;
        inspectorBox.ScrollbarVisibilityY = ScrollbarVisibility.Always;
        inspectorBox.UseStyle("well");
        inspectorBox.Bind(s =>
        {
            var th = DemoThemes.Current;
            s.ScrollbarThumbColor = th.Colour("muted").WithAlpha(180);
            s.ScrollbarThumbHoverColor = th.Colour("text").WithAlpha(220);
            s.ScrollbarThumbDragColor = th.Colour("accent").WithAlpha(230);
            s.ScrollbarRadius = th.Number("radius");
            s.InvalidatePaint();
        });

        var inspectorBody = new Stack
        {
            Orientation = Orientation.Vertical,
            Gap = 8,
            Padding = new Thickness(12, 10)
        };

        var inspectorTitle = new VisualElement { Text = "STATE", IsClickthrough = true };
        inspectorTitle.UseStyle("kicker");

        var stats = new Grid
        {
            Name = "Controls_InspectorGrid",
            Columns = "*, *, *, *",
            Rows = "Auto, Auto, Auto, Auto",
            ColumnGap = 10,
            RowGap = 8
        };
        stats.MinHeight = 176;

        void AddStat(string key, Func<string> value)
        {
            var cell = new Stack
            {
                Orientation = Orientation.Vertical,
                Gap = 2
            };
            var k = new VisualElement { Text = key, IsClickthrough = true };
            k.UseStyle("kicker");
            var v = new VisualElement { IsClickthrough = true };
            v.UseStyle("heading");
            v.BindText(value);
            cell.AddChild(k);
            cell.AddChild(v);
            stats.AddChild(cell);
        }

        static string OnOff(bool on) => on ? "on" : "off";

        AddStat("ROUND", () => $"{roundness.Value:F0}px");
        AddStat("OPACITY", () => $"{opacityVal.Value:F2}");
        AddStat("SCALE", () => $"{scale.Value:F2}");
        AddStat("PERSP", () => $"{perspective.Value:F0}");
        AddStat("YAW", () => $"{rotY.Value:F0}");
        AddStat("PITCH", () => $"{rotX.Value:F0}");
        AddStat("ROLL", () => $"{rotZ.Value:F0}");
        AddStat("CHARS", () => $"{charCount.Value}");
        AddStat("DARK", () => OnOff(isDarkMode.Value));
        AddStat("GLOW", () => OnOff(isGlowActive.Value));
        AddStat("GLASS", () => OnOff(isBlurActive.Value));
        AddStat("AA", () => OnOff(antialiasShader.Value));
        AddStat("GRID", () => animateGrid.Value ? "live" : "still");
        AddStat("SPEED", () => $"{gridSpeed.Value:0.0}x");
        AddStat("CLICKS", () => $"{clickCount.Value}");
        AddStat("HITS", () => $"{hitCount.Value}");

        inspectorBody.AddChild(inspectorTitle);
        inspectorBody.AddChild(stats);
        inspectorBox.AddChild(inspectorBody);

        stageWell.LiveCard = liveCard;
        stageWell.Frosted = frosted;
        stageWell.AddChild(liveCard);
        stageWell.AddChild(frosted);

        stageCard.AddChild(stageTitle);
        stageCard.AddChild(stageWell);
        stageCard.AddChild(inspectorBox);
        Stack.SetGrow(stageWell, 1f);

        AddChild(formCard);
        AddChild(stageCard);
    }

    protected override void LayoutChildren()
    {
        bool stacked = Transform.Width > 0 && Transform.Width < 980f;
        var next = stacked ? Orientation.Vertical : Orientation.Horizontal;
        if (Orientation != next)
        {
            Orientation = next;
            Ratio = stacked ? 0.52f : 0.38f;
        }

        base.LayoutChildren();
    }

    private static VisualElement SectionSpacer()
    {
        var el = new VisualElement { IsClickthrough = true };
        el.Transform.Height = 4;
        el.MinHeight = 4;
        el.Style.BackColor = SKColors.Transparent;
        el.Style.Border.Width = 0;
        return el;
    }

    private static VisualElement SectionLabel(string text)
    {
        var el = new VisualElement { Text = text, IsClickthrough = true };
        el.UseStyle("kicker");
        return el;
    }

    private static VisualElement BodyLabel(string text)
    {
        var el = new VisualElement { Text = text, IsClickthrough = true };
        el.UseStyle("body");
        return el;
    }

    private static VisualElement BindLabel(Func<string> text)
    {
        var el = new VisualElement();
        el.UseStyle("body");
        el.BindText(text);
        return el;
    }

    private static Slider BindSlider(Signal<float> signal, float min, float max)
    {
        var slider = new Slider("", min, max, signal.Value);
        slider.Changed += v => signal.Value = v;
        slider.Bind(_ =>
        {
            if (Math.Abs(slider.Value - signal.Value) > 0.01f)
                slider.Value = signal.Value;
        });
        return slider;
    }

    private static Switch BindSwitch(string label, Signal<bool> signal)
    {
        var sw = new Switch(label, signal.Value);
        sw.Changed += v => signal.Value = v;
        sw.Bind(_ =>
        {
            if (sw.IsOn != signal.Value)
                sw.IsOn = signal.Value;
        });
        return sw;
    }

    private sealed class StackScroll : ScrollContainer
    {
        public StackScroll()
        {
            OverflowX = OverflowMode.Clip;
            OverflowY = OverflowMode.Scroll;
            ScrollbarVisibilityX = ScrollbarVisibility.Hidden;
            Style.Shadow = null!;
        }

        protected override void LayoutChildren()
        {
            if (Children.Count > 0)
            {
                var stack = Children[0];
                float w = Math.Max(1f, Transform.Width);
                float pref = stack.GetPreferredSize(w, 0).Height;
                stack.Transform.SetLocalFrame(0, 0, w, Math.Max(1f, pref));
                SetContentSize(w, Math.Max(1f, pref));
            }

            base.LayoutChildren();
        }
    }

    private sealed class StageWell : VisualElement
    {
        public VisualElement LiveCard { get; set; } = null!;
        public VisualElement Frosted { get; set; } = null!;

        protected override void LayoutChildren()
        {
            if (LiveCard == null || Frosted == null) return;

            float x = Transform.AbsoluteX;
            float y = Transform.AbsoluteY;
            float w = Transform.Width;
            float h = Transform.Height;

            float liveW = Math.Max(220f, Math.Min(420f, w - 48f));
            float liveH = Math.Max(140f, Math.Min(220f, h - 56f));
            float liveX = x + (w - liveW) / 2f;
            float liveY = y + Math.Max(0f, (h - liveH) / 2f - 6f);
            LiveCard.Transform.SetAbsoluteFrame(liveX, liveY, liveW, liveH);
            Frosted.Transform.SetAbsoluteFrame(x + 12f, y + 12f, 132f, 28f);
        }
    }
}
