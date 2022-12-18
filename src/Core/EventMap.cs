using System.Net;
using Microsoft.VisualBasic.CompilerServices;
using System.Text;
using System.Collections.Generic;
using System;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Blossom.Core.Delegates.Inputs;
using Blossom.Utils;
using System.Numerics;
using Blossom.Core.Visual;

namespace Blossom.Core.Input
{
    public class EventMap : IDisposable
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
        private DateTime[] lastClicks = new DateTime[15];
        private bool[] wasDoubleClick = new bool[15];

        public int DoubleClickTime = 200;

        // Keyboard
        public event Action<int> OnKeyDown;
        public event Action<int> OnKeyUp;
        public event Action<char> OnKeyType;
        public event ForHotkey OnHotkey;

        // Mouse
        public event Action<Vector2, Vector2> OnMouseMove;
        public event Action<Vector2> OnMouseScroll;
        public event Action<int, Vector2, Vector2> OnMouseDown;
        public event Action<int, Vector2, Vector2> OnMouseUp;
        public event Action<int, Vector2, Vector2> OnMouseClick;
        public event Action<int, Vector2, Vector2> OnMouseDoubleClick;

        /// <summary>
        /// Register series of keys to one event
        /// </summary>
        public Hotkey AddHotkey(Key[] keybind, string id = "")
        {
            int[] karr;
            Arr.Map<int>(keybind, out karr);
            Array.Sort(karr);

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

                // sort an array of integers
                Array.Sort(karr);

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

        internal void HandleKeyChar(char ch) =>
            OnKeyType?.Invoke(ch);

        #endregion

        #region Mouse
        internal void HandleMouseMove(Vector2 pos, VisualElement el = default)
        {
            var relative = el != null ? el.PointToClient(pos.X, pos.Y) : pos;
            OnMouseMove?.Invoke(pos, relative);
        }

        internal void HandleMouseDown(int btn, Vector2 pos, VisualElement el = default)
        {
            var relative = el != null ? el.PointToClient(pos.X, pos.Y) : pos;
            OnMouseDown?.Invoke(btn, pos, relative);
            OnMouseClick?.Invoke(btn, pos, relative);

            DateTime now = DateTime.Now;
            bool isDoubleClick = DateTime.Now - lastClicks[btn] < TimeSpan.FromMilliseconds(DoubleClickTime);

            if (isDoubleClick && !wasDoubleClick[btn])
            {
                OnMouseDoubleClick?.Invoke(btn, pos, relative);
                wasDoubleClick[btn] = true;
            }
            else
            {
                wasDoubleClick[btn] = false;
            }

            lastClicks[btn] = now;
        }

        internal void HandleMouseUp(int ButtonName, Vector2 pos, VisualElement el = default)
        {
            var relative = el != null ? el.PointToClient(pos.X, pos.Y) : pos;
            OnMouseUp?.Invoke(ButtonName, pos, relative);
        }

        internal void HandleMouseScroll(Vector2 pos) =>
            OnMouseScroll?.Invoke(pos);
        #endregion

        public void Dispose()
        {
            Hotkeys.Clear();
        }
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