using System;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Blossom.Core.Delegates.Common;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Blossom.Core
{
    public abstract class View : IDisposable
    {
        public EventMap Events = new();
        public ElementTree Elements = new();
        public SKColor BackColor = SKColors.White;
        public readonly CommandLedger Ledger = new();

        public bool UseReferenceResolution { get; set; } = false;
        public int ReferenceWidth { get; set; } = 1280;
        public int ReferenceHeight { get; set; } = 800;

        public int Width => UseReferenceResolution ? ReferenceWidth : (int)Browser.RenderRect.Width;
        public int Height => UseReferenceResolution ? ReferenceHeight : (int)Browser.RenderRect.Height;

        public event ForVoid Loop;

        internal bool IsLoaded { get; set; }
        public bool RenderRequired { get; internal set; } = true;
        public bool FullRenderRequired { get; set; } = true;

        private VisualElement hoveredElement;
        private VisualElement mouseDownElement;

        private readonly object _dirtyRectsLock = new();
        internal readonly List<SKRect> DirtyRects = new();
        private readonly List<SKRect> _localDirtyRects = new();

        private bool _hierarchyDirty = true;
        internal void MarkHierarchyDirty() { _hierarchyDirty = true; }
        internal readonly List<VisualElement> CachedRenderQueue = new();
        internal readonly List<VisualElement> CachedSortedElements = new();

        internal void AddDirtyRect(SKRect rect)
        {
            lock (_dirtyRectsLock)
            {
                DirtyRects.Add(rect);
            }
        }

        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                //! #render title only
            }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
            }
        }

        public Application Application { get; internal set; }
        public VisualElement FocusedElement { get; set; }

        public abstract void Init();
        public virtual void OnActivated() { }
        public virtual void OnDeactivated() { }

        internal View(string name)
        {
            Name = name;

            Events.OnMouseDown += OnMouseDown;
            Events.OnMouseUp += OnMouseUp;
            Events.OnMouseMove += OnMouseMove;
            Events.OnMouseScroll += OnMouseScroll;
        }

        private void OnMouseDown(object _, MouseEventArgs args)
        {
            VisualElement element = Elements.FirstFromPoint(
                new(args.Global.X, args.Global.Y));

            if (element != null)
            {
                element.Events.HandleMouseDown(args.Button, args.Global, element);

                if (FocusedElement != null && FocusedElement != element)
                {
                    FocusedElement.OnFocusLost?.Invoke(FocusedElement);
                }

                if (element.Focusable)
                {
                    element.GetFocus();
                    element.OnFocused?.Invoke(element);
                }
            }
            else
            {
                FocusedElement?.OnFocusLost?.Invoke(FocusedElement);
                FocusedElement = null!;
            }

            mouseDownElement = element ?? null!;
        }

        private void OnMouseUp(object _, MouseEventArgs args)
        {
            if (mouseDownElement != null)
            {
                mouseDownElement.Events.HandleMouseUp(args.Button, args.Global, mouseDownElement);
                mouseDownElement = null;
                return;
            }

            var element = Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));
            element?.Events.HandleMouseUp(args.Button, args.Global, element);
        }

        private void OnMouseMove(object _, MouseEventArgs args)
        {
            var element = Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));
            element?.Events.HandleMouseMove(args.Global, element);

            if (hoveredElement != element)
            {
                hoveredElement?.Events.HandleMouseLeave(hoveredElement);
                element?.Events.HandleMouseEnter(element);
            }
            else if (element == hoveredElement)
            {
                element?.Events.HandleMouseHover(element, args.Global);
            }

            hoveredElement = element;
        }

        private void OnMouseScroll(object sender, System.Numerics.Vector2 offset)
        {
            var el = hoveredElement;
            while (el != null)
            {
                el.Events.HandleMouseScroll(offset, el);
                if (el is ScrollContainer)
                {
                    break;
                }
                el = el.Parent;
            }
        }

        internal void TriggerLoop() => Loop?.Invoke();

        public void AddElement(VisualElement element)
        {
            element.ParentView = this;
            Elements.AddElement(ref element);

            element.AddedToView();
            _hierarchyDirty = true;
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        public void RemoveElement(VisualElement element)
        {
            Elements.RemoveElement(element);
            _hierarchyDirty = true;
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        public void TrackElement(ref VisualElement element)
        {
            Elements.AddElement(ref element);
            element.AddedToView();
            _hierarchyDirty = true;
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        public void UntrackElement(ref VisualElement element)
        {
            Elements.RemoveElement(element);
            _hierarchyDirty = true;
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        private void CollectElements(VisualElement root, List<VisualElement> list)
        {
            list.Add(root);
            var sortedChildren = root.Children.Where(c => c != null).OrderBy(c => c.ZIndex).ToList();
            foreach (var child in sortedChildren)
            {
                CollectElements(child, list);
            }
        }

        internal void Render()
        {
            if (Browser.WasResized) FullRenderRequired = true;

            lock (_dirtyRectsLock)
            {
                if (DirtyRects.Count == 0 && !RenderRequired && !FullRenderRequired) return;

                if (FullRenderRequired)
                {
                    // Full redraw required (e.g. view switch or resize)
                    DirtyRects.Clear();
                    DirtyRects.Add(new SKRect(0, 0, (int)Browser.RenderRect.Width, (int)Browser.RenderRect.Height));
                    FullRenderRequired = false;
                }
                else if (RenderRequired && DirtyRects.Count == 0)
                {
                    DirtyRects.Add(new SKRect(0, 0, (int)Browser.RenderRect.Width, (int)Browser.RenderRect.Height));
                }

                _localDirtyRects.Clear();
                _localDirtyRects.AddRange(DirtyRects);
                DirtyRects.Clear();
            }

            // Union dirty rects to simplify clipping path and minimize element overlap checks
            if (_localDirtyRects.Count > 10)
            {
                var unionRect = _localDirtyRects[0];
                for (int i = 1; i < _localDirtyRects.Count; i++)
                {
                    unionRect = SKRect.Union(unionRect, _localDirtyRects[i]);
                }
                _localDirtyRects.Clear();
                _localDirtyRects.Add(unionRect);
            }

            if (_hierarchyDirty)
            {
                CachedRenderQueue.Clear();
                var rootElements = Elements.Items
                    .Where(e => e.Parent == null)
                    .OrderBy(e => e.ZIndex)
                    .ToList();
                
                foreach (var element in rootElements)
                {
                    CollectElements(element, CachedRenderQueue);
                }

                CachedSortedElements.Clear();
                CachedSortedElements.AddRange(CachedRenderQueue);

                foreach (var element in CachedRenderQueue)
                {
                    element.MarkVisibilityClippingDirty();
                }

                _hierarchyDirty = false;
            }

            // Evaluate transform and visibility in hierarchical order (parents first)
            for (int idx = 0; idx < CachedRenderQueue.Count; idx++)
            {
                var element = CachedRenderQueue[idx];
                if (element.Transform.Evaluate())
                {
                    element.IsDirty = true;
                    element.MarkVisibilityClippingDirty();
                }

                if (element._visibilityClippingDirty)
                {
                    element.EvaluateVisibilityAndClipping();
                    element._visibilityClippingDirty = false;
                }
            }

            using (new SKAutoCanvasRestore(Renderer.Canvas))
            {
                if (_localDirtyRects.Count == 1)
                {
                    var r = _localDirtyRects[0];
                    var rounded = SKRect.Create((int)Math.Floor(r.Left), (int)Math.Floor(r.Top), (int)Math.Ceiling(r.Width) + 1, (int)Math.Ceiling(r.Height) + 1);
                    Renderer.Canvas.ClipRect(rounded, SKClipOperation.Intersect, true);
                }
                else
                {
                    using var dirtyPath = new SKPath();
                    for (int i = 0; i < _localDirtyRects.Count; i++)
                    {
                        var r = _localDirtyRects[i];
                        var rounded = SKRect.Create((int)Math.Floor(r.Left), (int)Math.Floor(r.Top), (int)Math.Ceiling(r.Width) + 1, (int)Math.Ceiling(r.Height) + 1);
                        dirtyPath.AddRect(rounded);
                    }
                    Renderer.Canvas.ClipPath(dirtyPath, SKClipOperation.Intersect, true);
                }
                
                // Clear dirty region to background
                Renderer.Canvas.DrawColor(BackColor);

                for (int idx = 0; idx < CachedSortedElements.Count; idx++)
                {
                    var element = CachedSortedElements[idx];
                    if (!element.Visible) continue;
                    if (element.ComputedVisibility == Visibility.Hidden) continue;

                    var elementRect = element.RenderBounds;

                    bool overlapsDirty = false;
                    for (int i = 0; i < _localDirtyRects.Count; i++)
                    {
                        if (_localDirtyRects[i].IntersectsWith(elementRect))
                        {
                            overlapsDirty = true;
                            break;
                        }
                    }

                    if (overlapsDirty)
                    {
                        element.RenderSingle(Renderer.Canvas);
                        element.IsDirty = false;
                    }
                }
            }

            RenderRequired = false;
        }

        internal void RenderChanges(Action doChanges)
        {
            // clear state
            doChanges();
            // pull changes
            // update state
            // render from previous state or new
            RenderRequired = true;
        }

        public void ForceLayoutEvaluation()
        {
            _hierarchyDirty = true;
            FullRenderRequired = true;
            RenderRequired = true;

            foreach (var element in Elements.Items)
            {
                if (element?.Transform != null)
                {
                    element.Transform._transformDirty = true;
                    element.MarkVisibilityClippingDirty();
                }
            }
        }

        public void Dispose()
        {
            // Not implemented yet
        }
    }
}