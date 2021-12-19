using System.Security.Principal;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;

namespace Kara.Testing
{
	public class Nesting : View
	{
		public Nesting() : base("Nesting View") { }

		public override void Main()
		{
			VisualElement parent = new VisualElement()
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

			VisualElement CloseButton = new VisualElement()
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

			VisualElement SearchBox = new VisualElement()
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

			VisualElement SearchButton = new VisualElement()
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

			Elements.AddElement(ref parent, this);
			Elements.AddElement(ref CloseButton, this);
			Elements.AddElement(ref SearchBox, this);
			Elements.AddElement(ref SearchButton, this);

			parent.AddChild(CloseButton);
			parent.AddChild(SearchBox);
			parent.AddChild(SearchButton);

			Loop += Update;
		}

		private void Update()
		{

		}
	}
}