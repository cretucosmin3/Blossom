using System;

namespace Blossom.Animations;

/// <summary>
/// Easing curves: each maps linear progress 0..1 to eased progress. Any <c>Func&lt;float, float&gt;</c>
/// works where an ease is taken, so a custom curve is just a lambda. Some curves (<see cref="OutBack"/>,
/// <see cref="OutElastic"/>) overshoot past 1 on purpose.
/// </summary>
public static class Ease
{
    public static float Linear(float t) => t;

    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
    public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;

    public static float InCubic(float t) => t * t * t;
    public static float OutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);
    public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;

    public static float InQuart(float t) => t * t * t * t;
    public static float OutQuart(float t) => 1f - MathF.Pow(1f - t, 4f);
    public static float InOutQuart(float t) => t < 0.5f ? 8f * t * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 4f) / 2f;

    public static float InExpo(float t) => t <= 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);
    public static float OutExpo(float t) => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);

    public static float InSine(float t) => 1f - MathF.Cos(t * MathF.PI / 2f);
    public static float OutSine(float t) => MathF.Sin(t * MathF.PI / 2f);
    public static float InOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f;

    private const float BackC1 = 1.70158f;
    private const float BackC2 = BackC1 * 1.525f;
    private const float BackC3 = BackC1 + 1f;

    /// <summary>Pulls back slightly before moving forward.</summary>
    public static float InBack(float t) => BackC3 * t * t * t - BackC1 * t * t;

    /// <summary>Overshoots the target slightly, then settles.</summary>
    public static float OutBack(float t) => 1f + BackC3 * MathF.Pow(t - 1f, 3f) + BackC1 * MathF.Pow(t - 1f, 2f);

    public static float InOutBack(float t) => t < 0.5f
        ? MathF.Pow(2f * t, 2f) * ((BackC2 + 1f) * 2f * t - BackC2) / 2f
        : (MathF.Pow(2f * t - 2f, 2f) * ((BackC2 + 1f) * (t * 2f - 2f) + BackC2) + 2f) / 2f;

    /// <summary>Springs past the target and wobbles into place.</summary>
    public static float OutElastic(float t) => t <= 0f ? 0f : t >= 1f ? 1f
        : MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * (2f * MathF.PI / 3f)) + 1f;

    /// <summary>Lands and bounces like a dropped ball.</summary>
    public static float OutBounce(float t)
    {
        const float n1 = 7.5625f, d1 = 2.75f;
        if (t < 1f / d1)
            return n1 * t * t;
        if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    public static float InBounce(float t) => 1f - OutBounce(1f - t);

    /// <summary>Jumps in <paramref name="count"/> equal steps (a ticking counter, a sprite flipbook).</summary>
    public static Func<float, float> Steps(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count));
        return t => t >= 1f ? 1f : MathF.Floor(t * count) / count;
    }

    /// <summary>A CSS-style <c>cubic-bezier(x1, y1, x2, y2)</c> curve.</summary>
    public static Func<float, float> CubicBezier(float x1, float y1, float x2, float y2)
    {
        if (x1 < 0f || x1 > 1f || x2 < 0f || x2 > 1f)
            throw new ArgumentOutOfRangeException(nameof(x1), "x1 and x2 must be within 0..1.");

        float Bezier(float a, float b, float s) => 3f * a * (1 - s) * (1 - s) * s + 3f * b * (1 - s) * s * s + s * s * s;
        float Slope(float a, float b, float s) => 3f * a * (1 - s) * (1 - s) + 6f * (b - a) * (1 - s) * s + 3f * (1 - b) * s * s;

        return t =>
        {
            if (t <= 0f || t >= 1f)
                return t;
            // Solve x(s) = t with Newton steps, falling back to bisection where the slope is flat.
            float s = t;
            for (int i = 0; i < 6; i++)
            {
                float dx = Bezier(x1, x2, s) - t;
                if (MathF.Abs(dx) < 1e-5f)
                    return Bezier(y1, y2, s);
                float slope = Slope(x1, x2, s);
                if (MathF.Abs(slope) < 1e-6f)
                    break;
                s -= dx / slope;
            }

            float lo = 0f, hi = 1f;
            s = t;
            for (int i = 0; i < 24; i++)
            {
                float x = Bezier(x1, x2, s);
                if (MathF.Abs(x - t) < 1e-5f)
                    break;
                if (x < t) lo = s; else hi = s;
                s = (lo + hi) / 2f;
            }
            return Bezier(y1, y2, s);
        };
    }

    /// <summary>Plays <paramref name="ease"/> backwards in time: an ease-out becomes the matching ease-in.</summary>
    public static Func<float, float> Reverse(Func<float, float> ease) => t => 1f - ease(1f - t);
}
