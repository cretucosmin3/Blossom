using System;
namespace Blossom.Core.Visual;

public class Transform
{
    internal VisualElement ParentElement;
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

    private float ParentWidth => Parent != null ? Parent.Width : Browser.window.Size.X;
    private float ParentHeight => Parent != null ? Parent.Height : Browser.window.Size.Y;

    internal float FixedLeft;
    internal float FixedRight;
    internal float FixedTop;
    internal float FixedBottom;

    internal float RelativeLeft;
    internal float RelativeRight;
    internal float RelativeTop;
    internal float RelativeBottom;

    private readonly Rect ComputedTransform = new(0, 0, 0, 0);
    public Rect Computed => ComputedTransform;
    public Rect Local { get; } = new Rect(0, 0, 0, 0);

    public bool FixedHeight { get; set; } = false;
    public bool FixedWidth { get; set; } = false;

    public bool FixedSize
    {
        set
        {
            FixedHeight = value;
            FixedWidth = value;
        }
    }

    /// <summary>
    /// Called when the transform is updated. (x, y, w, h)
    /// </summary>
    public Action<Transform> OnChanged;

    public float X
    {
        get => ComputedTransform.X;
        set
        {
            Local.X = value;
            CalculateLeftAnchor();
            CalculateRighAnchor();

            CenterX = X + (Width / 2f);

            OnChanged?.Invoke(this);
            ParentElement?.ScheduleRender();
        }
    }

    public float Y
    {
        get => ComputedTransform.Y;
        set
        {
            Local.Y = value;
            CalculateTopAnchor();
            CalculateBottomAnchor();

            CenterY = Y + (Height / 2f);

            OnChanged?.Invoke(this);
            ParentElement?.ScheduleRender();
        }
    }

    public float Width
    {
        get => ComputedTransform.Width;
        set
        {
            Local.Width = value;
            CalculateLeftAnchor();
            CalculateRighAnchor();

            CenterX = X + (Width / 2f);

            OnChanged?.Invoke(this);
            ParentElement?.ScheduleRender();
        }
    }

    public float Height
    {
        get => ComputedTransform.Height;
        set
        {
            Local.Height = value;
            CalculateTopAnchor();
            CalculateBottomAnchor();

            CenterY = Y + (Height / 2f);

            OnChanged?.Invoke(this);
            ParentElement?.ScheduleRender();
        }
    }

    public float CenterX { get; private set; }
    public float CenterY { get; private set; }

    public float Left { get => X; }
    public float Right { get => X + Width; }
    public float Top { get => Y; }
    public float Bottom { get => Y + Height; }

    public bool ValidateOnAnchor { get; set; } = false;

    private Anchor _Anchor;
    public Anchor Anchor
    {
        get => _Anchor;
        set
        {
            _Anchor = value;

            Local.X = Computed.X;
            Local.Y = Computed.Y;
            Local.Width = Computed.Width;
            Local.Height = Computed.Height;

            if (ValidateOnAnchor)
                SetAnchorValues();

            OnChanged?.Invoke(this);
            ParentElement?.ScheduleRender();
        }
    }

    public Transform() { }

    public Transform(float x, float y, float width, float height)
    {
        Local = new Rect(x, y, width, height);
        SetAnchorValues();
    }

    public void DetachParent()
    {
        Parent = null;
        SetAnchorValues();
    }

    internal void SetAnchorValues()
    {
        // Horizontal anchors.
        CalculateLeftAnchor();
        CalculateRighAnchor();

        // Vertical anchors.
        CalculateTopAnchor();
        CalculateBottomAnchor();

        ComputeHorizontalTransform();
        ComputeVerticalTransform();
    }

    private void CalculateLeftAnchor()
    {
        FixedLeft = Local.X;
        RelativeLeft = FixedLeft / ParentWidth;
    }

    private void CalculateRighAnchor()
    {
        FixedRight = ParentWidth - (Local.X + Local.Width);
        RelativeRight = FixedRight / ParentWidth;
    }

    private void CalculateTopAnchor()
    {
        FixedTop = Local.Y;
        RelativeTop = FixedTop / ParentHeight;
    }

    private void CalculateBottomAnchor()
    {
        FixedBottom = ParentHeight - (Local.Y + Local.Height);
        RelativeBottom = FixedBottom / ParentHeight;
    }

    private void ComputeHorizontalTransform()
    {
        float ParentWidth = Browser.window.Size.X;

        if (Parent is not null)
            ParentWidth = Parent.ComputedTransform.Width;

        if (_Anchor.HasFlag(Anchor.Left) && !_Anchor.HasFlag(Anchor.Right))
        {
            ComputedTransform.X = FixedLeft;
            ComputedTransform.Width = Local.Width;
        }
        else if (_Anchor.HasFlag(Anchor.Right) && !_Anchor.HasFlag(Anchor.Left))
        {
            ComputedTransform.X = ParentWidth - FixedRight - Local.Width;
            ComputedTransform.Width = Local.Width;
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
                ComputedTransform.X = centerX - (Local.Width / 2f);
                ComputedTransform.Width = Local.Width;
            }
        }

        if (ComputedTransform.Width < 0)
        {
            ComputedTransform.Width = 0;
        }

        // Add parent X
        ComputedTransform.X += Parent is null ? 0 : Parent.ComputedTransform.X;

        Local.X = ComputedTransform.X;
        Local.Width = ComputedTransform.Width;
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
            ComputedTransform.Height = Local.Height;
        }
        else if (bottomAnchored && !topAnchored)
        {
            ComputedTransform.Y = ParentHeight - FixedBottom - Local.Height;
            ComputedTransform.Height = Local.Height;
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
                ComputedTransform.Y = centerY - (Local.Height / 2f);
                ComputedTransform.Height = Local.Height;
            }
        }

        if (ComputedTransform.Height < 0)
        {
            ComputedTransform.Height = 0;
        }

        // Add parent Y
        ComputedTransform.Y += Parent != null ? Parent.ComputedTransform.Y : 0;

        Local.Y = ComputedTransform.Y;
        Local.Height = ComputedTransform.Height;
    }

    internal void Evaluate()
    {
        ComputeHorizontalTransform();
        ComputeVerticalTransform();
    }
}