using System;
using Blossom.Core.Design;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Blossom.Core.Delegates.Common;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SkiaSharp;
using Silk.NET.Input;

namespace Blossom.Core
{
    public abstract class View : IDisposable
    {
        public EventMap Events = new();
        public ElementTree Elements = new();
        public SKColor BackColor = SKColors.White;
        public readonly CommandLedger Ledger = new();

        private DesignCanvas _canvas = new DesignCanvas();

        /// <summary>
        /// The declared design canvas defining authoring dimensions (<see cref="Ru"/>) and host fit policy.
        /// </summary>
        public DesignCanvas Canvas
        {
            get => _canvas;
            set
            {
                if (_canvas != value)
                {
                    if (_canvas != null)
                        _canvas.Changed -= ForceLayoutEvaluation;
                    _canvas = value ?? new DesignCanvas();
                    _canvas.Changed += ForceLayoutEvaluation;
                    ForceLayoutEvaluation();
                }
            }
        }

        public View() : this("Untitled")
        {
        }

        /// <summary>
        /// Logical layout width. Under the product model (window-bound apps), tracks the window client width
        /// so root content with Left|Right anchors fills the app.
        /// </summary>
        public int Width => (int)GetLayoutWidth();

        /// <summary>
        /// Logical layout height. Under the product model (window-bound apps), tracks the window client height
        /// so root content with Top|Bottom anchors fills the app.
        /// </summary>
        public int Height => (int)GetLayoutHeight();

        /// <summary>
        /// Strongly-typed layout width in design units (<see cref="Ru"/>).
        /// </summary>
        public Ru LayoutWidth => new(GetLayoutWidth());

        /// <summary>
        /// Strongly-typed layout height in design units (<see cref="Ru"/>).
        /// </summary>
        public Ru LayoutHeight => new(GetLayoutHeight());

        private float GetLayoutWidth()
        {
            // Apps fill the window: layout size is the client size so anchors reflow on resize.
            if (Browser.RenderRect.Width > 0)
                return Browser.RenderRect.Width;
            return Canvas.Width;
        }

        private float GetLayoutHeight()
        {
            if (Browser.RenderRect.Height > 0)
                return Browser.RenderRect.Height;
            return Canvas.Height;
        }

        // Backward compatibility shims
        public bool UseReferenceResolution
        {
            get => true;
            set { }
        }

        public int ReferenceWidth
        {
            get => (int)Canvas.Width;
            set => Canvas.Width = value;
        }

        public int ReferenceHeight
        {
            get => (int)Canvas.Height;
            set => Canvas.Height = value;
        }

        /// <summary>
        /// Maps a window coordinate to host design canvas units.
        /// </summary>
        public Vector2 PointToDesign(Vector2 windowPos) =>
            PointToDesign(windowPos.X, windowPos.Y);

        /// <summary>
        /// Maps a window coordinate to host design canvas units.
        /// </summary>
        public Vector2 PointToDesign(float winX, float winY) =>
            Canvas.PointToDesign(winX, winY, Browser.RenderRect.Width, Browser.RenderRect.Height);

        /// <summary>
        /// Maps a design canvas unit point to window coordinates.
        /// </summary>
        public Vector2 PointToWindow(Vector2 designPos) =>
            PointToWindow(designPos.X, designPos.Y);

        /// <summary>
        /// Maps a design canvas unit point to window coordinates.
        /// </summary>
        public Vector2 PointToWindow(float desX, float desY) =>
            Canvas.PointToWindow(desX, desY, Browser.RenderRect.Width, Browser.RenderRect.Height);

        public event ForVoid Loop;

        internal bool IsLoaded { get; set; }
        public bool RenderRequired { get; internal set; } = true;
        public bool FullRenderRequired { get; set; } = true;
        public bool LayoutRequired { get; internal set; } = true;

        private VisualElement? hoveredElement;
        public VisualElement? HoveredElement => hoveredElement;

        public VisualElement? PointerCaptureElement { get; private set; }
        public VisualElement? ActiveKeyboardElement { get; private set; }

        // Legacy compatibility
        public VisualElement? FocusedElement
        {
            get => ActiveKeyboardElement;
            set => SetActiveKeyboardElement(value);
        }

