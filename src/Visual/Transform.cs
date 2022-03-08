using System.ComponentModel;
using System;
using System.Drawing;
using Kara.Core.Visual;

public class Transform
{
    private Transform ParentRect = null;
    private Rect ComputedTransform = new Rect(0, 0, 0, 0);
    public Rect Global { get; set; } = new Rect(0, 0, 0, 0);
    public Rect Local { get; private set; } = new Rect(0, 0, 0, 0);

    /// <summary>
    /// Called when the transform is updated. (x, y, w, h)
    /// </summary>
    public Action<float, float, float, float> OnResized = null;

    public float X
    {
        get => Local.X;
        set
        {
            XChanged = true;
            Local.X = value;
            CalculateHorizontalAnchors();

            OnResized?.Invoke(
                Local.X,
                Local.Y,
                Local.Width,
                Local.Height
            );
        }
    }

    public float Y
    {
        get => Local.Y;
        set
        {
            YChanged = true;
            Local.Y = value;
            CalculateVerticalAnchors();

            OnResized?.Invoke(
                Local.X,
                Local.Y,
                Local.Width,
                Local.Height
            );
        }
    }

    public float Width
    {
        get => Local.Width;
        set
        {
            WidthChanged = true;
            Local.Width = value;
            CalculateHorizontalAnchors();

            OnResized?.Invoke(
                Local.X,
                Local.Y,
                Local.Width,
                Local.Height
            );
        }
    }

    public float Height
    {
        get => Local.Height;
        set
        {
            HeightChanged = true;
            Local.Height = value;
            CalculateVerticalAnchors();

            OnResized?.Invoke(
                Local.X,
                Local.Y,
                Local.Width,
                Local.Height
            );
        }
    }

    /// <summary>
    /// Global x of the transform
    /// </summary>
    public float gX { get => Global.X; }

    /// <summary>
    /// Global y of the transform
    /// </summary>
    public float gY { get => Global.Y; }

    /// <summary>
    /// Global width of the transform
    /// </summary>
    public float gWidth { get => Global.Width; }

    /// <summary>
    /// Global height of the transform
    /// </summary>
    public float gHeight { get => Global.Height; }

    private Anchor _Anchor;
    public Anchor Anchor
    {
        get => _Anchor;
        set
        {
            _Anchor = value;
            SetAnchorValues();
        }
    }

    public Transform() { }

    public Transform(float x, float y, float width, float height)
    {
        Local = new Rect(x, y, width, height);
        SetAnchorValues();
    }

    public void SyncWith(Transform transform)
    {
        ParentRect = transform;
        SetAnchorValues();
    }

    public void Unsync()
    {
        ParentRect = null;
        SetAnchorValues();
    }

    internal float FixedLeft = 0f;
    internal float FixedRight = 0f;
    internal float FixedTop = 0f;
    internal float FixedBottom = 0f;

    internal float RelativeLeft = 0f;
    internal float RelativeRight = 0f;
    internal float RelativeTop = 0f;
    internal float RelativeBottom = 0f;

    public bool FixedHeight { get; set; } = false;
    public bool FixedWidth { get; set; } = false;

    internal void SetAnchorValues()
    {
        if (ParentRect != null)
        {
            CalculateHorizontalAnchors();
            CalculateVerticalAnchors();

            ComputeHorizontalTransform();
            ComputeVerticalTransform();
        }
    }

    private bool XChanged = false;
    private bool YChanged = false;
    private bool WidthChanged = false;
    private bool HeightChanged = false;

    private void CalculateHorizontalAnchors()
    {
        var ParentWidth = ParentRect.Local.Width;

        FixedLeft = X;
        RelativeLeft = FixedLeft / ParentWidth;

        FixedRight = ParentWidth - (X + Width);
        RelativeRight = FixedRight / ParentWidth;
    }

    private void ComputeHorizontalTransform()
    {
        var ParentWidth = ParentRect.ComputedTransform.Width;

        if (_Anchor.HasFlag(Anchor.Left) && !_Anchor.HasFlag(Anchor.Right))
        {
            ComputedTransform.X = FixedLeft;
            ComputedTransform.Width = Width;
        }
        else if (_Anchor.HasFlag(Anchor.Right) && !_Anchor.HasFlag(Anchor.Left))
        {
            ComputedTransform.X = ParentWidth - FixedRight - Width;
            ComputedTransform.Width = Width;
        }
        else if (_Anchor.HasFlag(Anchor.Left) && _Anchor.HasFlag(Anchor.Right))
        {
            ComputedTransform.X = FixedLeft;
            ComputedTransform.Width = ParentWidth - FixedLeft - FixedRight;
        }
        else
        {
            ComputedTransform.X = RelativeLeft * ParentWidth;
            ComputedTransform.Width = ParentWidth - (RelativeRight * ParentWidth) - ComputedTransform.X;

            if (FixedWidth)
            {
                var centerX = ComputedTransform.X + (ComputedTransform.Width / 2f);
                ComputedTransform.X = centerX - (Width / 2f);
                ComputedTransform.Width = Width;
            }
        }

        if (ComputedTransform.Width < 0)
        {
            ComputedTransform.Width = 0;
        }

        // Add parent X
        ComputedTransform.X += ParentRect.ComputedTransform.X;
    }

    private void CalculateVerticalAnchors()
    {
        var ParentHeight = ParentRect.Height;

        FixedTop = Y;
        RelativeTop = FixedTop / ParentHeight;

        FixedBottom = ParentHeight - (Y + Height);
        RelativeBottom = FixedBottom / ParentHeight;
    }

    private void ComputeVerticalTransform()
    {
        var ParentHeight = ParentRect.ComputedTransform.Height;

        bool bottomAnchored = _Anchor.HasFlag(Anchor.Bottom);
        bool topAnchored = _Anchor.HasFlag(Anchor.Top);

        if (topAnchored && !bottomAnchored)
        {
            ComputedTransform.Y = FixedTop;
            ComputedTransform.Height = Height;
        }
        else if (bottomAnchored && !topAnchored)
        {
            ComputedTransform.Y = ParentHeight - FixedBottom - Height;
            ComputedTransform.Height = Height;
        }
        else if (topAnchored && bottomAnchored)
        {
            ComputedTransform.Y = FixedTop;
            ComputedTransform.Height = ParentHeight - FixedTop - FixedBottom;
        }
        else
        {
            ComputedTransform.Y = RelativeTop * ParentHeight;
            ComputedTransform.Height = ParentHeight - (RelativeBottom * ParentHeight) - ComputedTransform.Y;

            if (FixedHeight)
            {
                var centerY = ComputedTransform.Y + (ComputedTransform.Height / 2f);
                ComputedTransform.Y = centerY - (Height / 2f);
                ComputedTransform.Height = Height;
            }
        }

        if (ComputedTransform.Height < 0)
        {
            ComputedTransform.Height = 0;
        }

        // Add parent Y
        ComputedTransform.Y += ParentRect.ComputedTransform.Y;
    }
}

public class Rect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public Rect()
    {
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
    }

    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
