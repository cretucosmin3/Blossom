using System;
using SkiaSharp;
using Blossom.Core.Input;

namespace Blossom.Core.Visual
{
    public class ScrollContainer : VisualElement
    {
        public float ScrollX { get; set; } = 0f;
        public float ScrollY { get; set; } = 0f;

        public float MaxScrollX => Math.Max(0, ContentWidth - Transform.Computed.Width);
        public float MaxScrollY => Math.Max(0, ContentHeight - Transform.Computed.Height);

        public float ContentWidth
        {
            get
            {
                float max = 0;
                var children = Children;
                for (int i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    if (child == null) continue;
                    float right = child.Transform.Computed.X - Transform.Computed.X + ScrollX + child.Transform.Computed.Width;
                    if (right > max) max = right;
                }
                return max;
            }
        }

        public float ContentHeight
        {
            get
            {
                float max = 0;
                var children = Children;
                for (int i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    if (child == null) continue;
                    float bottom = child.Transform.Computed.Y - Transform.Computed.Y + ScrollY + child.Transform.Computed.Height;
                    if (bottom > max) max = bottom;
                }
                return max;
            }
        }

        public ScrollContainer()
        {
            IsClipping = true; // Auto-clips children to viewport bounds

            Events.OnMouseScroll += (sender, offset) =>
            {
                // Update scroll offset (e.g. 25px per scroll tick)
                ScrollY = Math.Clamp(ScrollY - offset.Y * 25f, 0, MaxScrollY);
                ScrollX = Math.Clamp(ScrollX - offset.X * 25f, 0, MaxScrollX);

                // Mark children transforms dirty so they recompute their positions
                MarkChildrenTransformDirty();
                ScheduleRender();
            };
        }

        public void MarkChildrenTransformDirty()
        {
            var children = Children;
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
}