        public void SetPointerCapture(VisualElement element)
        {
            if (element != null && !element.EffectiveInteractive) return;
            if (PointerCaptureElement == element) return;
            PointerCaptureElement = element;
        }

        public void ReleasePointerCapture(VisualElement? element = null)
        {
            if (element == null || PointerCaptureElement == element)
            {
                PointerCaptureElement = null;
            }
        }

        public void SetActiveKeyboardElement(VisualElement? element)
        {
            if (element != null && !element.EffectiveInteractive) return;
            if (ActiveKeyboardElement == element) return;
            var old = ActiveKeyboardElement;
            ActiveKeyboardElement = element;
            old?.OnFocusLost?.Invoke(old);
            element?.OnFocused?.Invoke(element);
        }

        public VisualElement? FindById(Guid id) => Elements.FindById(id);
        public VisualElement? FindByName(string name) => Elements.FindByName(name);

        private VisualElement? _clickCandidateElement;
        private System.Numerics.Vector2 _mouseDownPos;
        private int _mouseDownButton;
        private const float ClickDistanceThreshold = 4.0f;

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

        public abstract void Init();
        public virtual void OnActivated() { }
        public virtual void OnDeactivated()
        {
            ReleasePointerCapture();
            UpdateCursorForTarget(null);
        }

        public View(string name)
        {
            Name = name;
            _canvas.Changed += ForceLayoutEvaluation;

            Events.OnMouseDown += OnMouseDown;
            Events.OnMouseUp += OnMouseUp;
            Events.OnMouseMove += OnMouseMove;
            Events.OnMouseScroll += OnMouseScroll;
        }

        private void UpdateCursorForTarget(VisualElement? target)
        {
            var cursorElem = target;
            StandardCursor? effectiveCursor = null;
            while (cursorElem != null)
            {
                if (cursorElem.Cursor.HasValue)
                {
                    effectiveCursor = cursorElem.Cursor.Value;
                    break;
                }
                cursorElem = cursorElem.Parent;
            }

            try
            {
                Browser.ChangeCursor(effectiveCursor ?? StandardCursor.Default);
            }
            catch { }
        }

        private void UpdateHoverTarget(VisualElement? target, System.Numerics.Vector2 mousePos)
        {
            if (hoveredElement != target)
            {
                // Gather parent chains
                var oldChain = new List<VisualElement>();
                var curOld = hoveredElement;
                while (curOld != null)
                {
                    oldChain.Add(curOld);
                    curOld = curOld.Parent;
                }

                var newChain = new List<VisualElement>();
                var curNew = target;
                while (curNew != null)
                {
                    newChain.Add(curNew);
                    curNew = curNew.Parent;
                }

                // Trigger MouseLeave for elements in oldChain that are NOT in newChain
                foreach (var el in oldChain)
                {
                    if (!newChain.Contains(el))
                    {
                        el.Events.HandleMouseLeave(el);
                    }
                }

                // Trigger MouseEnter for elements in newChain that are NOT in oldChain
                foreach (var el in newChain)
                {
                    if (!oldChain.Contains(el))
                    {
                        el.Events.HandleMouseEnter(el);
                    }
                }

                hoveredElement = target;
                UpdateCursorForTarget(target);
            }
            else if (target != null && target == hoveredElement)
            {
                UpdateCursorForTarget(target);
                target.Events.HandleMouseHover(target, PointToDesign(mousePos));
            }
        }

        private void OnMouseDown(object _, MouseEventArgs args)
        {
            VisualElement element = Elements.FirstFromPoint(
                new(args.Global.X, args.Global.Y));

            // Find first element walking up the parent chain with ReceivesKeyboard and EffectiveInteractive
            var focusTarget = element;
            while (focusTarget != null && (!focusTarget.ReceivesKeyboard || !focusTarget.EffectiveInteractive))
            {
                focusTarget = focusTarget.Parent;
            }
            SetActiveKeyboardElement(focusTarget);

            // Record candidate for Click policy (in window logical coords for threshold)
            _clickCandidateElement = element;
            _mouseDownPos = args.Global;
            _mouseDownButton = args.Button;

            if (element != null)
            {
                var designPos = PointToDesign(args.Global);
                // Bubble mouse down event
                var current = element;
                while (current != null)
                {
                    var relative = current.PointToClient(args.Global.X, args.Global.Y);
                    var elemArgs = new MouseEventArgs
                    {
                        Button = args.Button,
                        Global = designPos,
                        Relative = relative,
                        Handled = args.Handled
                    };
                    current.Events.HandleMouseDown(elemArgs, current);
                    if (elemArgs.Handled)
                    {
                        args.Handled = true;
                        break;
                    }
                    current = current.Parent;
                }
            }
        }

