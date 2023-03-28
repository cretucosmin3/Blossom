using System;
using System.Drawing;
using System.Numerics;

namespace Blossom.Core.Visual;

public class Rect
{
    private RectangleF _Rect;
    public RectangleF RectF => _Rect;

    public float X
    {
        get => _Rect.X;
        set => _Rect.X = value;
    }

    public float Y
    {
        get => _Rect.Y;
        set => _Rect.Y = value;
    }

    public float Width
    {
        get => _Rect.Width;
        set => _Rect.Width = value;
    }

    public float Height
    {
        get => _Rect.Height;
        set => _Rect.Height = value;
    }

    public double Rotation { get; set; }

    public Rect(float x, float y, float width, float height)
    {
        _Rect = new RectangleF(x, y, width, height);
    }

    public static Rect Max(Rect a, Rect b)
    {
        float x = Math.Min(a.X, b.X);
        float y = Math.Min(a.Y, b.Y);
        float width = Math.Max(a.X + a.Width, b.X + b.Width) - x;
        float height = Math.Max(a.Y + a.Height, b.Y + b.Height) - y;

        return new Rect(x, y, width, height);
    }

    public bool CollidesWithRotated(Rect r1, Rect r2)
    {
        // Get the normals of the edges of the second rectangle
        Vector2[] normals = new Vector2[]
        {
            new Vector2((float)Math.Cos(r2.Rotation), (float)Math.Sin(r2.Rotation)),
            new Vector2((float)-Math.Sin(r2.Rotation), (float)Math.Cos(r2.Rotation))
        };

        // Check the projections of the rectangles onto the normals of the second rectangle
        for (int i = 0; i < normals.Length; i++)
        {
            Vector2 normal = normals[i];

            // Project the first rectangle onto the current normal
            Vector2[] points1 = new Vector2[]
            {
                new Vector2(r1.X, r1.Y),
                new Vector2(r1.X + r1.Width, r1.Y),
                new Vector2(r1.X, r1.Y + r1.Height),
                new Vector2(r1.X + r1.Width, r1.Y + r1.Height)
            };

            ProjectRectangle(points1, normal, out double min1, out double max1);

            // Project the second rectangle onto the current normal
            Vector2[] points2 = new Vector2[]
            {
                new Vector2(r2.X, r2.Y),
                new Vector2(r2.X + r2.Width, r2.Y),
                new Vector2(r2.X, r2.Y + r2.Height),
                new Vector2(r2.X + r2.Width, r2.Y + r2.Height)
            };

            ProjectRectangle(points2, normal, out double min2, out double max2);

            // Check if the projections do not overlap
            if (max1 < min2 || max2 < min1)
            {
                return false;
            }
        }

        // If the function reaches this point, it means that the rectangles collide
        return true;
    }

    private void ProjectRectangle(Vector2[] points, Vector2 normal, out double min, out double max)
    {
        min = double.MaxValue;
        max = double.MinValue;
        for (int i = 0; i < points.Length; i++)
        {
            double dot = (points[i].X * normal.X) + (points[i].Y * normal.Y);
            min = Math.Min(min, dot);
            max = Math.Max(max, dot);
        }
    }
}