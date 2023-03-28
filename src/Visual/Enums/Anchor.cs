using System;

namespace Blossom.Core.Visual
{
    [Flags]
    public enum Anchor
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Horizontal = Left | Right,
        Vertical = Top | Bottom
    }
}