        private void OnMouseUp(object _, MouseEventArgs args)
        {
            var target = PointerCaptureElement ?? Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));

            if (target != null)
            {
                var designPos = PointToDesign(args.Global);
                var current = target;
                while (current != null)
                {
                    var relative = current.PointToClient(args.Global.X, args.Global.Y);
                    var elemArgs = new MouseEventArgs
                    {
                        Button = args.Button,
                        Global = designPos,
                        Relative = relative,
                        Handled = args.Handled
                    };
                    current.Events.HandleMouseUp(elemArgs, current);
                    if (elemArgs.Handled)
                    {
                        args.Handled = true;
                        break;
                    }
                    current = current.Parent;
                }
            }

            // Click policy: primary button down + up on same element with distance within threshold
            if (args.Button == 0 && _clickCandidateElement != null && target != null)
            {
                bool sameElement = (target == _clickCandidateElement);
                float distance = System.Numerics.Vector2.Distance(_mouseDownPos, args.Global);
                if (sameElement && distance <= ClickDistanceThreshold)
                {
                    var designPos = PointToDesign(args.Global);
                    var current = target;
                    while (current != null)
                    {
                        var relative = current.PointToClient(args.Global.X, args.Global.Y);
                        var clickArgs = new MouseEventArgs
                        {
                            Button = args.Button,
                            Global = designPos,
                            Relative = relative
                        };
                        current.Events.HandleClick(clickArgs, current);
                        if (clickArgs.Handled)
                            break;
                        current = current.Parent;
                    }
                }
            }
            _clickCandidateElement = null;

            // Release pointer capture on mouse up (default)
            if (PointerCaptureElement != null)
            {
                ReleasePointerCapture();
            }

