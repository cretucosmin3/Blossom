using System;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Reactive;
using Blossom.Testing.Components;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Tabs;

public class ControlsTab : Container
{
    private readonly FormColumn _formCard;
    private readonly Container _stageCard;

    private readonly VisualElement _formTitle;
    private readonly VisualElement _inputLabel;
    private readonly InputField _inputField;
    private readonly VisualElement _inputMeta;
    private readonly VisualElement _inputError;

    private readonly VisualElement _xformTitle;
    private readonly VisualElement _rotYLabel;
    private readonly Slider _rotYSlider;
    private readonly VisualElement _rotXLabel;
    private readonly Slider _rotXSlider;
    private readonly VisualElement _perspLabel;
    private readonly Slider _perspSlider;
    private readonly VisualElement _scaleLabel;
    private readonly Slider _scaleSlider;
    private readonly VisualElement _rollLabel;
    private readonly Slider _rollSlider;

    private readonly VisualElement _lookTitle;
    private readonly VisualElement _roundnessLabel;
    private readonly Slider _roundnessSlider;
    private readonly VisualElement _opacityLabel;
    private readonly Slider _opacitySlider;
    private readonly Switch _darkSwitch;
    private readonly Switch _glowSwitch;
    private readonly Switch _blurSwitch;
    private readonly Switch _aaSwitch;
    private readonly Button _animateGridBtn;
    private readonly VisualElement _gridSpeedLabel;
    private readonly Slider _gridSpeedSlider;

    private readonly VisualElement _btnLabel;
    private readonly Button _primaryBtn;
    private readonly Button _depthBtn;
    private readonly Button _dangerBtn;
    private readonly Checkbox _checkbox;
    private readonly Button _batchResetBtn;

    private readonly VisualElement _stageTitle;
    private readonly Container _stageWell;
    private readonly Container _liveCard;
    private readonly VisualElement _previewHeading;
    private readonly VisualElement _previewSub;
    private readonly Button _hitBtn;
    private readonly VisualElement _hitLabel;
    private readonly Container _frosted;
    private readonly VisualElement _frostedText;
    private readonly Container _inspectorBox;
    private readonly VisualElement _inspectorTitle;
    private readonly VisualElement _stateRow1;
    private readonly VisualElement _stateRow2;
    private readonly VisualElement _stateRow3;
    private readonly VisualElement _stateRow4;

    public ControlsTab() : base(new SKColor(23, 23, 23), roundness: 0f)
    {
        Name = "Studio_ControlsTab";
        Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        Style.Border.Width = 0;
        Style.Shadow = null!;

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

        _formCard = new FormColumn { Name = "Controls_Form" };
        _formCard.Style.BackColor = new SKColor(38, 38, 38);
        _formCard.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(58, 58, 58), Roundness = 10f };

        _stageCard = Panel("Controls_Stage");

        _formTitle = SectionLabel("CONTROLS & BINDINGS", new SKColor(163, 163, 163));
        _inputLabel = BodyLabel("Card heading");
        _inputField = new InputField("Type a title...", textContent.Value);
        _inputField.Changed += val => textContent.Value = val ?? "";
        _inputField.Bind(_ =>
        {
            var next = textContent.Value;
            if (_inputField.Value != next)
                _inputField.Value = next;
        });

        _inputMeta = new VisualElement();
        _inputMeta.Style.Text = BodyText();
        _inputMeta.BindText(() => $"{charCount.Value} / 40 characters");

        _inputError = new VisualElement();
        _inputError.Style.Text = new TextStyle { Color = new SKColor(190, 120, 120), Size = 11f, Weight = 600, Alignment = TextAlign.Left };
        _inputError.BindText(() => validation.Value);
        _inputError.BindVisible(() => !isValid.Value);

