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
	public abstract class Application : IDisposable
	{
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

		internal int OffsetX = Browser.RenderOffsetX;
		internal int OffsetY = Browser.RenderOffsetY;

		private Dictionary<string, View> Views = new();
		private string _ActiveView = "";
		public View ActiveView
		{
			get
			{
				if (Views.TryGetValue(_ActiveView, out var view))
					return view;

				return null;
			}
		}

		public void SetActiveView(string name)
		{
			if (Views.ContainsKey(name))
				_ActiveView = name;
			else
				Log.Error($"View {name} does not exist");
		}

		public void SetActiveView(View view) => SetActiveView(view.Name);

		public EventMap Events = new();

		public void AddView(View view)
		{
			if (!Views.ContainsKey(view.Name))
			{
				Views.Add(view.Name, view);
				view.ParentApp = this;
			}
			else
				Log.Error($"View with name {view.Name} already exists!");
		}

		public void RemoveView(View view)
		{
			if (Views.ContainsKey(view.Name))
			{
				Views.Remove(view.Name);
				view.Dispose();
			}
			else
				Log.Error($"View with name {view.Name} does not exist!");
		}

		internal void Render() => ActiveView?.Render();

		public void Dispose()
		{
			foreach (var view in Views.Values)
			{
				view.Dispose();
			}

			Views.Clear();
			Events.Dispose();
		}
	}
}