            var currentUnderCursor = Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));
            UpdateHoverTarget(currentUnderCursor, args.Global);
        }

        private void OnMouseMove(object _, MouseEventArgs args)
        {
            var target = PointerCaptureElement ?? Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));

            if (target != null)
            {
                var designPos = PointToDesign(args.Global);
                var current = target;
                while (current != null)
                {
                    var relative = current.PointToClient(args.Global.X, args.Global.Y);
                    var elemArgs = new MouseEventArgs
                    {
                        Button = args.Button,
                        Global = designPos,
                        Relative = relative,
                        Handled = args.Handled
                    };
                    current.Events.HandleMouseMove(elemArgs, current);
                    if (elemArgs.Handled)
                    {
                        args.Handled = true;
                        break;
                    }
                    current = current.Parent;
                }
            }

            if (PointerCaptureElement != null)
            {
                // Freeze hover to capture target during capture
                UpdateHoverTarget(PointerCaptureElement, args.Global);
            }
            else
            {
                UpdateHoverTarget(target, args.Global);
            }
        }

        private void OnMouseScroll(object sender, System.Numerics.Vector2 offset)
        {
            var target = PointerCaptureElement ?? hoveredElement;
            var el = target;
            var args = new MouseScrollEventArgs { Offset = offset, Handled = false };
            while (el != null)
            {
                el.Events.HandleMouseScroll(offset, el, args);
                // Prefer Handled; still stop at ScrollContainer for backward compatibility with old OnMouseScroll handlers.
                if (args.Handled)
                    break;
                if (el is ScrollContainer)
                    break;
                el = el.Parent;
            }
        }

        internal void TriggerLoop() => Loop?.Invoke();

        public void AddElement(VisualElement element)
        {
            if (element == null) return;
            element.ParentView = this;
            TrackElement(ref element);
            foreach (var child in element.Children)
            {
                VisualElement.RegisterSubtree(child, this);
            }
            _hierarchyDirty = true;
            RenderRequired = true;
        }

        public void RemoveElement(VisualElement element)
        {
            if (element == null) return;
            VisualElement.UnregisterSubtree(element, this);
            element.ParentView = null!;
            _hierarchyDirty = true;
            RenderRequired = true;
        }

        /// <summary>
        /// Attaches an embeddable plugin into a destination slot element in host design units.
        /// </summary>
        public void AttachPlugin(PluginRoot plugin, VisualElement slotHost) =>
            PluginEmbed.Attach(plugin, slotHost);

        /// <summary>
        /// Detaches an embedded plugin from its host slot.
        /// </summary>
        public void DetachPlugin(PluginRoot plugin) =>
            PluginEmbed.Detach(plugin);

        public void TrackElement(ref VisualElement element)
        {
            Elements.AddElement(ref element);
            element.AddedToView();
            _hierarchyDirty = true;
            RenderRequired = true;
        }

        public void UntrackElement(ref VisualElement element)
        {
            if (PointerCaptureElement != null && element.ContainsElement(PointerCaptureElement))
            {
                ReleasePointerCapture(PointerCaptureElement);
            }
            if (ActiveKeyboardElement != null && element.ContainsElement(ActiveKeyboardElement))
            {
                SetActiveKeyboardElement(null);
            }
            if (hoveredElement != null && element.ContainsElement(hoveredElement))
            {
                hoveredElement = null;
                UpdateCursorForTarget(null);
            }

            Elements.RemoveElement(element);
            element.RemovedFromView();
            _hierarchyDirty = true;
            RenderRequired = true;
        }

        private void CollectElements(VisualElement root, List<VisualElement> list)
        {
            list.Add(root);
            var sortedChildren = root.GetVisualChildren().Where(c => c != null).OrderBy(c => c.ZIndex).ToList();
            foreach (var child in sortedChildren)
            {
                CollectElements(child, list);
            }
        }

        internal void Render()
        {
            if (Browser.WasResized)
            {
                FullRenderRequired = true;
                // Anchors re-evaluate on resize, but LayoutChildren only runs when layout-dirty.
                // Without this, manual layouts (Kanban columns, modals) stay at old coordinates.
                foreach (var element in Elements.Items)
                {
                    if (element == null) continue;
                    element.InvalidateLayout();
                    element.Transform._transformDirty = true;
                    element.MarkVisibilityClippingDirty();
                    element.ClearRenderCache();
                }
                _hierarchyDirty = true;
            }

            lock (_dirtyRectsLock)
            {
                if (DirtyRects.Count == 0 && !RenderRequired && !FullRenderRequired) return;

                if (FullRenderRequired)
                {
                    // Full redraw required (e.g. view switch or resize)
                    DirtyRects.Clear();
                    // Use framebuffer-sized dirty in logical pixels (RenderRect)
                    float rw = Math.Max(1, Browser.RenderRect.Width);
                    float rh = Math.Max(1, Browser.RenderRect.Height);
                    DirtyRects.Add(new SKRect(0, 0, rw, rh));
                    FullRenderRequired = false;

                    foreach (var element in Elements.Items)
                    {
                        element.ClearRenderCache();
                    }
                }
                else if (RenderRequired && DirtyRects.Count == 0)
                {
                    float rw = Math.Max(1, Browser.RenderRect.Width);
                    float rh = Math.Max(1, Browser.RenderRect.Height);
                    DirtyRects.Add(new SKRect(0, 0, rw, rh));
                }

                _localDirtyRects.Clear();
                _localDirtyRects.AddRange(DirtyRects);
                DirtyRects.Clear();
            }

            RenderRequired = false;

            // Always union dirty rects into one region. Multiple separate clips cause
            // Windows-XP-style trails: background is cleared in a rect but only part of
            // the z-stack is restored under complex multi-path scissors.
            if (_localDirtyRects.Count > 1)
            {
                var unionRect = _localDirtyRects[0];
                for (int i = 1; i < _localDirtyRects.Count; i++)
                    unionRect = SKRect.Union(unionRect, _localDirtyRects[i]);
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

            // Layout and evaluation pass in hierarchical order (parents before children):
            // 1. Evaluate transforms (anchors, relative positions, min/max bounds)
            // 2. Perform layout on dirty nodes (LayoutChildren hook)
            // 3. Evaluate visibility and clipping
            // Layout can produce additional dirty rects (children moving) — merge those before paint.
            for (int idx = 0; idx < CachedRenderQueue.Count; idx++)
            {
                var element = CachedRenderQueue[idx];
                element.UpdateHover(Blossom.Core.Visual.SKSLShaderTimeTracker.DeltaTime);

                if (element.Transform.Evaluate())
                {
                    element.IsDirty = true;
                    element.MarkVisibilityClippingDirty();
                }

                if (element.IsLayoutDirty)
                {
                    element.PerformLayout();
                }

                if (element._visibilityClippingDirty)
                {
                    element.EvaluateVisibilityAndClipping();
                    element._visibilityClippingDirty = false;
                }
            }

            LayoutRequired = false;

            // Fold dirty rects generated during layout/evaluate into this frame's paint set
            lock (_dirtyRectsLock)
            {
                if (DirtyRects.Count > 0)
                {
                    _localDirtyRects.AddRange(DirtyRects);
                    DirtyRects.Clear();
                }
            }

            if (_localDirtyRects.Count == 0)
                return;

            // Final union + padding (shadows, AA, sub-pixel movement)
            {
                var unionRect = _localDirtyRects[0];
                for (int i = 1; i < _localDirtyRects.Count; i++)
                    unionRect = SKRect.Union(unionRect, _localDirtyRects[i]);
                unionRect.Inflate(8, 8);
                // Clamp to view
                float rw = Math.Max(1, Browser.RenderRect.Width);
                float rh = Math.Max(1, Browser.RenderRect.Height);
                unionRect.Intersect(new SKRect(0, 0, rw, rh));
                _localDirtyRects.Clear();
                if (unionRect.Width > 0 && unionRect.Height > 0)
                    _localDirtyRects.Add(unionRect);
                else
                    return;
            }

            using (new SKAutoCanvasRestore(Renderer.Canvas))
            {
                float scaleX = Browser.RenderRect.Width > 0 ? (float)Renderer.FramebufferWidth / Browser.RenderRect.Width : 1f;
                float scaleY = Browser.RenderRect.Height > 0 ? (float)Renderer.FramebufferHeight / Browser.RenderRect.Height : 1f;
                if (scaleX > 0 && scaleY > 0 && (scaleX != 1f || scaleY != 1f))
                {
                    Renderer.Canvas.Scale(scaleX, scaleY);
                }

                var r = _localDirtyRects[0];
                var rounded = SKRect.Create(
                    (int)Math.Floor(r.Left),
                    (int)Math.Floor(r.Top),
                    (int)Math.Ceiling(r.Width) + 2,
                    (int)Math.Ceiling(r.Height) + 2);
                Renderer.Canvas.ClipRect(rounded, SKClipOperation.Intersect, false);

                // Clear dirty region to background
                Renderer.Canvas.DrawColor(BackColor);

                // Painter's algorithm: every visible element that intersects the damage region
                // must redraw (including scroll panels under the cards — not only the top card).
                for (int idx = 0; idx < CachedSortedElements.Count; idx++)
                {
                    var element = CachedSortedElements[idx];
                    if (!element.Visible) continue;
                    // Still draw Clipped elements; only skip fully hidden
                    if (element.ComputedVisibility == Visibility.Hidden) continue;

                    var elementRect = element.RenderBounds;
                    elementRect.Inflate(4, 4);

                    if (!rounded.IntersectsWith(elementRect))
                        continue;

                    element.RenderSingle(Renderer.Canvas);
                    element.IsDirty = false;
                }
            }
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
            LayoutRequired = true;

            foreach (var element in Elements.Items)
            {
                if (element != null)
                {
                    element.InvalidateLayout();
                    element.MarkVisibilityClippingDirty();
                }
            }
        }

        public void Dispose()
        {
            if (_canvas != null)
            {
                _canvas.Changed -= ForceLayoutEvaluation;
            }
            ReleasePointerCapture();
            SetActiveKeyboardElement(null);
            hoveredElement = null;
            UpdateCursorForTarget(null);
            _clickCandidateElement = null;

            var roots = Elements.Items.Where(e => e.Parent == null).ToList();
            foreach (var root in roots)
            {
                root.Dispose();
            }

            var remaining = Elements.Items;
            foreach (var element in remaining)
            {
                element.Dispose();
            }

            Elements.Dispose();
            Events.Dispose();

            lock (_dirtyRectsLock)
            {
                DirtyRects.Clear();
                _localDirtyRects.Clear();
            }

            CachedRenderQueue.Clear();
            CachedSortedElements.Clear();
        }
    }
}