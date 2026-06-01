# 3D Transformations & Hit Testing in Blossom UI

To bring modern CSS-like 3D transformations (rotation on all axes, scaling, translation, and perspective projection) to the Blossom UI rendering engine, we must address two key pillars:
1. **Graphics Pipeline**: How to render 3D-transformed 2D elements using SkiaSharp.
2. **Input Pipeline (Hit Testing)**: How to determine if the cursor intersects a 3D-transformed quad (preserving mouse hits).

---

## 1. 3D Graphics Pipeline with SkiaSharp

SkiaSharp natively supports 4x4 matrices via the `SKM44` class. This allows us to represent 3D translations, scaling, rotation (along X, Y, and Z axes), and perspective.

### The Transformation Matrix ($M$)

Each visual element's local transformation matrix $M_{local}$ can be computed as:
$$M_{local} = T(x, y) \cdot T(origin) \cdot R_x(\theta_x) \cdot R_y(\theta_y) \cdot R_z(\theta_z) \cdot S(s_x, s_y, s_z) \cdot T(-origin)$$

Where:
- $T(x, y)$ is the 2D layout translation.
- $T(origin)$ translates to the transform-origin point (typically center `(Width/2, Height/2)`).
- $R_x, R_y, R_z$ are the rotation matrices around the respective axes.
- $S(s_x, s_y, s_z)$ is the scale matrix.

The **global transformation matrix** $M_{global}$ is the concatenation of the parent's global matrix and the element's local matrix:
$$M_{global} = M_{parent\_global} \cdot M_{local}$$

### Rendering in Skia

In `VisualElement.cs`, instead of translating the canvas linearly with `targetCanvas.Translate(X, Y)`, we apply the 3D matrix using `SKCanvas.Concat(ref SKM44)`:

```csharp
using (new SKAutoCanvasRestore(targetCanvas))
{
    // Retrieve the 4x4 matrix
    SKM44 matrix = Transform.GetLocalM44();
    targetCanvas.Concat(ref matrix);

    // Render the element's draw ledger commands in its local coordinate space (0, 0 to Width, Height)
    for (int i = 0; i < cmds.Count; i++)
    {
        cmds[i].Execute(targetCanvas);
    }
}
```

---

## 2. 3D Hit Testing & Preserving Mouse Hits

When an element is rotated in 3D space, its screen-space projection is a non-axis-aligned quad (or arbitrary polygon due to perspective). Standard axis-aligned boundary box (AABB) checks like `SKRect.Contains(x, y)` will fail.

To perform correct hit testing, we must project the screen/cursor point back onto the element's local $z = 0$ plane.

### The Ray-Plane Intersection Algorithm

1. **Construct the Screen Ray**:
   Under orthographic projection (standard UI), the cursor position $P_{screen} = (x_{mouse}, y_{mouse}, 0)$ cast as a ray has:
   - **Ray Origin** $R_o = (x_{mouse}, y_{mouse}, -1000)$
   - **Ray Direction** $R_d = (0, 0, 1)$ (pointing straight down the Z-axis into the screen)

2. **Transform Ray to Local Space**:
   Using the inverse of the element's global transformation matrix $M_{global}^{-1}$, transform the ray origin and direction:
   $$R_{o\_local} = M_{global}^{-1} \cdot R_o$$
   $$R_{d\_local} = M_{global}^{-1} \cdot R_d$$

3. **Intersect with Local Plane ($z = 0$)**:
   The local plane equation for the element is $z = 0$, meaning the plane normal is $N = (0, 0, 1)$ and a point on the plane is $P_0 = (0, 0, 0)$.
   
   To find the intersection distance $t$ along the ray:
   $$t = \frac{-R_{o\_local}.Z}{R_{d\_local}.Z}$$
   
   If $R_{d\_local}.Z$ is close to 0, the ray is parallel to the element's plane, and no hit occurs.

4. **Compute Local Hit Point**:
   Multiply the local ray direction by $t$ and add it to the local ray origin:
   $$P_{local} = R_{o\_local} + t \cdot R_{d\_local}$$

5. **Bounds Verification**:
   The hit point is successful if the local coordinates lie within the bounds:
   $$0 \le P_{local}.X \le Width$$
   $$0 \le P_{local}.Y \le Height$$

