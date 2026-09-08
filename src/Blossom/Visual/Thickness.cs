using System;

namespace Blossom.Core.Visual;

public struct Thickness : IEquatable<Thickness>
{
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }

    public Thickness(float uniform)
    {
        Left = Top = Right = Bottom = uniform;
    }

    public Thickness(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    public Thickness(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public static implicit operator Thickness(float uniform) => new(uniform);

    public bool Equals(Thickness other) =>
        Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override bool Equals(object? obj) => obj is Thickness other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);
    public static bool operator !=(Thickness left, Thickness right) => !left.Equals(right);

    public override string ToString() => $"Thickness({Left}, {Top}, {Right}, {Bottom})";
}