        _xformTitle = SectionLabel("3D TRANSFORM", new SKColor(163, 163, 163));
        _rotYLabel = BindLabel(() => $"Yaw  {rotY.Value:F0} deg");
        _rotYSlider = BindSlider(rotY, -40f, 40f);
        _rotXLabel = BindLabel(() => $"Pitch  {rotX.Value:F0} deg");
        _rotXSlider = BindSlider(rotX, -30f, 30f);
        _perspLabel = BindLabel(() => $"Perspective  {perspective.Value:F0}");
        _perspSlider = BindSlider(perspective, 500f, 1600f);
        _scaleLabel = BindLabel(() => $"Scale  {scale.Value:F2}");
        _scaleSlider = BindSlider(scale, 0.75f, 1.25f);
        _rollLabel = BindLabel(() => $"Roll  {rotZ.Value:F0} deg");
        _rollSlider = BindSlider(rotZ, -20f, 20f);

        _lookTitle = SectionLabel("SURFACE", new SKColor(163, 163, 163));
        _roundnessLabel = BindLabel(() => $"Corner radius  {roundness.Value:F0}px");
        _roundnessSlider = BindSlider(roundness, 0f, 32f);
        _opacityLabel = BindLabel(() => $"Opacity  {opacityVal.Value * 100f:F0}%");
        _opacitySlider = BindSlider(opacityVal, 0.35f, 1.0f);

        _darkSwitch = BindSwitch("Dark surface", isDarkMode);
        _glowSwitch = BindSwitch("Accent glow", isGlowActive);
        _blurSwitch = BindSwitch("Frosted glass", isBlurActive);
        _aaSwitch = BindSwitch("Antialias shader", antialiasShader);

        _animateGridBtn = new Button("Animate grid", new SKColor(58, 58, 58), enable3DEffect: false);
        _animateGridBtn.Clicked += () => animateGrid.Update(v => !v);
        _animateGridBtn.Bind(b =>
        {
            b.Label = animateGrid.Value ? "Stop grid motion" : "Animate grid";
            b.NormalColor = animateGrid.Value ? new SKColor(79, 70, 229) : new SKColor(58, 58, 58);
        });
        _gridSpeedLabel = BindLabel(() => $"Grid speed  {gridSpeed.Value:0.0}x");
        _gridSpeedSlider = BindSlider(gridSpeed, 0.1f, 120f);
        _gridSpeedSlider.ValueFormat = "{0:0.0}x";

        _btnLabel = BindLabel(() => $"Actions  ({clickCount.Value} clicks, {hitCount.Value} 3D hits)");
        _primaryBtn = new Button("Primary", new SKColor(79, 70, 229), enable3DEffect: true);
        _depthBtn = new Button("3D press", new SKColor(70, 70, 78), enable3DEffect: true);
        _dangerBtn = new Button("Danger", new SKColor(140, 70, 70), enable3DEffect: true);
        _primaryBtn.Clicked += () => clickCount.Update(c => c + 1);
        _depthBtn.Clicked += () => clickCount.Update(c => c + 1);
        _dangerBtn.Clicked += () => clickCount.Update(c => c + 1);

        _checkbox = new Checkbox("Accept live updates", accepted.Value);
        _checkbox.Changed += v => accepted.Value = v;
        _checkbox.Bind(_ =>
        {
            if (_checkbox.IsChecked != accepted.Value)
                _checkbox.IsChecked = accepted.Value;
        });

        _batchResetBtn = new Button("Batch reset", new SKColor(58, 58, 58), enable3DEffect: false);
        _batchResetBtn.Clicked += () =>
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

