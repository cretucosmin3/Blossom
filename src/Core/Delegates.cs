using System.Numerics;
using Rux.Core.Input;

namespace Rux.Core.Delegates.Window
{
    public delegate void ForLoad();
    public delegate void ForResize(int w, int h);
    public delegate void ForState(int state);
}

namespace Rux.Core.Delegates.Inputs
{
    public delegate void ForKey(int key);
    public delegate void ForChar(char c);
    public delegate void ForHotkey(Hotkey hotkey);
    public delegate void ForPosition(Vector2 pos);
    public delegate void ForMouseButton(int btn, Vector2 pos);
}

namespace Rux.Core.Delegates.Common
{
    public delegate void ForVoid();
    public delegate void ForString(string val);
    public delegate void ForInt(int val);
    public delegate void ForFloat(float val);
    public delegate void ForV3(float x, float y, float z);
    public delegate void ForV4(float x, float y, float w, float h);
}

namespace Rux.Core.Visual
{
    public delegate void ForDispose(VisualElement e);
}