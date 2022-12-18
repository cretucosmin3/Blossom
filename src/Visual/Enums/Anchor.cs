using System;

namespace Blossom.Core.Visual
{
    [Flags]
    public enum Anchor : int
    {
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Horizontal = Anchor.Left | Anchor.Right,
        Vertical = Anchor.Top | Bottom
    }
}