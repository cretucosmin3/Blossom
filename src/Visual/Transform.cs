using System;
using Kara;
using Kara.Core.Visual;

public class Transform
{
    private Transform _Parent = null;
    public Transform Parent
    {
        get => _Parent;
        set
        {
            _Parent = value;
            SetAnchorValues();
        }
    }

    private Rect ComputedTransform = new Rect(0, 0, 0, 0);
    public Rect Computed { get => ComputedTransform; }
    public Rect Local { get; private set; } = new Rect(0, 0, 0, 0);

    /// <summary>
    /// Called when the transform is updated. (x, y, w, h)
    /// </summary>
    public Action<Transform> OnChanged;

    public float X
    {
        get => Local.X;
        set
        {
            XChanged = true;
            Local.X = value;
            CalculateHorizontalAnchors();

            OnChanged?.Invoke(this);
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

            OnChanged?.Invoke(this);
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

            OnChanged?.Invoke(this);
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

            OnChanged?.Invoke(this);
        }
    }

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

    public Transform()
    {

    }

    public Transform(float x, float y, float width, float height)
    {
        Local = new Rect(x, y, width, height);
        SetAnchorValues();
    }

    public void DetachParent()
    {
        Parent = null;
        Console.WriteLine(Local.X);
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
        CalculateHorizontalAnchors();
        CalculateVerticalAnchors();

        ComputeHorizontalTransform();
        ComputeVerticalTransform();
    }

    internal bool XChanged = true;
    internal bool YChanged = true;
    private bool WidthChanged = true;
    private bool HeightChanged = true;

    private void CalculateHorizontalAnchors()
    {
        float ParentWidth = Browser.window.Size.X;

        if (Parent is not null)
            ParentWidth = Parent.Width;

        FixedLeft = X;
        RelativeLeft = FixedLeft / ParentWidth;

        FixedRight = ParentWidth - (X + Width);
        RelativeRight = FixedRight / ParentWidth;
    }

    private void ComputeHorizontalTransform()
    {
        float ParentWidth = Browser.window.Size.X;

        if (Parent is not null)
            ParentWidth = Parent.ComputedTransform.Width;

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
        ComputedTransform.X += Parent is null ? 0 : Parent.ComputedTransform.X;
    }

    private void CalculateVerticalAnchors()
    {
        float ParentHeight = Browser.window.Size.Y;

        if (Parent is not null)
            ParentHeight = Parent.Height;

        FixedTop = Y;
        RelativeTop = FixedTop / ParentHeight;

        FixedBottom = ParentHeight - (Y + Height);
        RelativeBottom = FixedBottom / ParentHeight;
    }

    private void ComputeVerticalTransform()
    {
        float ParentHeight = Browser.window.Size.Y;

        if (Parent != null)
            ParentHeight = Parent.Computed.Height;

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
        ComputedTransform.Y += Parent != null ? Parent.ComputedTransform.Y : 0;
    }

    private bool once = true;
    internal void Evaluate()
    {
        ComputeHorizontalTransform();
        ComputeVerticalTransform();
    }

    internal void ClearRenderData()
    {
        XChanged = false;
        YChanged = false;
        WidthChanged = false;
        HeightChanged = false;
    }
}

public class Rect
{

    private System.Drawing.RectangleF _Rect;

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

    public System.Drawing.RectangleF RectF { get => _Rect; }

    public Rect()
    {
        new System.Drawing.RectangleF(0, 0, 0, 0);
    }

    public Rect(float x, float y, float width, float height)
    {
        _Rect = new System.Drawing.RectangleF(x, y, width, height);
    }
}
