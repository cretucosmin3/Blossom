using System;
using System.Reflection.Metadata;

namespace Blossom.Core.Visual;

public class StyleProperty
{
    internal ElementStyle StyleContext;
    internal Action OnChanged;

    internal void TriggerRender()
    {
        this.StyleContext?.ScheduleRender();
    }

    internal void TriggerChange() => OnChanged?.Invoke();
}