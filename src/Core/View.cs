using System.Diagnostics;
using System.Net;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Drawing;
using System;
using SilkyNvg;
using SilkyNvg.Images;
using SilkyNvg.Text;
using Silk.NET.Input;
using Kara.Core.Visual;
using Kara.Core.Input;
using System.Collections.Generic;
using Kara.Utils;
using SilkyNvg.Graphics;
using SilkyNvg.Paths;
using StbImageSharp;

namespace Kara.Core
{
	public abstract class View : IDisposable
	{
		public EventMap Events = new();
		public ElementsMap Elements = new();

		private int DefaultFont;
		private string _title = "";
		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				//! #render title only
			}
		}

		private string _name = "";
		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				//! #render title only
			}
		}

		internal Application ParentApp { get; set; }
		public VisualElement FocusedElement { get; set; }
		public VisualElement Element;

		public void AddElement(VisualElement element)
		{
			Elements.AddElement(ref element, this);
		}

		public void RemoveElement(VisualElement element)
		{
			Elements.RemoveElement(element);
		}

		internal void Render()
		{
			Element.Draw();
		}

		public void Dispose()
		{

		}
	}
}