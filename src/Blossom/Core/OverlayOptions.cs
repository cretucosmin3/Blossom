using Blossom.Core.Visual;

namespace Blossom.Core;

/// <summary>
/// Behavior for a layer pushed with <see cref="View.PushOverlay"/>.
/// </summary>
public sealed class OverlayOptions
{
    /// <summary>
    /// When true (default), pointer hits do not fall through to the app under this layer (modal).
    /// Menus typically use <c>false</c> with <see cref="CloseOnPointerOutside"/>.
    /// </summary>
    public bool BlockHitsUnderneath { get; set; } = true;

    /// <summary>
    /// When true, a pointer-down outside this layer pops/disposes it.
    /// </summary>
    public bool CloseOnPointerOutside { get; set; }

    /// <summary>
    /// Keyboard target to restore when the overlay is popped. When null, the view
    /// restores the element that was active at push time.
    /// </summary>
    public VisualElement? RestoreKeyboardTo { get; set; }
}
