# Blossom
A rich .Net browser.<br>

Blossom is a framework / browser and app distribuitor for .Net C# applications with rich web like controls.

**Blossom.Reactive** is an optional SolidJS-inspired layer on top of the retained `VisualElement` tree: signals, memos, effects, and keyed list / conditional flow. See [`src/Blossom.Reactive/README.md`](src/Blossom.Reactive/README.md).

## Building and Running

Blossom is a **class library** (`src/Blossom`) plus a sample host (`src/Blossom.Demo`). Other C# apps reference the library and call `Browser.Initialize(yourApplication)`.

You can compile the demo on Linux using the build script:

```bash
./build.sh
```

By default, the script compiles Blossom in Release mode targeting Linux x64 with ReadyToRun (R2R) ahead-of-time compilation enabled to ensure rapid startup.

### Build Options

- **Framework-Dependent Build**: Run with `--framework-dependent` (or `-fd`). This requires the .NET 10 runtime to be installed on the system.
- **Self-Contained Build**: Run with the `--self-contained` (or `-s`) flag:
  ```bash
  ./build.sh --self-contained
  ```
  This packages the entire .NET runtime inside the build output directory `./dist/` so the application can run on any machine without .NET pre-installed.

### Launching the Application

Once built, launch Blossom using the root-level generated runner:
```bash
./Blossom
```

Or run the rendering benchmarks with:
```bash
./Blossom --benchmark
```

## License

Blossom is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
