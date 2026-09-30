# Blossom.Reactive

Fine-grained reactive state for Blossom’s retained `VisualElement` tree. The design is **inspired by [SolidJS](https://www.solidjs.com/)**: signals, memos, effects, batching, and keyed list / conditional flow — without a virtual DOM.

Blossom already owns a live element graph. This package mutates those nodes in place when state changes. There is no render function that returns a new tree, and no diff of two trees.

```text
Signal / Memo  →  Effect  →  VisualElement property or child list
```

The demo host (`src/Blossom.Demo`) uses this layer in **Blossom Studio**: the Board tab (`For.Each` kanban) and the Controls tab (two-way bindings, `Batch`, derived validation). Framework host contracts (window, keyboard, overlays, 3D) are in [`guide/`](../../guide/README.md).

---

## Why this shape

SolidJS tracks **which computations read which values**, then re-runs only those computations. Blossom.Reactive does the same on the CLR:

| SolidJS | Blossom.Reactive |
|---|---|
| `createSignal` | `ReactiveEngine.CreateSignal` / `Signal<T>` |
| `createMemo` | `ReactiveEngine.CreateMemo` / `Memo<T>` |
| `createEffect` | `ReactiveEngine.CreateEffect` / `Effect` |
| `batch` | `ReactiveEngine.Batch` |
| `untrack` | `ReactiveEngine.Untrack` |
| `onCleanup` / owner | `EffectScope` |
| `<Show>` | `Show.When` |
| `<For>` | `For.Each` |

The SolidJS analog of JSX is **C# object construction** plus `Bind*` helpers. A “component” is a class (or factory) that builds `VisualElement`s and wires signals. Disposing an element disposes the effects attached to it.

---

## Install

Shipped in the **`Blossom`** NuGet package together with core and primitives.

```bash
dotnet add package Blossom
```

From this repo, project-reference `src/Blossom.Reactive/Blossom.Reactive.csproj` next to core.

```csharp
using Blossom.Reactive;
using static Blossom.Reactive.ReactiveEngine;
```

Target framework is **.NET 10**. Tests live in `tests/Blossom.Reactive.Tests`.

---

## Primitives

### `Signal<T>`

A cell. **Reading** `Value` (or `Get()`) while an effect is running subscribes that effect. **Writing** notifies subscribers if the value changed according to `IEqualityComparer<T>` (default equality).

```csharp
var count = CreateSignal(0);

count.Value++;           // notifies
count.Set(1);            // no-op if still 1
count.Update(n => n + 1);

int n = count.Peek();    // read without subscribing
```

`Signal<T>` converts implicitly to `T` via `Value` (that **does** track).

Tuple style, closer to Solid’s `[get, set]`:

```csharp
var (count, setCount) = CreateSignalPair(0);
```

### `Memo<T>`

A derived value. It recomputes when an upstream signal or memo it read last time goes stale, and it notifies **downstream only if the computed result changed**.

```csharp
var first = CreateSignal("Ada");
var last  = CreateSignal("Lovelace");
var name  = CreateMemo(() => $"{first.Value} {last.Value}");

// name.Value is cached until first or last changes
```

Memos use a tracker effect internally. They do not re-run user effects just because an input changed if the output is equal.

### `Effect`

A side effect that re-runs when its **current** dependencies change. Dependencies are rebuilt every run (dynamic graph): a branch you did not read this time is dropped.

```csharp
var toggle = CreateSignal(true);
var a = CreateSignal("A");
var b = CreateSignal("B");

using var fx = CreateEffect(() =>
{
    var text = toggle.Value ? a.Value : b.Value;
    label.Text = text;
});
```

While `toggle` is `true`, writes to `b` do not re-run the effect.

Effects register themselves with the ambient `EffectScope` (if any) and are `IDisposable`.

An effect that writes a signal it also read is **requeued after the current run** instead of recursing. A cycle that never settles throws after 10 000 flush iterations.

### `Batch` and `Untrack`

```csharp
Batch(() =>
{
    title.Value = "Blossom Studio";
    roundness.Value = 14f;
    yaw.Value = 22f;
}); // dependent effects run once

var snapshot = Untrack(() => expensive.Value); // read without subscribing
```

Writes inside `Batch` still update signal values immediately; only **effect execution** is deferred until the batch (and any in-flight notify/execute) finishes.

---

## Binding to `VisualElement`

Effects that touch the tree should be owned by an element so they die with it.

| Helper | Role |
|---|---|
| `element.Bind(el => …)` | Scoped effect; re-runs when signals read inside change |
| `BindText(() => …)` | Sets `Text` |
| `BindVisible(() => …)` | Sets `Visible` |
| `BindOpacity(() => …)` | Sets `Opacity` |
| `BindBackColor(() => …)` | Sets `Style.BackColor` |
| `BindBorderRoundness(() => …)` | Sets `Style.Border.Roundness` |
| `BindLifecycle(disposable)` | Dispose with the element |
| `GetReactiveScope()` | The `EffectScope` stored on the element |

```csharp
title.BindText(() => heading.Value);
error.BindVisible(() => validation.Value.Length > 0);

card.Bind(c =>
{
    c.Transform.RotationY = yaw.Value;
    c.Style.Border.Roundness = roundness.Value;
});
```

`Bind` / `For.Each` / `Show.When` all attach to the element’s reactive scope. `Dispose()` on the element stops updates.

**Two-way controls.** Push from the widget into the signal, and pull from the signal only when the value actually differs, so the caret or thumb is not reset on every keystroke:

```csharp
field.Changed += v => text.Value = v ?? "";
field.Bind(_ =>
{
    if (field.Value != text.Value)
        field.Value = text.Value;
});
```

On `Button`, bind `NormalColor` rather than `BindBackColor`. `BindBackColor` fights hover and press colors.

Do not allocate a new `TextStyle` (or other style objects that own `SKPaint`) on every bind tick; mutate the existing style.

---

## Flow: `Show.When` and `For.Each`

### Conditional mount

```csharp
Show.When(
    parent,
    condition: () => isOpen.Value,
    contentFactory: () => new ModalPanel(),
    fallbackFactory: () => new VisualElement { Name = "Closed" } // optional
);
```

When the boolean changes, the previous branch is **removed and disposed**, including its `EffectScope`. Factories run inside `EffectScope.Record`, so effects created while building the branch are cleaned up on unmount.

### Keyed lists

```csharp
For.Each<TaskItem, Guid>(
    parent: scroller,
    items: () => columnTasks.Value,
    keySelector: item => item.Id,
    template: item => new TaskCard(item, tasks));
```

- Keys must be unique. Duplicate keys: **the first item with that key wins**; later duplicates are skipped.
- Existing keys keep their `VisualElement` (state, focus, bindings).
- Missing keys unmount (remove + dispose element + scope).
- Order follows the item list (`InsertChild` / `SetChildIndex`).
- `startIndex` leaves leading children alone (static chrome in the same parent). Prefer putting list items in a dedicated parent so chrome is not mixed with reconciled children.

The template runs **once per key** at mount. If `T` is a mutable class, the card should `Bind` to signals (or a store) rather than assume the template re-runs.

`For.Each` calls `InvalidateLayout()` on the parent after reconcile.

---

## `EffectScope`

Owner analog. Nested scopes:

```csharp
var (scope, panel) = EffectScope.Record(() => BuildPanel());
// effects constructed inside BuildPanel() are registered on scope
scope.Dispose(); // disposes those effects
```

`Effect` constructors call `EffectScope.Current?.Register(this)`. `Show` / `For` use `Record` so a removed card does not leak subscriptions.

---

## Runtime model

Tracking is **thread-static** (`ReactiveContext`). Use the API on the UI thread that owns the view.

```text
Write signal
  → if value changed, BeginNotify → Effect.Notify
       → user effects enqueue
       → memo trackers recompute; notify only if output changed
  → EndNotify → Flush (unless batching or an effect is still running)

Flush
  → Execute each pending effect once (deduped)
  → Execute rebuilds the dependency set
  → writes during Execute queue more work until the current effect finishes
```

There is no scheduler that paints. Bindings that change `Text`, `Visible`, transforms, or style call into Blossom’s existing dirty-rect path (`InvalidatePaint` / `InvalidateLayout`). Idle sleep still applies when nothing is dirty.

---

## Layout of this project

```text
src/Blossom.Reactive/
├── Signals/     ReactiveEngine, Signal, Memo, Effect, ReactiveContext
├── Lifecycle/   EffectScope
├── Bindings/    VisualElement Bind* extensions
└── Flow/        Show.When, For.Each
```

`Blossom.Reactive` references `Blossom` because `For` / `Show` / `Bind*` talk to `VisualElement`. The signal core is otherwise independent of drawing.

---

## Conventions

- Treat this as a **meta-framework on top of the element tree**, not a replacement for `VisualElement`.
- Prefer `Bind` on existing nodes over tearing down subtrees.
- Use `For.Each` keys that are stable for the lifetime of the row (`Guid`, primary id).
- `Peek()` / `Untrack` when a read should not subscribe (logging, measuring, one-shot init).
- Equality: pass a comparer to `CreateSignal` / `CreateMemo` for sequences or records that need structural compare. Default is `EqualityComparer<T>.Default` (reference equality for most classes). A `List<T>` signal will **not** notify if you mutate the list in place; assign a new list (or a version signal).
- `Signals.Cell` / `Signals.Effect` / `Signals.Memo` are short aliases for the `ReactiveEngine` factories.

---

## See also

- Demo: `src/Blossom.Demo/Tabs/TasksTab.cs`, `src/Blossom.Demo/Tabs/ControlsTab.cs`
- Tests: `tests/Blossom.Reactive.Tests/ReactiveTests.cs`
- SolidJS mental model: [https://docs.solidjs.com/concepts/intro-to-reactivity](https://docs.solidjs.com/concepts/intro-to-reactivity)
