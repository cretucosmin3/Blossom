using Kara.Core.Input;

namespace Kara.Core.Delegates.Window
{
	public delegate void ForLoad();
	public delegate void ForResize(int w, int h);
	public delegate void ForState(int state);
}

namespace Kara.Core.Delegates.Inputs
{
	public delegate void ForKey(int key);
	public delegate void ForChar(char c);
	public delegate void ForHotkey(Hotkey hotkey);
	public delegate void ForPosition(int x, int y);
}

namespace Kara.Core.Delegates.Common
{
	public delegate void ForVoid();
	public delegate void ForString(string val);
	public delegate void ForInt(int val);
	public delegate void ForFloat(float val);
	public delegate void ForV3(float x, float y, float z);
	public delegate void ForV4(float x, float y, float w, float h);
}

namespace Kara.Core.Visual
{
	public delegate void ForDispose(VisualElement e);
}