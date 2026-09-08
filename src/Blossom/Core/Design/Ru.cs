using System;
using System.Globalization;

namespace Blossom.Core.Design;

/// <summary>
/// Represents a length or coordinate expressed in Blossom Reference Units (design units) on a declared <see cref="DesignCanvas"/>.
/// <para>
/// 1 Ru = 1 unit on the design canvas of the active UI root. Layout calculations, positions, sizes, padding, and radii
/// are measured in design units and scaled to destination window/slot viewports by Blossom's fit pipeline.
/// </para>
/// </summary>
public readonly struct Ru : IEquatable<Ru>, IComparable<Ru>, IComparable<float>, IFormattable
{
    /// <summary>
    /// The scalar value in design units.
    /// </summary>
    public float Value { get; }

    public Ru(float value)
    {
        Value = value;
    }

    /// <summary>
    /// A zero-length design unit value.
    /// </summary>
    public static Ru Zero => new(0f);

    /// <summary>
    /// Creates a new <see cref="Ru"/> instance from a scalar float.
    /// </summary>
    public static Ru From(float value) => new(value);

    /// <summary>
    /// Creates a new <see cref="Ru"/> instance by converting physical device pixels under the current host fit scale.
    /// </summary>
    public static Ru FromPixels(float devicePixels, View? view = null) => new(Px.FromPixels(devicePixels, view));

    // Implicit conversions for ergonomic integration with float-based layout APIs
    public static implicit operator Ru(float value) => new(value);
    public static implicit operator Ru(int value) => new(value);
    public static implicit operator Ru(double value) => new((float)value);
    public static implicit operator float(Ru ru) => ru.Value;
    public static explicit operator int(Ru ru) => (int)ru.Value;
    public static explicit operator double(Ru ru) => ru.Value;

    // Arithmetic operators
    public static Ru operator +(Ru a, Ru b) => new(a.Value + b.Value);
    public static Ru operator +(Ru a, float b) => new(a.Value + b);
    public static Ru operator +(float a, Ru b) => new(a + b.Value);

    public static Ru operator -(Ru a, Ru b) => new(a.Value - b.Value);
    public static Ru operator -(Ru a, float b) => new(a.Value - b);
    public static Ru operator -(float a, Ru b) => new(a - b.Value);

    public static Ru operator -(Ru a) => new(-a.Value);

    public static Ru operator *(Ru a, Ru b) => new(a.Value * b.Value);
    public static Ru operator *(Ru a, float b) => new(a.Value * b);
    public static Ru operator *(float a, Ru b) => new(a * b.Value);

    public static Ru operator /(Ru a, Ru b) => new(a.Value / b.Value);
    public static Ru operator /(Ru a, float b) => new(a.Value / b);
    public static Ru operator /(float a, Ru b) => new(a / b.Value);

    // Relational operators
    public static bool operator ==(Ru a, Ru b) => Math.Abs(a.Value - b.Value) < 1e-6f;
    public static bool operator !=(Ru a, Ru b) => !(a == b);
    public static bool operator <(Ru a, Ru b) => a.Value < b.Value;
    public static bool operator >(Ru a, Ru b) => a.Value > b.Value;
    public static bool operator <=(Ru a, Ru b) => a.Value <= b.Value;
    public static bool operator >=(Ru a, Ru b) => a.Value >= b.Value;

    public static Ru Min(Ru a, Ru b) => new(Math.Min(a.Value, b.Value));
    public static Ru Max(Ru a, Ru b) => new(Math.Max(a.Value, b.Value));
    public static Ru Clamp(Ru value, Ru min, Ru max) => new(Math.Clamp(value.Value, min.Value, max.Value));

    public bool Equals(Ru other) => Math.Abs(Value - other.Value) < 1e-6f;
    public bool Equals(float other) => Math.Abs(Value - other) < 1e-6f;
    public override bool Equals(object? obj) => obj switch
    {
        Ru other => Equals(other),
        float f => Equals(f),
        _ => false
    };

    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Ru other) => Value.CompareTo(other.Value);
    public int CompareTo(float other) => Value.CompareTo(other);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture) + " ru";
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider) + " ru";
}
