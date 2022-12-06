using System.Reflection.Metadata;
namespace Rux.Core.Visual;

public class StyleProperty
{
    internal ElementStyle StyleContext;

    internal void TriggerRender()
    {
        this.StyleContext?.ScheduleRender();
    }
}