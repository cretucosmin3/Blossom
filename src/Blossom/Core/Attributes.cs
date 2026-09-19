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

/// <summary>
/// Enables a 3D press effect on a button, scaling it down (default: 0.95x) while held down and restoring it upon release.
/// Can be placed on a button class or property.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class Enable3DEffectAttribute : Attribute
{
    public bool Enabled { get; }
    public float Scale { get; }

    public Enable3DEffectAttribute(bool enabled = true, float scale = 0.95f)
    {
        Enabled = enabled;
        Scale = scale;
    }
}

/// <summary>
/// Synonym for <see cref="Enable3DEffectAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class Effect3DAttribute : Enable3DEffectAttribute
{
    public Effect3DAttribute(bool enabled = true, float scale = 0.95f)
        : base(enabled, scale)
    {
    }
}

