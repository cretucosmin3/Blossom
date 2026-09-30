# Blossom

Retained-mode UI framework for **C#**. Native desktop host for rich 2D/3D UI — not an HTML/CSS engine.

- Window + input: **Silk.NET** (GLFW)
- Drawing: **SkiaSharp** (GPU, dirty-rect retained pipeline)
- Apps are C# object graphs of **`VisualElement`** nodes inside **`View`s**, owned by an **`Application`**, hosted by **`Shell`**

`VisualElement` is the platform. Buttons, fields, terminals, and charts are subclasses of that node. Core ships host contracts (window, keyboard, fonts, overlays, layout, threading) so other apps can build those primitives without reflection.

**Guide (capabilities, APIs, pitfalls):** [`guide/README.md`](guide/README.md)

**Blossom.Reactive** is an optional [SolidJS](https://www.solidjs.com/)-inspired layer on the retained tree: signals, memos, effects, keyed lists. See [`src/Blossom.Reactive/README.md`](src/Blossom.Reactive/README.md).

**Blossom.Primitives** is the optional element library (Stack, Grid, Split, `Colours`, `Theme`; later widgets). See [`src/Blossom.Primitives/README.md`](src/Blossom.Primitives/README.md).

## Building and running

Blossom is a **class library** (`src/Blossom`) plus a sample host (`src/Blossom.Demo`). Other C# apps reference the library and call `Shell.Initialize(yourApplication)`.

```bash
dotnet build Blossom.sln
dotnet run --project src/Blossom.Demo/Blossom.Demo.csproj
```

Linux packaging script:

```bash
./build.sh
# --framework-dependent / -fd   needs a machine-wide .NET 10 runtime
# --self-contained / -s         copies the runtime into ./dist/
```

After a script build, `./Blossom` runs the demo. `./Blossom --benchmark` runs isolated benchmark views. `--fps` opts into the F12 stats overlay.

## License

MIT. See [LICENSE](LICENSE).
