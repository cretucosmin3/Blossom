using Microsoft.VisualBasic.CompilerServices;
using System.Text;
using System.Collections.Generic;
using System;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Kara.Core.Delegates.Inputs;
using Kara.Utils;

namespace Kara.Core.Input
{
	public class EventMap
	{

		// TODO: manage events by access
		public EventAccess Access = EventAccess.All;

		private List<Key> CtrlKeys = new List<Key> {
			Key.ControlLeft,
			Key.ControlRight,
			Key.AltLeft,
			Key.AltRight
		};

		private bool IsCommand = false;
		private List<Key> KeySequence = new List<Key>();
		private Dictionary<string, Hotkey> Hotkeys = new Dictionary<string, Hotkey>();

		// Keyboard
		public event ForKey OnKeyDown;
		public event ForKey OnKeyUp;
		public event ForChar OnKeyType;
		public event ForHotkey OnHotkey;

		// Mouse
		public event ForPosition OnMouseMove;
		public event ForPosition OnMouseScroll;
		public event ForKey OnMouseDown;
		public event ForKey OnMouseUp;
		public event ForKey OnMouseClick;
		public event ForKey OnMouseDoubleClick;

		/// <summary>
		/// Register series of keys to one event
		/// </summary>
		public Hotkey AddHotkey(Key[] keybind, string id = "")
		{
			int[] karr;
			Arr.Map<int>(keybind, out karr);

			string StringKey = String.Join(':', karr);

			if (Hotkeys.ContainsKey(StringKey))
			{
				Log.Error($"Keybind {StringKey} already exists");
				OnHotkey -= Hotkeys[StringKey].Method;
				return Hotkeys[StringKey];
			}

			var newHotkey = new Hotkey(id, this);
			Hotkeys.Add(StringKey, newHotkey);

			return newHotkey;
		}

		#region Keyboard
		internal bool HandleKeyDown(Key key, int i)
		{
			bool FoundEvent = false;

			// Start keybind
			if (CtrlKeys.Contains(key))
			{
				IsCommand = true;
				KeySequence.Add(key);
			}

			// Check for keybind
			if (IsCommand && !CtrlKeys.Contains(key))
			{
				KeySequence.Add(key);

				int[] karr;
				Arr.Map<int>(KeySequence.ToArray(), out karr);

				string StringKey = string.Join(':', karr);

				if (Hotkeys.ContainsKey(StringKey))
				{
					var Hotkey = Hotkeys[StringKey];
					if (string.IsNullOrEmpty(Hotkey.Name))
					{
						Hotkey.Invoke();
						FoundEvent = true;
					}
					else
					{
						OnHotkey.Invoke(Hotkey);
						FoundEvent = true;
					}
				}
			}
			else
			{
				if (OnKeyDown != null)
				{
					OnKeyDown.Invoke(i);
					FoundEvent = true;
				}
			}

			return FoundEvent;
		}

		internal void HandleKeyUp(Key key, int i)
		{
			if (CtrlKeys.Contains(key))
			{
				IsCommand = false;
				KeySequence.Clear();
			}

			// Check for keybind
			if (IsCommand && !CtrlKeys.Contains(key))
			{
				KeySequence.Remove(key);
			}
			else
			{
				OnKeyUp?.Invoke(i);
			}
		}

		internal void HandleKeyChar(char ch)
		{
			OnKeyType?.Invoke(ch);
		}
		#endregion

		#region Mouse
		private void Handle_Mouse_Move(IMouse _, System.Numerics.Vector2 Position)
		{

		}

		private void Handle_Mouse_Down(IMouse _, MouseButton ButtonName)
		{

		}

		private void Handle_Mouse_Up(IMouse _, MouseButton ButtonName)
		{

		}

		private void Handle_Mouse_Click(IMouse _, MouseButton ButtonName, System.Numerics.Vector2 Position)
		{

		}

		private void Handle_Mouse_Double_Click(IMouse _, MouseButton ButtonName, System.Numerics.Vector2 Position)
		{

		}

		private void Handle_Mouse_Scroll(IMouse _, ScrollWheel Scroll)
		{

		}
		#endregion
	}

	public enum EventAccess
	{
		All,
		Keyboard,
		Mouse,
		Gamepad
	}

	public class Hotkey
	{
		public string Name { get; set; }
		private EventMap Parent;
		internal ForHotkey Method;

        /// <summary>
        /// Create new input event
        /// </summary>
        internal Hotkey(string name, EventMap map)
		{
			(Name, Parent) = (name, map);
		}

		/// <summary>
		/// Add an action to this event
		/// </summary>
		/// <param name="action">Action to handle</param>
		public void Handle(ForHotkey action)
		{
			if (!String.IsNullOrEmpty(Name))
				Parent.OnHotkey += action;

			Method = action;
		}

		internal void Invoke()
		{
			Method?.Invoke(this);
		}

		public override string ToString() => Name;
	}
}