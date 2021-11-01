using Microsoft.VisualBasic.CompilerServices;
using System.Text;
using System.Collections.Generic;
using System;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Kara.Core.Delegates.Inputs;
using Kara.Utils;

namespace Kara.Core
{
	internal class EventMap
	{
		private List<Key> CtrlKeys = new List<Key> {
			Key.ControlLeft,
			Key.ControlRight,
			Key.AltLeft,
			Key.AltRight
		};

		private bool IsCommand = false;
		private List<Key> KeySequence = new List<Key>();
		private Dictionary<string, Action> Keybindings = new Dictionary<string, Action>();

		/// <summary>
		/// Register series of keys to one event
		/// </summary>
		public void AddKeybind(Key[] keybind, Action action = null)
		{
			Console.WriteLine($"Registered keybind {string.Join(",", keybind)}");

			int[] karr;
			Arr.Map<int>(keybind, out karr);

			string StringKey = String.Join(':', karr);

			if (!Keybindings.ContainsKey(StringKey))
			{
				Keybindings.Add(StringKey, action);
			}
			else
			{
				Console.WriteLine("");
			}
		}

		#region Keyboard
		private void Handle_Key_Down(IKeyboard _, Key key, int i)
		{
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

				string StringKey = String.Join(':', karr);

				if (Keybindings.ContainsKey(StringKey))
				{
					var method = Keybindings[StringKey];
					if (method != null) method.Invoke();
					else Handle_Keybind(KeySequence.ToArray());
				}
			}
		}

		private void Handle_Key_Up(IKeyboard _, Key key, int i)
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
		}

		private void Handle_Keybind(Key[] keybind)
		{

		}

		private void Handle_Key_Char(IKeyboard _, char ch)
		{
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

		internal void GetFromWindow(IWindow window)
		{
			IInputContext input = window.CreateInput();

			// Register keyboard events
			foreach (IKeyboard keyboard in input.Keyboards)
			{
				keyboard.KeyDown += Handle_Key_Down;
				keyboard.KeyUp += Handle_Key_Up;
				keyboard.KeyChar += Handle_Key_Char;
			}

			// Register mouse events
			foreach (IMouse mouse in input.Mice)
			{
				mouse.MouseMove += Handle_Mouse_Move;

				mouse.Click += Handle_Mouse_Click;
				mouse.DoubleClick += Handle_Mouse_Double_Click;

				mouse.MouseDown += Handle_Mouse_Down;
				mouse.MouseUp += Handle_Mouse_Up;

				mouse.Scroll += Handle_Mouse_Scroll;
			}
		}
	}
}