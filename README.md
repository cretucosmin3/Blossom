<div align="center">
  <img alt="Blossom" src="assets/icon.png" height="110">

<table width="600" align="center">
  <tr>
    <td align="center">
      <strong>Blossom is a new way to build fast, efficient C# desktop apps with a web-like look and feel.</strong>
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

The `Blossom` package (`net10.0`) includes `Blossom.dll`, `Blossom.Reactive.dll`, and `Blossom.Primitives.dll`.

From this repo, project-reference `src/Blossom`, `src/Blossom.Reactive`, and `src/Blossom.Primitives`, then `Shell.Initialize(yourApplication)`.

## Projects

- [`src/Blossom`](src/Blossom) — window, tree, draw, input
- [`src/Blossom.Reactive`](src/Blossom.Reactive/README.md) — signals, memos, effects, `Bind*`, `For.Each`, `Show.When`
- [`src/Blossom.Primitives`](src/Blossom.Primitives/README.md) — Stack, Grid, Split, `Colours`, `Theme`
- [`src/Blossom.Demo`](src/Blossom.Demo) — sample host (Blossom Studio)
- [`src/Blossom.Pack`](src/Blossom.Pack) — packs the three libraries into one nupkg

## Build

```bash
dotnet build Blossom.sln
dotnet run --project src/Blossom.Demo/Blossom.Demo.csproj
dotnet pack src/Blossom.Pack/Blossom.Pack.csproj -c Release   # → artifacts/
./build.sh   # Linux; --self-contained / --framework-dependent
```

Demo flags: `--benchmark`, `--fps`.

## Docs

- Guide: [`guide/README.md`](guide/README.md)
- Reactive: [`src/Blossom.Reactive/README.md`](src/Blossom.Reactive/README.md)
- Primitives: [`src/Blossom.Primitives/README.md`](src/Blossom.Primitives/README.md)

## Versioning

`version.json` (`"0.1"` → `0.1.{git height}`). Patch increments per commit. Edit `"version"` for a minor or major line (e.g. `"1.0"`). Tag `v*.*.*` for a GitHub Release and nuget.org push (`nuget.yml`).

## Built with

- [Silk.NET](https://github.com/dotnet/Silk.NET) 2.23.0 — window + input (GLFW)
- [SkiaSharp](https://github.com/mono/SkiaSharp) 4.153.0 — GPU drawing (`SkiaSharp.NativeAssets.Linux`)
- [ImageSharp](https://github.com/SixLabors/ImageSharp) 3.1.12 — raster images
- [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) 5.2.3 — SVG

## License

MIT. See [LICENSE](LICENSE).
