# Getting started

## Stack

| Piece | Version |
|---|---|
| Language / TFM | C#, **.NET 10** (`net10.0`) |
| Window / input | Silk.NET **2.23.0** (GLFW) |
| Graphics | SkiaSharp **4.153.0** + `SkiaSharp.NativeAssets.Linux` |
| Images | ImageSharp **3.1.12**, Svg.Skia **5.2.3** |
| Solution | `Blossom.sln` — library, `Blossom.Reactive`, demo |

Linux uses `SkiaSharp.NativeAssets.Linux` (fontconfig). Do not switch to `NoDependencies` unless you are building a stripped container and you register every face yourself.

## Build and run

From the repo root:

```bash
dotnet build Blossom.sln
dotnet run --project src/Blossom.Demo/Blossom.Demo.csproj
# optional: --builder | --benchmark | --fps
```

Packaging script (Linux): `./build.sh` (see the script for `--self-contained` / `--framework-dependent`).

`--fps` / `--show-fps` / `--debug-overlay` set both `Application.EnableStatsOverlay` and `Shell.ShowDebugOverlay` so F12 is reserved and the overlay starts visible.

## Consume the library

Reference `src/Blossom/Blossom.csproj` (and optionally `src/Blossom.Reactive`). Then:

```csharp
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;

public sealed class MyApplication : Application
{
    public MyApplication()
    {
        Title = "My app";
        Window.Width = 1280;
        Window.Height = 800;
        Window.MinWidth = 800;
        Window.MinHeight = 500;
        Window.CenterOnLoad = true;
        // EnableStatsOverlay = true; // F12 becomes Blossom's

        var view = new MyView();
        AddView(view);
        SetActiveView(view);
    }
}

public sealed class MyView : View
{
    public MyView() : base("Main") { }

    public override void Init()
    {
        var root = new VisualElement { Name = "Root" };
        root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        AddElement(root);
        // children...
    }
}

Shell.Initialize(new MyApplication());
```

`View.Width` / `Height` track the window client size. Stretch anchors on the root fill that rect.

## Repository layout

```text
Blossom/
├── guide/                  # this guide
├── src/Blossom/            # framework (class lib); Shell.cs is the host
├── src/Blossom.Reactive/   # optional SolidJS-inspired signals
├── src/Blossom.Primitives/ # optional layout hosts (Stack, Grid, Split)
├── src/Blossom.Demo/       # Blossom Studio (not the public API)
├── tests/Blossom.Reactive.Tests/
├── assets/                 # fonts, images (copied to demo output)
└── README.md
```
