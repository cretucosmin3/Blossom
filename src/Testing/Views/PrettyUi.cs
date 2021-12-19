using System;
using System.Diagnostics;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;
using System.Threading;

namespace Kara.Testing
{
	public class PrettyUi : View
	{
		float time = 3000f; // seconds
		float from = 200;
		float to = 400;
		bool moveTo = true;

		VisualElement parent;
		VisualElement CloseButton;
		VisualElement SearchBox;
		VisualElement SearchButton;
		VisualElement AnimatedParent;
		VisualElement TopLeft;
		VisualElement TopRight;
		VisualElement BottomLeft;
		VisualElement BottomRight;

		public PrettyUi() : base("PrettyUi View") { }

		public override void Main()
		{
			Browser.ShowFps();
			parent = new VisualElement()
			{
				Name = "parent",
				Text = "",
				X = 0,
				Y = 0,
				Width = Browser.RenderRect.Width,
				Height = 40f,
				FontSize = 20,
				BackColor = Color.FromArgb(222, Color.Black),
				Anchor = Anchor.Top | Anchor.Left,
			};

			CloseButton = new VisualElement()
			{
				Name = "close button",
				Text = "X",
				X = 5,
				Y = 4,
				Width = 30f,
				Height = 32f,
				FontSize = 18,
				Roundness = 3f,
				BackColor = Color.DimGray,
				FontColor = Color.White,
				Anchor = Anchor.Top | Anchor.Left,
			};

			SearchBox = new VisualElement()
			{
				Name = "search",
				Text = "Search...",
				X = CloseButton.X + CloseButton.Width + 5,
				Y = 4,
				Width = 350f,
				Height = 32f,
				FontSize = 18,
				BorderWidth = 0.5f,
				Roundness = 2f,
				BorderColor = Color.DarkGray,
				BackColor = Color.White,
				FontColor = Color.FromArgb(222, Color.Black),
				TextAlignment = TextAlign.Left,
				TextPadding = 10,
				Anchor = Anchor.Top | Anchor.Left,
			};

			SearchButton = new VisualElement()
			{
				Name = "button",
				Text = "Go",
				X = SearchBox.X + SearchBox.Width + 5,
				Y = 4,
				Width = 42f,
				Height = 32f,
				FontSize = 18,
				Roundness = 2f,
				BackColor = Color.Aquamarine,
				FontColor = Color.Black,
				Anchor = Anchor.Top | Anchor.Left,
			};

			AnimatedParent = new VisualElement()
			{
				Name = "AnimatedParent",
				X = 20,
				Y = 75,
				Width = 200,
				Height = 200,
				Roundness = 2f,
				BorderWidth = 1f,
				BorderColor = Color.Black,
				BackColor = Color.AliceBlue,
				Anchor = Anchor.Top | Anchor.Left,
			};

			TopLeft = new VisualElement()
			{
				Name = "TopLeft",
				Text = "Hello World",
				X = 0,
				Y = 0,
				Width = 70,
				Height = 70,
				BorderWidth = 1f,
				BorderColor = Color.Black,
				FontColor = Color.Black,
				BackColor = Color.AliceBlue,
				Anchor = Anchor.Top | Anchor.Right | Anchor.Left | Anchor.Bottom,
			};

			TopRight = new VisualElement()
			{
				Name = "TopRight",
				X = 150,
				Y = 0,
				Width = 50,
				Height = 50,
				BorderWidth = 1f,
				BorderColor = Color.Black,
				BackColor = Color.AliceBlue,
				Anchor = Anchor.Top | Anchor.Right,
			};

			BottomLeft = new VisualElement()
			{
				Name = "BottomLeft",
				X = 0,
				Y = 150,
				Width = 50,
				Height = 50,
				BorderWidth = 1f,
				BorderColor = Color.Black,
				BackColor = Color.AliceBlue,
				Anchor = Anchor.Bottom | Anchor.Left,
			};

			BottomRight = new VisualElement()
			{
				Name = "BottomRight",
				X = 150,
				Y = 150,
				Width = 50,
				Height = 50,
				BorderWidth = 1f,
				BorderColor = Color.Black,
				BackColor = Color.AliceBlue,
				Anchor = Anchor.Bottom | Anchor.Right,
			};

			Elements.AddElement(ref parent, this);
			Elements.AddElement(ref CloseButton, this);
			Elements.AddElement(ref SearchBox, this);
			Elements.AddElement(ref SearchButton, this);

			Elements.AddElement(ref AnimatedParent, this);

			Elements.AddElement(ref TopLeft, this);
			Elements.AddElement(ref TopRight, this);
			Elements.AddElement(ref BottomLeft, this);
			Elements.AddElement(ref BottomRight, this);

			AnimatedParent.AddChild(TopLeft);
			AnimatedParent.AddChild(TopRight);
			AnimatedParent.AddChild(BottomLeft);
			AnimatedParent.AddChild(BottomRight);

			parent.AddChild(CloseButton);
			parent.AddChild(SearchBox);
			parent.AddChild(SearchButton);

			Loop += Update;
			watch.Start();
		}

		private Stopwatch watch = new Stopwatch();
		private void Update()
		{
			float progress = (float)watch.ElapsedMilliseconds / (time / 2f);
			float Cycleprogress = (float)watch.ElapsedMilliseconds / time;
			float newVal = moveTo ? smoothLerp(from, to, progress) : smoothLerp(to, from, progress);

			int alpha = (int)(moveTo ? smoothLerp(0, 255, progress) : smoothLerp(255, 0, progress));

			TopLeft.BackColor = Color.FromArgb(alpha, Color.LightYellow);
			TopLeft.FontColor = Color.FromArgb(alpha, Color.Black);
			TopRight.BackColor = Color.FromArgb(alpha, Color.RoyalBlue);
			BottomLeft.BackColor = Color.FromArgb(alpha, Color.YellowGreen);
			BottomRight.BackColor = Color.FromArgb(alpha, Color.Red);

			AnimatedParent.Width = newVal;
			AnimatedParent.Height = newVal;

			if (watch.ElapsedMilliseconds >= (time / 2f))
			{
				moveTo = !moveTo;
				watch.Reset();
				watch.Start();
			}
		}

		// create a function to smoothly interpolate between two values
		float smoothLerp(float from, float to, float progress)
		{
			return from + (to - from) * (progress * progress * (3 - 2 * progress));
		}

		float lerp(float a, float b, float f)
		{
			return a + f * (b - a);
		}
	}
}