        _formCard.AddChild(_formTitle);
        _formCard.AddChild(_inputLabel);
        _formCard.AddChild(_inputField);
        _formCard.AddChild(_inputMeta);
        _formCard.AddChild(_inputError);
        _formCard.AddChild(_xformTitle);
        _formCard.AddChild(_rotYLabel);
        _formCard.AddChild(_rotYSlider);
        _formCard.AddChild(_rotXLabel);
        _formCard.AddChild(_rotXSlider);
        _formCard.AddChild(_perspLabel);
        _formCard.AddChild(_perspSlider);
        _formCard.AddChild(_scaleLabel);
        _formCard.AddChild(_scaleSlider);
        _formCard.AddChild(_rollLabel);
        _formCard.AddChild(_rollSlider);
        _formCard.AddChild(_lookTitle);
        _formCard.AddChild(_roundnessLabel);
        _formCard.AddChild(_roundnessSlider);
        _formCard.AddChild(_opacityLabel);
        _formCard.AddChild(_opacitySlider);
        _formCard.AddChild(_darkSwitch);
        _formCard.AddChild(_glowSwitch);
        _formCard.AddChild(_blurSwitch);
        _formCard.AddChild(_aaSwitch);
        _formCard.AddChild(_animateGridBtn);
        _formCard.AddChild(_gridSpeedLabel);
        _formCard.AddChild(_gridSpeedSlider);
        _formCard.AddChild(_btnLabel);
        _formCard.AddChild(_primaryBtn);
        _formCard.AddChild(_depthBtn);
        _formCard.AddChild(_dangerBtn);
        _formCard.AddChild(_checkbox);
        _formCard.AddChild(_batchResetBtn);

        _stageTitle = SectionLabel("LIVE 3D STAGE", new SKColor(110, 170, 130));

