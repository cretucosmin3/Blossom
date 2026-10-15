<div align="center">
  <img alt="Blossom" src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/assets/icon.png" height="110">

<table width="600" align="center">
  <tr>
    <td align="center">
      <strong>A C# UI framework for desktop apps and game UI, drawn on the GPU with Skia.</strong>
    </td>
  </tr>
</table>

[![NuGet](https://github.com/cretucosmin3/Blossom/actions/workflows/nuget.yml/badge.svg)](https://github.com/cretucosmin3/Blossom/actions/workflows/nuget.yml)
[![GitHub last commit](https://img.shields.io/github/last-commit/cretucosmin3/Blossom.svg)](https://github.com/cretucosmin3/Blossom/commits/main)
[![GitHub stars](https://img.shields.io/github/stars/cretucosmin3/Blossom.svg)](https://github.com/cretucosmin3/Blossom/stargazers)
[![NuGet](https://img.shields.io/nuget/v/Blossom.svg)](https://www.nuget.org/packages/Blossom)

</div>

Blossom is a retained-mode UI framework for C#. A UI is a tree of `VisualElement`s inside a `View`; Blossom redraws only the areas that changed and doesn't render at all while the app is idle. There's no HTML or CSS. The core handles layout, styling, input and drawing, and controls like buttons, text fields and sliders are built on top of it, as in the demos below.

Blossom runs in two ways:

- **As a desktop app.** Blossom opens the window (GLFW through Silk.NET) and handles input.
- **Inside a game.** Your engine owns the window and the loop. Blossom draws into a texture you give it and takes input you pass in. The core libraries don't depend on Silk.NET or GLFW.

## What you get

- Elements with anchors, transforms (including 3D with correct hit-testing), opacity, clipping and z-order
- Styles: fills, borders, rounded corners, shadows, text, images, SVG, backdrop blur and SKSL shaders
- Input: pointer capture, hover, click, scroll, keyboard and text entry
- Scroll containers with scrollbars, and overlays for dialogs and popups
- Animations that run on UI time, so they play at the same speed at any frame rate
- Signals for state (`Blossom.Reactive`): bind a property to a value and it updates when the value changes; `For.Each` and `Show.When` keep lists and conditional parts in sync
- Layout hosts (`Stack`, `Grid`, `Split`) and a hook for writing your own

## Application demo

`demos/ApplicationDemo` is a small app with a sidebar, four pages and a dark/light theme with six accent colours. Every control in it is built in the demo on top of Blossom's primitives.

<p align="center">
  <img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/gallery-overview.png" alt="Overview page with stat cards, a bar chart and an activity feed" width="820">
</p>

<table>
  <tr>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/gallery-components.png" alt="Buttons, text fields, toggles and sliders"></td>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/gallery-tasks.png" alt="Task board with three columns"></td>
  </tr>
  <tr>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/gallery-playground.png" alt="Shader background with a glass card rotated in 3D"></td>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/gallery-light.png" alt="Overview page in light mode with a green accent"></td>
  </tr>
</table>

The Playground page has animated SKSL shader backgrounds and a blurred glass card rotated with a perspective matrix. The button on the card still takes clicks in the right place.

```bash
dotnet run --project demos/ApplicationDemo/ApplicationDemo.csproj
```

## Game UI demo

`demos/Snake.MonoGame` is Snake in MonoGame. MonoGame draws the board; every piece of UI (top bar, menus, score popups, level-up banner, perf panel) is Blossom, drawn into a GPU texture through [SkiaGameRendering](https://www.nuget.org/packages/SkiaGameRendering). The UI only repaints when something on it changes.

<table>
  <tr>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/snake-menu.png" alt="Snake main menu"></td>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/snake-play.png" alt="Snake gameplay with a score popup and the perf panel"></td>
  </tr>
  <tr>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/snake-pause.png" alt="Pause menu"></td>
    <td><img src="https://raw.githubusercontent.com/cretucosmin3/Blossom/main/guide/images/snake-over.png" alt="Game over screen with a new best score"></td>
  </tr>
</table>

```bash
dotnet run --project demos/Snake.MonoGame/Snake.MonoGame.csproj
```

See [`guide/game-ui.md`](guide/game-ui.md) for how to embed Blossom in your own engine.

## Install

```bash
dotnet add package Blossom
```

The `Blossom` package (`net10.0`) contains four libraries: `Blossom` (core), `Blossom.Desktop` (window host), `Blossom.Reactive` and `Blossom.Primitives`.

A minimal desktop app:

```csharp
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Desktop;
using SkiaSharp;

Shell.Initialize(new HelloApp());

sealed class HelloApp : DesktopApplication
{
    public HelloApp()
    {
        Title = "Hello";
        var view = new HelloView();
        AddView(view);
        SetActiveView(view);
    }
}

sealed class HelloView : View
{
    public HelloView() : base("Main") { }

    public override void Init()
    {
        var label = new VisualElement { Text = "Hello, Blossom" };
        label.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        label.Style.BackColor = new SKColor(30, 30, 46);
        label.Style.Text.Color = SKColors.White;
        label.Style.Text.Size = 28;
        label.Style.Text.Alignment = TextAlign.Center;
        AddElement(label);
    }
}
```

To work from this repo instead, reference `src/Blossom`, `src/Blossom.Desktop`, `src/Blossom.Reactive` and `src/Blossom.Primitives`. A game only needs the three without `Blossom.Desktop`.

## Projects

- [`src/Blossom`](src/Blossom): element tree, layout, styles, drawing, input, animations, headless `UiSession`
- [`src/Blossom.Desktop`](src/Blossom.Desktop): desktop window host (`Shell`, `DesktopApplication`)
- [`src/Blossom.Reactive`](src/Blossom.Reactive/README.md): signals, memos, effects, `Bind*`, `For.Each`, `Show.When`
- [`src/Blossom.Primitives`](src/Blossom.Primitives/README.md): `Stack`, `Grid`, `Split`, `Colours`, `Theme`, control shells
- [`src/Blossom.Pack`](src/Blossom.Pack): packs the four libraries into the `Blossom` NuGet package
- [`demos/ApplicationDemo`](demos/ApplicationDemo): the desktop demo above
- [`demos/Snake.MonoGame`](demos/Snake.MonoGame/README.md): the game demo above

## Build

```bash
dotnet build Blossom.sln
dotnet pack src/Blossom.Pack/Blossom.Pack.csproj -c Release   # → artifacts/
./build.sh   # Linux; --self-contained / --framework-dependent
```

Application demo flags: `--benchmark`, `--no-fps` (hides the stats panel), `--max-fps <n>`.

## Docs

- Guide: [`guide/README.md`](guide/README.md)
- Reactive: [`src/Blossom.Reactive/README.md`](src/Blossom.Reactive/README.md)
- Primitives: [`src/Blossom.Primitives/README.md`](src/Blossom.Primitives/README.md)

## Versioning

`version.json` (`"0.1"` → `0.1.{git height}`). Patch increments per commit. Edit `"version"` for a minor or major line (e.g. `"1.0"`). Tag `v*.*.*` for a GitHub Release and nuget.org push (`nuget.yml`).

## Built with

- [Silk.NET](https://github.com/dotnet/Silk.NET) 2.23.0: window and input (GLFW), desktop host only
- [SkiaSharp](https://github.com/mono/SkiaSharp) 4.153.0: GPU drawing
- [ImageSharp](https://github.com/SixLabors/ImageSharp) 3.1.12: raster images
- [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) 5.2.3: SVG

## License

MIT. See [LICENSE](LICENSE).
