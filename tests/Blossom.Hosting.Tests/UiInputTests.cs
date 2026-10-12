using System.Numerics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Hosting;
using SkiaSharp;
using Xunit;

namespace Blossom.Hosting.Tests;

public class UiInputTests
{
    [Fact]
    public void PointerClick_DispatchesToTargetElement_WhenDownAndUpOnSameElement()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            int clicks = 0;
            button.Events.OnClick += (sender, args) => clicks++;

            // Pointer down and up inside button bounds (10, 10 to 40, 30)
            session.Input.PointerDown(20, 20, 0);
            session.Input.PointerUp(20, 20, 0);

            Assert.Equal(1, clicks);
        }
    }

    [Fact]
    public void PointerClick_DoesNotDispatch_WhenPointerUpOnDifferentElement()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            int clicks = 0;
            button.Events.OnClick += (sender, args) => clicks++;

            // Pointer down on button
            session.Input.PointerDown(20, 20, 0);
            // Pointer up far away outside button bounds
            session.Input.PointerUp(70, 70, 0);

            Assert.Equal(0, clicks);
        }
    }

    [Fact]
    public void PointerCapture_RetainedAcrossPointerLeave_WhenMouseButtonHeld()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            // Acquire pointer capture on mouse down
            button.Events.OnMouseDown += (sender, args) => view.SetPointerCapture(button);

            session.Input.PointerDown(20, 20, 0);
            Assert.Same(button, view.PointerCaptureElement);

            // Pointer leaves viewport while primary button is down
            session.Input.PointerLeave();
            Assert.Same(button, view.PointerCaptureElement);

            // Mouse up outside releases capture
            session.Input.PointerUp(90, 90, 0);
            Assert.Null(view.PointerCaptureElement);
        }
    }

    [Fact]
    public void PointerCapture_RoutesMoveEvents_OutsideViewport()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            button.Events.OnMouseDown += (sender, args) => view.SetPointerCapture(button);

            Vector2 lastPos = Vector2.Zero;
            button.Events.OnMouseMove += (sender, args) => lastPos = args.Global;

            session.Input.PointerDown(20, 20, 0);
            // Move outside viewport while captured
            session.Input.PointerMove(150, 200);

            Assert.Equal(new Vector2(150, 200), lastPos);

            session.Input.PointerUp(150, 200, 0);
            Assert.Null(view.PointerCaptureElement);
        }
    }

    [Fact]
    public void KeyDown_And_KeyUp_DispatchedToFocusedElement()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            view.SetActiveKeyboardElement(button);
            Assert.Same(button, view.ActiveKeyboardElement);

            UiKey? downKey = null;
            UiKey? upKey = null;

            button.Events.OnKeyDown += e => downKey = e.Key;
            button.Events.OnKeyUp += e => upKey = e.Key;

            session.Input.KeyDown(UiKey.Enter);
            session.Input.KeyUp(UiKey.Enter);

            Assert.Equal(UiKey.Enter, downKey);
            Assert.Equal(UiKey.Enter, upKey);
        }
    }

    [Fact]
    public void Text_DispatchedToFocusedElement()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            view.SetActiveKeyboardElement(button);

            string receivedText = string.Empty;
            button.Events.OnTextInput += e => receivedText = e.Text;

            session.Input.Text("Hello Blossom");

            Assert.Equal("Hello Blossom", receivedText);
        }
    }

    [Fact]
    public void Scroll_DispatchedToHoveredElement()
    {
        var (session, view, button) = CreateTestEnvironment();
        using (session)
        {
            Vector2 scrollDelta = Vector2.Zero;
            button.Events.OnScroll += (sender, args) => scrollDelta = args.Offset;

            // Move hover over button (at 20, 20)
            session.Input.PointerMove(20, 20);
            session.Input.Scroll(2f, -3f);

            Assert.Equal(new Vector2(2f, -3f), scrollDelta);
        }
    }

    static (UiSession Session, InputTestView View, VisualElement Button) CreateTestEnvironment()
    {
        var host = new SoftwareUiHost();
        var app = new InputTestApp();
        var session = new UiSession(host, app, new UiSessionOptions
        {
            PaintMode = PaintMode.FullFrame,
            ClearColor = SKColors.Transparent,
            ClearCanvas = true
        });

        session.SetSize(100, 100, 100, 100);
        session.Init();

        // Perform initial paint so hit-testing geometry and transforms are evaluated
        using var surface = SKSurface.Create(new SKImageInfo(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
        session.Paint(new RenderFrame(surface.Canvas, session.LogicalSize, session.FramebufferSize), force: true);

        return (session, app.Main, app.Main.Button);
    }

    sealed class InputTestApp : Application
    {
        public InputTestView Main { get; }

        public InputTestApp()
        {
            Main = new InputTestView();
            AddView(Main);
            SetActiveView(Main);
        }
    }

    sealed class InputTestView : View
    {
        public VisualElement Button { get; private set; } = null!;

        public InputTestView() : base("input-test") { }

        public override void Init()
        {
            var root = new VisualElement { Name = "root" };
            root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;

            Button = new VisualElement { Name = "button" };
            Button.Transform.Anchor = Anchor.Left | Anchor.Top;
            Button.Transform.LocalX = 10;
            Button.Transform.LocalY = 10;
            Button.Transform.Width = 30;
            Button.Transform.Height = 20;

            root.AddChild(Button);
            AddElement(root);
        }
    }
}
