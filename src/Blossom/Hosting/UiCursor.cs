namespace Blossom.Hosting;

/// <summary>
/// Standard mouse cursors. Numeric values match Silk.NET.Input.StandardCursor
/// so the desktop host can cast. Games map to their own cursor APIs.
/// </summary>
public enum UiCursor
{
    Default = 0,
    Arrow = 1,
    IBeam = 2,
    Crosshair = 3,
    Hand = 4,
    HResize = 5,
    VResize = 6,
    NwseResize = 7,
    NeswResize = 8,
    ResizeAll = 9,
    NotAllowed = 10,
    Wait = 11,
    WaitArrow = 12
}
