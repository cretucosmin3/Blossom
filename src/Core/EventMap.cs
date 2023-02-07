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

    public int DoubleClickTime = 200;

    // Keyboard
    public event Action<int> OnKeyDown;
    public event Action<int> OnKeyUp;
    public event Action<char> OnKeyType;
    public event ForHotkey OnHotkey;

    // Mouse
    public event Action<object, MouseEventArgs> OnMouseMove;
    public event Action<object, Vector2> OnMouseScroll;
    public event Action<object, MouseEventArgs> OnMouseDown;
    public event Action<object, MouseEventArgs> OnMouseUp;
    public event Action<object, MouseEventArgs> OnMouseClick;
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

        OnMouseMove?.Invoke(el, new()
        {
            Global = pos,
            Relative = relative
        });
    }

    internal void HandleMouseDown(int btn, Vector2 pos, VisualElement target = default)
    {
        keysDown[btn] = true;
        var relative = target != null ? target.PointToClient(pos.X, pos.Y) : pos;
        OnMouseDown?.Invoke(target, new()
        {
            Button = btn,
            Global = pos,
            Relative = relative
        });

        OnMouseClick?.Invoke(target, new()
        {
            Button = btn,
            Global = pos,
            Relative = relative
        });

        DateTime now = DateTime.Now;
        bool isWithinTimeWindow = DateTime.Now - lastClicks[btn] < TimeSpan.FromMilliseconds(DoubleClickTime);
        bool isDoubleClick = isWithinTimeWindow && !wasDoubleClick[btn];

        if (isDoubleClick)
        {
            OnMouseDoubleClick?.Invoke(target, new()
            {
                Button = btn,
                Global = pos,
                Relative = relative
            });
        }

        wasDoubleClick[btn] = isDoubleClick;

        lastClicks[btn] = now;
    }

    internal void HandleMouseUp(int btn, Vector2 pos, VisualElement target = default)
    {
        keysDown[btn] = false;
        var relative = target != null ? target.PointToClient(pos.X, pos.Y) : pos;
        OnMouseUp?.Invoke(target, new()
        {
            Button = btn,
            Global = pos,
            Relative = relative
        });
    }

    internal void HandleMouseScroll(Vector2 pos, VisualElement target = default) =>
        OnMouseScroll?.Invoke(target, pos);
    #endregion

    internal bool isMouseDown(int key) => keysDown[key];

    public void Dispose()
    {
        Hotkeys.Clear();
    }
}

public struct MouseEventArgs
{
    public int Button;
    public Vector2 Global;
    public Vector2 Relative;
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