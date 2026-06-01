using System;
namespace Blossom.Core.Visual;

public class Transform
{
    internal VisualElement ParentElement;
    internal bool HasChanged;
    private bool _anchorsInitialized = false;
    internal bool _transformDirty = true;
    private Transform _Parent = null;
    public Transform Parent
    {
        get => _Parent;
        set
        {
            _Parent = value;
            _transformDirty = true;
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
    // TODO: Change into SKRect for simplicity and to avoid casting
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
            Local.Width = Computed.Width;
            Local.Height = Computed.Height;

            Local.X = value - (Parent != null ? Parent.ComputedTransform.X : 0);
            CalculateLeftAnchor();
            CalculateRighAnchor();

            CenterX = X + (Width / 2f);

            _transformDirty = true;
            OnChanged?.Invoke(this);
            ParentElement?.ScheduleRender();
        }
    }

    public float Y
    {
        get => ComputedTransform.Y;
        set
        {
            Local.Width = Computed.Width;
            Local.Height = Computed.Height;

            Local.Y = value - (Parent != null ? Parent.ComputedTransform.Y : 0);
            CalculateTopAnchor();
            CalculateBottomAnchor();

            CenterY = Y + (Height / 2f);

            _transformDirty = true;
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

            _transformDirty = true;
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

            _transformDirty = true;
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

            var parentX = Parent != null ? Parent.ComputedTransform.X : 0;
            var parentY = Parent != null ? Parent.ComputedTransform.Y : 0;

            Local.X = Computed.X - parentX;
            Local.Y = Computed.Y - parentY;
            Local.Width = Computed.Width;
            Local.Height = Computed.Height;

            if (ValidateOnAnchor)
                SetAnchorValues();

            _transformDirty = true;
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
        if (Parent != null && ParentWidth == 0)
        {
            _anchorsInitialized = false;
            return;
        }

        // Horizontal anchors.
        CalculateLeftAnchor();
        CalculateRighAnchor();

        // Vertical anchors.
        CalculateTopAnchor();
        CalculateBottomAnchor();

        ComputeHorizontalTransform();
        ComputeVerticalTransform();

        _anchorsInitialized = true;
    }

    private void CalculateLeftAnchor()
    {
        FixedLeft = Local.X;
        RelativeLeft = ParentWidth == 0 ? 0 : FixedLeft / ParentWidth;
    }

    private void CalculateRighAnchor()
    {
        FixedRight = ParentWidth - (Local.X + Local.Width);
        RelativeRight = ParentWidth == 0 ? 0 : FixedRight / ParentWidth;
    }

    private void CalculateTopAnchor()
    {
        FixedTop = Local.Y;
        RelativeTop = ParentHeight == 0 ? 0 : FixedTop / ParentHeight;
    }

    private void CalculateBottomAnchor()
    {
        FixedBottom = ParentHeight - (Local.Y + Local.Height);
        RelativeBottom = ParentHeight == 0 ? 0 : FixedBottom / ParentHeight;
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
        float scrollX = 0f;
        if (Parent?.ParentElement is ScrollContainer sc)
        {
            scrollX = sc.ScrollX;
        }
        ComputedTransform.X += Parent is null ? 0 : (Parent.ComputedTransform.X - scrollX);
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
        float scrollY = 0f;
        if (Parent?.ParentElement is ScrollContainer sc)
        {
            scrollY = sc.ScrollY;
        }
        ComputedTransform.Y += Parent != null ? (Parent.ComputedTransform.Y - scrollY) : 0;
    }

    internal bool Evaluate()
    {
        if (Browser.WasResized)
        {
            _transformDirty = true;
        }

        if (!_transformDirty && _anchorsInitialized)
        {
            return false;
        }

        if (!_anchorsInitialized && (Parent == null || ParentWidth > 0))
        {
            SetAnchorValues();
        }

        Rect previousComputed = new(Computed.X, Computed.Y, Computed.Width, Computed.Height);

        ComputeHorizontalTransform();
        ComputeVerticalTransform();

        _transformDirty = false;

        bool changed = previousComputed != Computed;
        if (changed)
        {
            var children = ParentElement?.Children;
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    if (child != null)
                    {
                        child.Transform._transformDirty = true;
                        child.ScheduleRender();
                    }
                }
            }
        }

        return changed;
    }
}