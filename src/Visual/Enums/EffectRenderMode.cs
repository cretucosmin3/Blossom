namespace Blossom.Core.Visual;

public enum EffectRenderMode
{
    /// <summary>
    /// Re-evaluates and renders the effect on every frame. Use for time-based animated shaders.
    /// </summary>
    Continuous,

    /// <summary>
    /// Renders the effect once and caches the result. It is only re-evaluated when the element changes.
    /// </summary>
    OnDemand
}
