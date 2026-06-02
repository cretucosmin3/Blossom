# Rendering & Allocation Optimizations

Optimized the Blossom rendering pipeline and layout solve engine to eliminate hot-path allocations and resolve rendering bottlenecks during dynamic screen animations.

## Modifications & Optimizations

### 1. Lazy Drop Shadow Image Filters (`src/Visual/Style/Shadow.cs`)
*   **Problem**: Properties like `SpreadX`, `SpreadY`, and `Color` were calling `RedoFilter` on every single mutation. In the animation loop, mutating these properties sequentially caused `SKImageFilter.CreateDropShadow` to be called 3 times per element every frame (900 filters allocated and discarded per frame).
*   **Solution**: Introduced a `_filterDirty` flag. Property setters now simply flag the filter as dirty and trigger the render tick. The drop shadow is only created lazily inside the `Paint` getter exactly once per frame during draw-command logging.
*   **Result**: Reduced drop shadow allocations from 900 to 300 per frame.

### 2. Matrix Cache Reuse & Leaks Fix (`src/Visual/Transform.cs`)
*   **Problem**: `Transform.GetLocalM44()` and `Transform.GetGlobalM44()` allocated new instances of the `SKMatrix44` class every frame for every moving element. Additionally, temporary rotation and perspective matrices were created but never disposed of, resulting in native Skia leaks and GC overhead.
*   **Solution**: 
    *   Mutated the pre-allocated `_cachedLocalM44` and `_cachedGlobalM44` instances in-place using `SetIdentity()` instead of creating new instances.
    *   Properly wrapped temporary rotation and perspective matrix allocations in `using` blocks.
    *   Implemented `IDisposable` on `Transform` to release the cached matrices native resources.
    *   Disposed of `Transform` inside `VisualElement.Dispose()`.

### 3. Reusable Dirty Rect Lists & Canvas Clipping (`src/Core/View.cs`)
*   **Problem**: 
    *   A new `List<SKRect>` was allocated on every frame to copy dirty rects.
    *   When 300 elements moved, Skia clipped the canvas against a complex `SKPath` containing 600 dirty rectangles. Clipping to complex paths forces Skia to perform heavy CPU/GPU rasterization math.
    *   The overlap check looped up to `300 * 600 = 180,000` times in C#.
*   **Solution**:
    *   Replaced the temporary list with a reusable `_localDirtyRects` list.
    *   Union-merged dirty rects into a single bounding box when their count exceeded 10.
    *   Used `canvas.ClipRect()` directly instead of building and clipping an `SKPath` when there is only one dirty rect (e.g. the union rect).
    *   Reduced the element overlap check loops from ~180,000 down to at most 300.

### 4. Layout Solve Stack Comparison (`src/Visual/Transform.cs`)
*   **Problem**: `Transform.Evaluate()` allocated a `new Rect` object on the heap to perform inequality checks of position and size changes, which also suffered from reference-equality issues.
*   **Solution**: Swapped the heap allocation with simple stack-based float comparison (`prevX != Computed.X || prevY != Computed.Y ...`).

### 5. Caching and Bounds Optimizations (`src/Visual/VisualElement.cs`)
*   **Problem**: Repeatedly checking six transform float properties on all ancestors for every element during bounds checks added overhead. Local bounds were also re-evaluated every time.
*   **Solution**: 
    *   Added cached `_has3DTransforms` flag inside `Transform` to avoid redundant float checks.
    *   Added `_cachedLocalBounds` caching inside `VisualElement` to bypass bounds math when style and dimensions are unchanged.

### 6. Wave Drawing Leaks (`src/Testing/Views/DashboardView.cs`)
*   **Problem**: `DrawWaveCommand` allocated linear gradient shaders and blur image filters but did not dispose of them, leaking native Skia resources.
*   **Solution**: Wrapped the allocations in `using` blocks.

### 7. Revert to Safe Dirty Rect Propagation (`src/Visual/VisualElement.cs`)
*   *Note:* Initially, we optimized `IsDirty` to skip dirty rect evaluation when the element was already dirty. However, this caused layout-solve updates during startup or sequential style changes (such as scaling during hover effects) to omit intermediate/final position updates, resulting in visual artifacts/clipping issues. We restored the safe bounds propagation logic, which remains highly performant due to the underlying lazy filter and cached bounds optimizations.

## Verification & Benchmarks

Benchmarks were verified using `./Blossom --benchmark` on a framework-dependent release build.

| Benchmark View | Baseline FPS (Before) | Optimized FPS (After) | Performance Increase |
| :--- | :--- | :--- | :--- |
| **Benchmark - Static Grid** | 218.6 FPS | **343.2 FPS** | **+57.0%** |
| **Benchmark - Dynamic Mutation** | 61.0 FPS | **75.4 FPS** | **+23.6%** |
