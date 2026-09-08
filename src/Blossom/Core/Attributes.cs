using System;

namespace Blossom.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class BuilderPropertyAttribute : Attribute
{
    public string Label { get; }
    public string Category { get; }
    public float Min { get; }
    public float Max { get; }
    public float Step { get; }

    public BuilderPropertyAttribute(string label, string category = "General", float min = 0f, float max = 0f, float step = 1f)
    {
        Label = label;
        Category = category;
        Min = min;
        Max = max;
        Step = step;
    }
}
