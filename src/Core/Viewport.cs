using System.Numerics;
using System;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Blossom.Core.Delegates.Common;

namespace Blossom.Core;

public abstract class Viewport
{
    private Scrollable _scrollable = Scrollable.None;
    public Rect FirstRect { get; private set; } = new Rect(0, 0, 0, 0);
    public Rect ProvisionalRect = new Rect(0, 0, 0, 0);

    public Scrollable Scrollable
    {
        get => _scrollable; set
        {
            _scrollable = value;
            // Trigger update
        }
    }

    public void UpdateFromParent(Transform parentTransform)
    {

    }

    public void UpdateWithChild(Transform childTransform)
    {

    }
}

public enum Scrollable
{
    None,
    Horizontal,
    Vertical,
    Both
}