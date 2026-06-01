# Blossom
A rich .Net browser.<br>

Blossom is a framework / browser and app distribuitor for .Net C# applications with rich web like controls.

## Building and Running

You can compile Blossom on Linux using the build script:

```bash
./build.sh
```

By default, the script compiles Blossom in Release mode targeting Linux x64 with ReadyToRun (R2R) ahead-of-time compilation enabled to ensure rapid startup.

### Build Options

- **Framework-Dependent Build (Default)**: Runs when you invoke `./build.sh`. This requires the .NET 8 runtime to be installed on the system.
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

## Licence


                     GNU GENERAL PUBLIC LICENSE
                       Version 3, 29 June 2007

    Blossom is a cross platform browser with rich web like grahpics.
    Copyright (C) 2023 Cosmin Crețu

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
