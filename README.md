<div align="center">
  <img alt="Blossom" src="assets/icon.png" height="110">

<table width="600" align="center">
  <tr>
    <td align="center">
      <strong>Blossom is a retained-mode UI framework for C#. Native desktop host for 2D/3D UI; apps are VisualElement trees inside Views, hosted by Shell.</strong>
    </td>
  </tr>
</table>

[![NuGet](https://github.com/cretucosmin3/Blossom/actions/workflows/nuget.yml/badge.svg)](https://github.com/cretucosmin3/Blossom/actions/workflows/nuget.yml)
[![GitHub last commit](https://img.shields.io/github/last-commit/cretucosmin3/Blossom.svg)](https://github.com/cretucosmin3/Blossom/commits/main)
[![GitHub stars](https://img.shields.io/github/stars/cretucosmin3/Blossom.svg)](https://github.com/cretucosmin3/Blossom/stargazers)
[![NuGet](https://img.shields.io/nuget/v/Blossom.svg)](https://www.nuget.org/packages/Blossom)

</div>

## Install

```bash
dotnet add package Blossom
```

| | |
|---|---|
| Package id | `Blossom` |
| Assemblies | `Blossom.dll`, `Blossom.Reactive.dll`, `Blossom.Primitives.dll` |
| TFM | `net10.0` |

From this repo, project-reference `src/Blossom`, `src/Blossom.Reactive`, and `src/Blossom.Primitives`, then `Shell.Initialize(yourApplication)`.

## Projects

| Path | Role |
|---|---|
| [`src/Blossom`](src/Blossom) | Window, tree, draw, input |
| [`src/Blossom.Reactive`](src/Blossom.Reactive/README.md) | Signals, memos, effects, `Bind*`, `For.Each`, `Show.When` |
| [`src/Blossom.Primitives`](src/Blossom.Primitives/README.md) | Stack, Grid, Split, `Colours`, `Theme` |
| [`src/Blossom.Demo`](src/Blossom.Demo) | Sample host (Blossom Studio) |
| [`src/Blossom.Pack`](src/Blossom.Pack) | Packs the three libraries into one nupkg |

## Build

| | |
|---|---|
| Solution | `dotnet build Blossom.sln` |
| Demo | `dotnet run --project src/Blossom.Demo/Blossom.Demo.csproj` |
| Pack | `dotnet pack src/Blossom.Pack/Blossom.Pack.csproj -c Release` → `artifacts/` |
| Linux | `./build.sh` (`--self-contained` / `--framework-dependent`) |

Demo flags: `--benchmark`, `--fps`.

## Docs

| | |
|---|---|
| Guide | [`guide/README.md`](guide/README.md) |
| Reactive | [`src/Blossom.Reactive/README.md`](src/Blossom.Reactive/README.md) |
| Primitives | [`src/Blossom.Primitives/README.md`](src/Blossom.Primitives/README.md) |

## Versioning

| | |
|---|---|
| Source | `version.json` (`"0.1"` → `0.1.{git height}`) |
| Patch | Automatic per commit |
| Minor / major | Edit `"version"` (e.g. `"1.0"`) |
| Publish | Tag `v*.*.*` → GitHub Release + nuget.org (workflow `nuget.yml`) |

## License

MIT. See [LICENSE](LICENSE).

## Built with

| | |
|---|---|
| [Silk.NET](https://github.com/dotnet/Silk.NET) 2.23.0 | Window + input (GLFW) |
| [SkiaSharp](https://github.com/mono/SkiaSharp) 4.153.0 | GPU drawing (`SkiaSharp.NativeAssets.Linux`) |
| [ImageSharp](https://github.com/SixLabors/ImageSharp) 3.1.12 | Raster images |
| [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) 5.2.3 | SVG |
