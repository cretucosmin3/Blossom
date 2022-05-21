using System;

namespace Rux.Core.Visual
{
    [Flags]
    public enum Anchor : int
    {
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
    }
}