using System;

namespace Kara.Core.Visual
{
	[Flags]
	public enum Anchor : int
	{
		Top = 0,
		Bottom = 1,
		Left = 2,
		Right = 4,
		None = 8,
		All = Top | Bottom | Left | Right,
	}
}