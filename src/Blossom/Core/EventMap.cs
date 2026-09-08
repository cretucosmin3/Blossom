using System.Collections.Generic;
using System;
using Silk.NET.Input;
using Blossom.Core.Delegates.Inputs;
using Blossom.Utils;
using System.Numerics;
using Blossom.Core.Visual;

namespace Blossom.Core.Input;

public class EventMap : IDisposable
{
    // TODO: manage events by access
    public EventAccess Access = EventAccess.All;

    private readonly List<Key> CtrlKeys = new()
    {
        Key.ControlLeft,
        Key.ControlRight,
        Key.AltLeft,
        Key.AltRight
    };

    private bool IsCommand = false;
    private readonly List<Key> KeySequence = new();
    private readonly Dictionary<string, Hotkey> Hotkeys = new();
    private readonly DateTime[] lastClicks = new DateTime[15];
    private readonly bool[] wasDoubleClick = new bool[15];
    private readonly bool[] keysDown = new bool[20];
    private readonly HashSet<Key> _keysDown = new();

    public bool IsKeyDown(Key key) => _keysDown.Contains(key);
    public bool IsShiftDown => _keysDown.Contains(Key.ShiftLeft) || _keysDown.Contains(Key.ShiftRight);
    public bool IsControlDown => _keysDown.Contains(Key.ControlLeft) || _keysDown.Contains(Key.ControlRight);
    public bool IsAltDown => _keysDown.Contains(Key.AltLeft) || _keysDown.Contains(Key.AltRight);

    public int DoubleClickTime = 200;

    // Keyboard
    public event Action<int> OnKeyDown;
    public event Action<int> OnKeyUp;
    public event Action<char> OnKeyType;
    public event ForHotkey OnHotkey;

    // Mouse
    public event Action<object, MouseEventArgs> OnMouseMove;
    public event Action<object, Vector2> OnMouseScroll;
    public event Action<object, MouseScrollEventArgs> OnScroll;
    public event Action<object, MouseEventArgs> OnMouseDown;
    public event Action<object, MouseEventArgs> OnMouseUp;
    public event Action<object, MouseEventArgs> OnClick;

    [Obsolete("Use OnClick instead.")]
    public event Action<object, MouseEventArgs> OnMouseClick
    {
        add => OnClick += value;
        remove => OnClick -= value;
    }

    public event Action<object, MouseEventArgs> OnMouseDoubleClick;

    /// <summary>
    /// Register series of keys to one event
    /// </summary>
    public Hotkey AddHotkey(Key[] keybind, string id = "")
    {
        Arr.Map(keybind, out int[] karr);
        Array.Sort(karr);

        string StringKey = string.Join(':', karr);

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
        _keysDown.Add(key);
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

            Arr.Map<int>(KeySequence.ToArray(), out int[] karr);

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
                OnKeyDown.Invoke((int)key);
                FoundEvent = true;
            }
        }

        return FoundEvent;
    }

    internal void HandleKeyUp(Key key, int i)
    {
        _keysDown.Remove(key);
        if (CtrlKeys.Contains(key))
        {
            IsCommand = false;
            KeySequence.Clear();
        }

        if (IsCommand && !CtrlKeys.Contains(key))
        {
            KeySequence.Remove(key);
        }
        else
        {
            OnKeyUp?.Invoke((int)key);
        }
    }

    internal void HandleKeyChar(char ch) =>
        OnKeyType?.Invoke(ch);

    #endregion

    #region Mouse
    internal void HandleMouseMove(MouseEventArgs args, VisualElement el = default)
    {
        OnMouseMove?.Invoke(el, args);
    }

    internal void HandleMouseMove(Vector2 pos, VisualElement el = default)
    {
        var relative = el != null ? el.PointToClient(pos.X, pos.Y) : pos;
        HandleMouseMove(new MouseEventArgs
        {
            Global = pos,
            Relative = relative
        }, el);
    }

    internal void HandleMouseDown(MouseEventArgs args, VisualElement target = default)
    {
        if (args.Button >= 0 && args.Button < keysDown.Length)
            keysDown[args.Button] = true;
        OnMouseDown?.Invoke(target, args);
    }

    internal void HandleMouseDown(int btn, Vector2 pos, VisualElement target = default)
    {
        var relative = target != null ? target.PointToClient(pos.X, pos.Y) : pos;
        HandleMouseDown(new MouseEventArgs
        {
            Button = btn,
            Global = pos,
            Relative = relative
        }, target);
    }

    internal void HandleMouseUp(MouseEventArgs args, VisualElement target = default)
    {
        if (args.Button >= 0 && args.Button < keysDown.Length)
            keysDown[args.Button] = false;
        OnMouseUp?.Invoke(target, args);
    }

    internal void HandleMouseUp(int btn, Vector2 pos, VisualElement target = default)
    {
        var relative = target != null ? target.PointToClient(pos.X, pos.Y) : pos;
        HandleMouseUp(new MouseEventArgs
        {
            Button = btn,
            Global = pos,
            Relative = relative
        }, target);
    }

    internal void HandleClick(MouseEventArgs args, VisualElement target = default)
    {
        OnClick?.Invoke(target, args);

        DateTime now = DateTime.Now;
        int btn = args.Button;
        if (btn >= 0 && btn < lastClicks.Length)
        {
            bool isWithinTimeWindow = DateTime.Now - lastClicks[btn] < TimeSpan.FromMilliseconds(DoubleClickTime);
            bool isDoubleClick = isWithinTimeWindow && !wasDoubleClick[btn];

            if (isDoubleClick)
            {
                OnMouseDoubleClick?.Invoke(target, args);
            }

            wasDoubleClick[btn] = isDoubleClick;
            lastClicks[btn] = now;
        }
    }

    internal void HandleMouseScroll(Vector2 pos, VisualElement target = default, MouseScrollEventArgs args = null)
    {
        OnMouseScroll?.Invoke(target, pos);
        if (args != null)
        {
            OnScroll?.Invoke(target, args);
        }
    }
    #endregion

    internal bool IsMouseDown(int key) => key >= 0 && key < keysDown.Length && keysDown[key];

    public void Dispose()
    {
        Hotkeys.Clear();
    }
}

public class MouseEventArgs
{
    public int Button { get; set; }
    public Vector2 Global { get; set; }
    public Vector2 Relative { get; set; }
    public bool Handled { get; set; }
}

public class MouseScrollEventArgs
{
    public Vector2 Offset { get; set; }
    public Vector2 Global { get; set; }
    public bool Handled { get; set; }
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
    private readonly EventMap Parent;
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