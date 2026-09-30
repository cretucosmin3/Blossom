# Drawing and 3D

## Retained paint

Blossom is retained-mode: dirty rects, scissor clip, painter's order (`ZIndex`, tree depth, registration order). Nodes record `DrawCommand`s into a `CommandLedger` and replay them when dirty.

Call `InvalidatePaint` / `ScheduleRender` when state that affects pixels changes. Continuous SKSL backgrounds keep the loop awake; otherwise the host waits (see [Host](host.md)).

`VisualElement.IsAntialias` (default true) controls fills, strokes, clips, and background shaders. Offscreen 2× AA caches must be blitted with a destination rect matching the element size, or a still frame looks zoomed.

## Shaders

`ElementStyle.BackgroundShader` plus `ShaderRenderMode`:

| Mode | Behavior |
|---|---|
| `OnDemand` | Paint once and cache |
| `Continuous` | Re-record every frame; `SKSLShaderTimeTracker.ElapsedSeconds` |

`ElementStyle.ShaderSpeed` (default 1) multiplies time-based shaders. The demo synthwave grid uses `u_time * 0.8 * max(u_speed, 0)`.

Switching Continuous → OnDemand freezes the last live frame if you clear the cache on the assignment. The setter skips no-op mode changes.

## 3D transforms

On `Transform`:

```csharp
el.Transform.TransformOriginX = 0.5f; // local size fraction
el.Transform.TransformOriginY = 0.5f;
el.Transform.RotationX = pitchDegrees;
el.Transform.RotationY = yawDegrees;
el.Transform.RotationZ = rollDegrees;
el.Transform.ScaleX = el.Transform.ScaleY = el.Transform.ScaleZ = 1.07f;
el.Transform.Perspective = 900; // 0 = off
```

`GetLocalM44()` / `GetGlobalM44()` return `SKMatrix44`. Drawing uses `SKCanvas.Concat(in SKMatrix44)` so perspective stays in the 4×4.

### SkiaSharp 4 matrix rules

SkiaSharp 4's `SKMatrix44` is `System.Numerics.Matrix4x4`: **row-vector**, translation in the **last row** (`[3,0]` / `[3,1]`).

| Do | Why |
|---|---|
| Build local matrices with `PostConcat` in the existing order (translate → origin → perspective → Rx, Ry, Rz → scale → −origin) | `PostConcat(A)` is `A * this`, which keeps the origin at the layout center |
| Compose world as `local * parent` | `p_world = p_local * local * parent` |
| Store perspective in **last column** `[2,3] = -1 / distance` | Skia and `Vector4.Transform` divide by `w` |
| `canvas.Concat(in SKMatrix44)` | `.Matrix` is 3×3 and **drops** perspective (a 3D card becomes a parallelogram) |
| Map points with `Transform.MapPoint` / `MapPoint3D` (w-divide) | `SKMatrix44.MapPoint` is `Vector3.Transform` and does **not** divide |
| Hit-test with `Transform.TryUnproject` | Invert + `MapPoint(z: 0)` is the wrong plane; see [Input](input.md) |

Skia 2 `PreConcat` was a left multiply. Calling `PreConcat` on Skia 4 with the old op order shifts the card (demo pose: center near `(428, 101)` instead of the layout center).

`Transform.MapPath` maps a path through the 4×4. `SKPath.Transform` is 3×3 only.

## Hit-testing 3D children

A child of a rotated card has `GetGlobalM44() = childLocal * parent3D`. Unproject the **screen** point through that matrix. Round-trip: map local `(0,0)` to screen, unproject, get `(0,0)` again. Invert-z0 on the same pixel can land tens of pixels off (demo “Hit in 3D” button: `(0,0)` became about `(60, -30)`).
