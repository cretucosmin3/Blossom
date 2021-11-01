namespace Kara.Core.Delegates.Window
{
	// Window
	public delegate void ForLoad();
	public delegate void ForResize(int w, int h);
	public delegate void ForState(int state);
}

namespace Kara.Core.Delegates.Inputs
{
	public delegate void ForKey(int key);
	public delegate void ForChar(char k);
	public delegate void ForKeybind(int[] keybind);
	public delegate void ForPosition(int x, int y);
}

namespace Kara.Core.Delegates.Common
{
	public delegate void ForString(string val);
	public delegate void ForInt(int val);
	public delegate void ForFloat(float val);
}