### Implementation Blueprint for `ElementTree.cs`

```csharp
public VisualElement FirstFromPoint(float x, float y)
{
    var elements = Map.Values.Select(x => x.Item1)
        .OrderByDescending(e => e.ZIndex)
        .ThenByDescending(e => e.Layer)
        .ToList();

    foreach (var element in elements)
    {
        if (element.ComputedVisibility == Visibility.Hidden)
            continue;

        // Obtain inverse global transform matrix
        SKM44 globalMatrix = element.Transform.GetGlobalM44();
        if (!globalMatrix.TryInvert(out SKM44 invGlobal))
            continue; // Singular matrix, cannot hit test

        // Ray origin & direction in screen space
        SKPoint3 rayOrigin = new SKPoint3(x, y, -1000f);
        SKPoint3 rayDir = new SKPoint3(0f, 0f, 1f);

        // Map ray to element's local space
        SKPoint3 localOrigin = invGlobal.MapPoint(rayOrigin);
        SKPoint3 localDir = invGlobal.MapPoint(rayOrigin + rayDir) - localOrigin;

        // If ray is parallel to plane, skip
        if (Math.Abs(localDir.Z) < 1e-6f)
            continue;

        // Calculate intersection factor t where localOrigin.Z + t * localDir.Z = 0
        float t = -localOrigin.Z / localDir.Z;
        
        // Discard intersection behind ray origin
        if (t < 0) continue;

        // local intersection point
        float localX = localOrigin.X + t * localDir.X;
        float localY = localOrigin.Y + t * localDir.Y;

        // Check if inside local rectangular boundary
        if (localX >= 0 && localX <= element.Transform.Width &&
            localY >= 0 && localY <= element.Transform.Height)
        {
            if (!element.IsClickthrough)
                return element;
        }
    }
    return default!;
}
```

---

## 3. High Performance Optimizations

1. **Caching Matrices**: Recomputing `SKM44` matrices on every frame is expensive. The matrices should be lazily cached, marked dirty when properties like `X`, `Y`, `Width`, `Height`, `RotationX/Y/Z`, or `ScaleX/Y/Z` change, and updated top-down.
2. **Axis-Aligned Bounding Box (AABB) Fast Pass**: Before performing matrix inversion and ray-plane intersection (which are mathematically heavy), project the 8 corner vertices of the transformed element into screen space to compute its AABB. If the cursor is outside this AABB, we can immediately discard the hit test without executing the inversion.
3. **Perspective Matrix Integration**: If the parent element specifies a `perspective` value (like `perspective: 800px`), apply the perspective projection factor $\frac{1}{1 - z/d}$ to the 4x4 matrix during calculation to generate the dramatic 3D distance depth effect.

---

## 4. Clipping & Anchors Stability Under 3D Transforms

### Anchor Stability
**Status: 100% Stable**

Anchors calculate layout positions and dimensions (e.g. `X`, `Y`, `Width`, `Height`) relative to the parent element. 
- 3D transforms are visual **post-layout effects** (identical to CSS transforms).
- The layout engine runs first, determining the 2D bounding boxes using anchors.
- The 3D matrix transformation is applied on top of these computed layout boundaries during drawing and hit testing.
- Consequently, anchors will function perfectly and are unaffected by 3D transforms.

### Clipping Stability
**Status: Requires Pipeline Modification to be Stable**

Currently, `ApplyClippingHierarchy` queries `GetOrCreateRoundRect()` which constructs clipping paths in absolute global screen coordinates. 
If an ancestor has a 3D rotation, an axis-aligned screen-space clip boundary will clip the child incorrectly.

**To make clipping stable:**
1. Clip regions must be applied dynamically relative to each ancestor's coordinate space.
2. In the rendering loop, instead of applying all clips in global screen space at the end, the canvas must clip to the ancestor's local boundaries *after* applying the ancestor's local transform matrix, but *before* applying the descendant's transform.
3. For hit testing, when walking down the tree to hit test a point, the coordinate intersection must also be validated against any clipping ancestor's local bounds by transforming the ray into the clipping ancestor's local space.