        _stageWell = new Container(new SKColor(18, 18, 22), 10f) { Name = "Controls_StageWell" };
        _stageWell.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(48, 48, 56), Roundness = 10f };
        _stageWell.Style.BackgroundShader = BackgroundShaderType.SynthwaveGrid;
        _stageWell.Style.BackgroundShaderColor = new SKColor(79, 70, 229, 80);
        _stageWell.Style.ShaderRenderMode = EffectRenderMode.OnDemand;
        _stageWell.IsAntialias = true;
        _stageWell.OverflowX = OverflowMode.Clip;
        _stageWell.OverflowY = OverflowMode.Clip;
        _stageWell.Bind(w =>
        {
            w.Style.ShaderRenderMode = animateGrid.Value
                ? EffectRenderMode.Continuous
                : EffectRenderMode.OnDemand;
            w.IsAntialias = antialiasShader.Value;
            w.Style.ShaderSpeed = gridSpeed.Value;
            w.InvalidatePaint();
        });

        _liveCard = new Container(new SKColor(45, 45, 52), 14f) { Name = "Controls_Live3DCard" };
        _liveCard.Transform.TransformOriginX = 0.5f;
        _liveCard.Transform.TransformOriginY = 0.5f;
        _liveCard.Bind(c =>
        {
            c.Opacity = opacityVal.Value;
            c.Style.BackColor = isDarkMode.Value ? new SKColor(42, 42, 50) : new SKColor(232, 234, 238);
            c.Style.Border.Width = isGlowActive.Value ? 2 : 1;
            c.Style.Border.Color = isGlowActive.Value ? new SKColor(79, 70, 229) : new SKColor(82, 82, 82);
            c.Style.Border.Roundness = roundness.Value;
            c.Style.Shadow.Color = isGlowActive.Value ? new SKColor(79, 70, 229, 100) : new SKColor(0, 0, 0, 60);
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

        _previewHeading = new VisualElement();
        _previewHeading.Style.Text = new TextStyle { Color = SKColors.White, Size = 20f, Weight = 700, Alignment = TextAlign.Left, Overflow = TextOverflow.Ellipsis, MaxLines = 2 };
        _previewHeading.BindText(() => textContent.Value);
        _previewHeading.Bind(h =>
        {
            h.Style.Text.Color = isDarkMode.Value ? SKColors.White : new SKColor(28, 28, 28);
        });

        _previewSub = new VisualElement();
        _previewSub.Style.Text = new TextStyle { Color = new SKColor(163, 163, 163), Size = 12f, Weight = 500, Alignment = TextAlign.Left };
        _previewSub.BindText(() => isValid.Value
            ? $"{charCount.Value} chars   ·   yaw {rotY.Value:F0}   ·   pitch {rotX.Value:F0}   ·   roll {rotZ.Value:F0}"
            : validation.Value);

        _hitBtn = new Button("Hit in 3D", new SKColor(79, 70, 229), enable3DEffect: true);
        _hitBtn.Clicked += () => hitCount.Update(c => c + 1);

        _hitLabel = new VisualElement();
        _hitLabel.Style.Text = new TextStyle { Color = new SKColor(163, 163, 163), Size = 11f, Weight = 600, Alignment = TextAlign.Left };
        _hitLabel.BindText(() => hitCount.Value == 0
            ? "Inverse hit-test: click the button on the rotated card"
            : $"3D hits registered: {hitCount.Value}");

        _liveCard.AddChild(_previewHeading);
        _liveCard.AddChild(_previewSub);
        _liveCard.AddChild(_hitBtn);
        _liveCard.AddChild(_hitLabel);

        _frosted = new Container(new SKColor(20, 20, 26, 140), 8f);
        _frosted.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(255, 255, 255, 28), Roundness = 8f };
        _frosted.Style.Shadow = null!;
        _frosted.Bind(c =>
        {
            c.Style.BackdropBlur = isBlurActive.Value ? 14f : 0f;
            c.Visible = true;
            c.InvalidatePaint();
        });
        _frostedText = new VisualElement { Text = "Frosted overlay", IsClickthrough = true };
        _frostedText.Style.Text = new TextStyle { Color = SKColors.White, Size = 11f, Weight = 700, Alignment = TextAlign.Center };
        _frosted.AddChild(_frostedText);

        _inspectorBox = new Container(new SKColor(23, 23, 23), 8f) { Name = "Controls_Inspector" };
        _inspectorBox.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(45, 45, 45), Roundness = 8f };
        _inspectorBox.Style.Shadow = null!;

        _inspectorTitle = new VisualElement { Text = "STATE", IsClickthrough = true };
        _inspectorTitle.Style.Text = new TextStyle { Color = new SKColor(120, 120, 120), Size = 10f, Weight = 700, Alignment = TextAlign.Left };

        _stateRow1 = InspectRow(new SKColor(110, 170, 130), () => $"round {roundness.Value:F0}px    opacity {opacityVal.Value:F2}    scale {scale.Value:F2}");
        _stateRow2 = InspectRow(new SKColor(150, 150, 210), () => $"yaw {rotY.Value:F0}    pitch {rotX.Value:F0}    roll {rotZ.Value:F0}    persp {perspective.Value:F0}");
        _stateRow3 = InspectRow(new SKColor(200, 160, 90), () => $"dark {isDarkMode.Value}    glow {isGlowActive.Value}    glass {isBlurActive.Value}    aa {antialiasShader.Value}    grid {(animateGrid.Value ? "live" : "still")} {gridSpeed.Value:0.0}x");
        _stateRow4 = InspectRow(new SKColor(163, 163, 163), () => $"chars {charCount.Value}    clicks {clickCount.Value}    3D hits {hitCount.Value}    valid {isValid.Value}");

        _inspectorBox.AddChild(_inspectorTitle);
        _inspectorBox.AddChild(_stateRow1);
        _inspectorBox.AddChild(_stateRow2);
        _inspectorBox.AddChild(_stateRow3);
        _inspectorBox.AddChild(_stateRow4);

        _stageWell.AddChild(_liveCard);
        _stageWell.AddChild(_frosted);
        _stageCard.AddChild(_stageTitle);
        _stageCard.AddChild(_stageWell);
        _stageCard.AddChild(_inspectorBox);

        AddChild(_formCard);
        AddChild(_stageCard);
    }

    protected override void LayoutChildren()
    {
        base.LayoutChildren();

        float viewW = Transform.Computed.Width;
        float viewH = Transform.Computed.Height;
        float ox = Transform.Computed.X;
        float oy = Transform.Computed.Y;
        if (viewW < 100 || viewH < 100) return;

        const float pad = 16f;
        const float gap = 14f;
        float availW = viewW - pad * 2;
        float availH = viewH - pad * 2;
        bool stacked = viewW < 980f;

        float formW = stacked ? availW : Math.Max(300f, Math.Min(420f, availW * 0.42f));
        float stageW = stacked ? availW : Math.Max(320f, availW - formW - gap);
        float formH = stacked ? Math.Max(280f, availH * 0.52f) : availH;
        float stageH = stacked ? Math.Max(240f, availH - formH - gap) : availH;

        _formCard.Transform.SetAbsoluteFrame(ox + pad, oy + pad, formW, formH);
        if (stacked)
            _stageCard.Transform.SetAbsoluteFrame(ox + pad, oy + pad + formH + gap, stageW, stageH);
        else
            _stageCard.Transform.SetAbsoluteFrame(ox + pad + formW + gap, oy + pad, stageW, stageH);

        LayoutForm(formW, formH);
        LayoutStage(stageW, stageH);
    }

    private void LayoutForm(float cardW, float cardH)
    {
        float inner = 16f;
        float contentW = Math.Max(80f, cardW - inner * 2 - 10f);
        float y = 14f;

        void Place(VisualElement el, float h)
        {
            el.Transform.SetLocalFrame(inner, y, contentW, h);
            y += h + 6f;
        }

        Place(_formTitle, 16f);
        Place(_inputLabel, 14f);
        Place(_inputField, 34f);
        Place(_inputMeta, 14f);
        if (_inputError.Visible) Place(_inputError, 14f);

        y += 6f;
        Place(_xformTitle, 16f);
        Place(_rotYLabel, 14f);
        Place(_rotYSlider, 22f);
        Place(_rotXLabel, 14f);
        Place(_rotXSlider, 22f);
        Place(_perspLabel, 14f);
        Place(_perspSlider, 22f);
        Place(_scaleLabel, 14f);
        Place(_scaleSlider, 22f);
        Place(_rollLabel, 14f);
        Place(_rollSlider, 22f);

        y += 6f;
        Place(_lookTitle, 16f);
        Place(_roundnessLabel, 14f);
        Place(_roundnessSlider, 22f);
        Place(_opacityLabel, 14f);
        Place(_opacitySlider, 22f);
        Place(_darkSwitch, 26f);
        Place(_glowSwitch, 26f);
        Place(_blurSwitch, 26f);
        Place(_aaSwitch, 26f);
        Place(_animateGridBtn, 32f);
        Place(_gridSpeedLabel, 14f);
        Place(_gridSpeedSlider, 22f);

        y += 6f;
        Place(_btnLabel, 16f);
        float btnW = Math.Max(70f, (contentW - 16f) / 3f);
        float btnY = y;
        _primaryBtn.Transform.SetLocalFrame(inner, btnY, btnW, 32f);
        _depthBtn.Transform.SetLocalFrame(inner + btnW + 8f, btnY, btnW, 32f);
        _dangerBtn.Transform.SetLocalFrame(inner + (btnW + 8f) * 2, btnY, btnW, 32f);
        y += 40f;
        Place(_checkbox, 24f);
        Place(_batchResetBtn, 34f);

        _formCard.SetContentSize(cardW, y + 16f);
    }

    private void LayoutStage(float cardW, float cardH)
    {
        float x = _stageCard.Transform.Computed.X;
        float y = _stageCard.Transform.Computed.Y;
        float inner = 16f;
        float contentW = Math.Max(80f, cardW - inner * 2);
        float cx = x + inner;

        _stageTitle.Transform.SetAbsoluteFrame(cx, y + 14f, contentW, 16f);

        float inspectorH = 128f;
        float wellTop = y + 38f;
        float wellH = Math.Max(160f, cardH - 38f - inspectorH - 24f);
        _stageWell.Transform.SetAbsoluteFrame(cx, wellTop, contentW, wellH);

        float cardMaxW = Math.Min(420f, contentW - 48f);
        float cardMaxH = Math.Min(220f, wellH - 56f);
        float liveW = Math.Max(220f, cardMaxW);
        float liveH = Math.Max(140f, cardMaxH);
        float liveX = cx + (contentW - liveW) / 2f;
        float liveY = wellTop + (wellH - liveH) / 2f - 6f;
        _liveCard.Transform.SetAbsoluteFrame(liveX, liveY, liveW, liveH);

        _previewHeading.Transform.SetAbsoluteFrame(liveX + 18f, liveY + 16f, Math.Max(40f, liveW - 36f), 28f);
        _previewSub.Transform.SetAbsoluteFrame(liveX + 18f, liveY + 48f, Math.Max(40f, liveW - 36f), 18f);
        _hitBtn.Transform.SetAbsoluteFrame(liveX + 18f, liveY + 78f, 110f, 32f);
        _hitLabel.Transform.SetAbsoluteFrame(liveX + 18f, liveY + liveH - 28f, Math.Max(40f, liveW - 36f), 16f);

        _frosted.Transform.SetAbsoluteFrame(cx + 12f, wellTop + 12f, 132f, 28f);
        _frostedText.Transform.SetAbsoluteFrame(cx + 12f, wellTop + 16f, 132f, 18f);

        float insY = wellTop + wellH + 10f;
        _inspectorBox.Transform.SetAbsoluteFrame(cx, insY, contentW, inspectorH);
        _inspectorTitle.Transform.SetAbsoluteFrame(cx + 14f, insY + 10f, contentW - 28f, 14f);
        _stateRow1.Transform.SetAbsoluteFrame(cx + 14f, insY + 32f, contentW - 28f, 16f);
        _stateRow2.Transform.SetAbsoluteFrame(cx + 14f, insY + 52f, contentW - 28f, 16f);
        _stateRow3.Transform.SetAbsoluteFrame(cx + 14f, insY + 72f, contentW - 28f, 16f);
        _stateRow4.Transform.SetAbsoluteFrame(cx + 14f, insY + 92f, contentW - 28f, 16f);
    }

    private static Container Panel(string name)
    {
        var panel = new Container(new SKColor(38, 38, 38), 10f) { Name = name };
        panel.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(58, 58, 58), Roundness = 10f };
        return panel;
    }

    private static VisualElement SectionLabel(string text, SKColor color)
    {
        var el = new VisualElement { Text = text, IsClickthrough = true };
        el.Style.Text = new TextStyle { Color = color, Size = 11f, Weight = 700, Alignment = TextAlign.Left };
        return el;
    }

    private static VisualElement BodyLabel(string text)
    {
        var el = new VisualElement { Text = text, IsClickthrough = true };
        el.Style.Text = BodyText();
        return el;
    }

    private static VisualElement BindLabel(Func<string> text)
    {
        var el = new VisualElement();
        el.Style.Text = BodyText();
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

    private static VisualElement InspectRow(SKColor color, Func<string> text)
    {
        var el = new VisualElement();
        el.Style.Text = new TextStyle { Color = color, Size = 12f, Alignment = TextAlign.Left };
        el.BindText(text);
        return el;
    }

    private static TextStyle BodyText() => new()
    {
        Color = new SKColor(163, 163, 163),
        Size = 12f,
        Weight = 500,
        Alignment = TextAlign.Left
    };

    private sealed class FormColumn : ScrollContainer
    {
        public FormColumn()
        {
            OverflowX = OverflowMode.Clip;
            OverflowY = OverflowMode.Scroll;
            ScrollbarVisibilityX = ScrollbarVisibility.Hidden;
            Style.Shadow = null!;
        }

        protected override void LayoutChildren()
        {
            base.LayoutChildren();
        }
    }
}
