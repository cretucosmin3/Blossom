# Blossom

Retained-mode UI framework for **C#**. Native desktop host for rich 2D/3D UI — not an HTML/CSS engine.

- Window + input: **Silk.NET** (GLFW)
- Drawing: **SkiaSharp** (GPU, dirty-rect retained pipeline)
- Apps are C# object graphs of **`VisualElement`** nodes inside **`View`s**, owned by an **`Application`**, hosted by **`Shell`**

`VisualElement` is the platform. Buttons, fields, terminals, and charts are subclasses of that node. Core ships host contracts (window, keyboard, fonts, overlays, layout, threading) so other apps can build those primitives without reflection.

**Guide (capabilities, APIs, pitfalls):** [`guide/README.md`](guide/README.md)

**Blossom.Reactive** is the [SolidJS](https://www.solidjs.com/)-inspired layer on the retained tree: signals, memos, effects, keyed lists. See [`src/Blossom.Reactive/README.md`](src/Blossom.Reactive/README.md).

**Blossom.Primitives** is the element library (Stack, Grid, Split, `Colours`, `Theme`; later widgets). See [`src/Blossom.Primitives/README.md`](src/Blossom.Primitives/README.md).

The published NuGet id is **`Blossom`**. One package contains `Blossom.dll`, `Blossom.Reactive.dll`, and `Blossom.Primitives.dll`.

```bash
dotnet add package Blossom
```

Pack from this repo:

```bash
dotnet pack src/Blossom.Pack/Blossom.Pack.csproj -c Release
```

The nupkg lands in `artifacts/`. Version comes from git (`version.json`): patch increments on every commit; **edit `"version"` there only for a minor or major bump** (for example `"1.0"`). GitHub Actions (`.github/workflows/nuget.yml`) packs on `main`, pull requests, and `v*.*.*` tags. A version tag also opens a GitHub Release with the nupkg.

nuget.org uses **Trusted Publishing**: on nuget.org, **Trusted Publishing** → policy with this GitHub repo, workflow file `nuget.yml`, environment empty. Push happens on `v*.*.*` tags or a manual workflow run with **Push to nuget.org**.

## Building and running

Blossom is a **class library** (`src/Blossom`) plus reactive and primitives libraries and a sample host (`src/Blossom.Demo`). Other C# apps reference the `Blossom` package (or the three project references in this repo) and call `Shell.Initialize(yourApplication)`.

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
