namespace Blossom.Core.Visual.Enums;

/// <summary>How text that does not fit the element box is handled.</summary>
public enum TextOverflow
{
    /// <summary>Draw the full string; it may paint outside the box.</summary>
    Visible,
    /// <summary>Clip painting to the content box.</summary>
    Clip,
    /// <summary>Truncate with an ellipsis (…) so the visible text stays inside the box.</summary>
    Ellipsis,
    /// <summary>Wrap to the box width and keep every line (used with scrollable <see cref="RichBox"/>).</summary>
    Wrap
}
