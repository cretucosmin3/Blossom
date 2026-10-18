#!/bin/bash
# Builds every demo for a 64-bit Raspberry Pi (Pi 3/4/5 on 64-bit Raspberry Pi OS: linux-arm64)
# into dist/raspberry-pi/, ready to copy to the Pi as-is (self-contained: no .NET install needed).
#
#   ./build-pi.sh            Native AOT when run on the Pi itself, otherwise self-contained ReadyToRun
#   ./build-pi.sh --aot      Native AOT (on an arm64 host: the Pi; needs clang or gcc, and zlib1g-dev)
#   ./build-pi.sh --r2r      Self-contained ReadyToRun (from any host, no extra toolchain)
#
# Native AOT can't be cross-compiled from x64 without an arm64 sysroot, so x64 hosts build
# ReadyToRun: still self-contained (no .NET install on the Pi) and precompiled, so it starts fast.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

RID="linux-arm64"
OUT="$SCRIPT_DIR/dist/raspberry-pi"
DEMOS=(ApplicationDemo ReactiveLab Snake.MonoGame)

HOST_ARCH="$(uname -m)"
MODE=""
for arg in "$@"; do
  case "$arg" in
    --aot) MODE="aot" ;;
    --r2r) MODE="r2r" ;;
    *) echo "Unknown option: $arg (use --aot or --r2r)"; exit 1 ;;
  esac
done
if [ -z "$MODE" ]; then
  if [ "$HOST_ARCH" = "aarch64" ]; then MODE="aot"; else MODE="r2r"; fi
fi

PUBLISH_ARGS=(-c Release -r "$RID" --self-contained true -p:DebugType=none -p:DebugSymbols=false)
if [ "$MODE" = "aot" ]; then
  if [ "$HOST_ARCH" != "aarch64" ]; then
    echo "Native AOT for $RID has to be built on an arm64 machine (e.g. the Pi). Use --r2r here."
    exit 1
  fi
  PUBLISH_ARGS+=(-p:PublishAot=true -p:StripSymbols=true)
  # The AOT linker defaults to clang; fall back to gcc, which also works.
  if ! command -v clang >/dev/null; then PUBLISH_ARGS+=(-p:CppCompilerAndLinker=gcc); fi
else
  PUBLISH_ARGS+=(-p:PublishReadyToRun=true -p:TieredCompilation=true)
fi

echo "=== Building demos for $RID ($MODE) into $OUT ==="
rm -rf "$OUT"
mkdir -p "$OUT"

for demo in "${DEMOS[@]}"; do
  echo "=== $demo ==="
  dotnet publish "demos/$demo/$demo.csproj" "${PUBLISH_ARGS[@]}" -o "$OUT/$demo"

  # The repo's glfw/ folder (copied in by the demo projects) holds x64 Linux and Windows builds.
  # The Pi uses Silk's arm64 GLFW 3.4 (libglfw.so.3), which publish already placed next to the app.
  rm -f "$OUT/$demo"/libglfw.so.3.3 "$OUT/$demo"/glfw3-*.dll

  # Launcher: start from the app folder so assets/ resolves.
  cat > "$OUT/run-$demo.sh" << EOF
#!/bin/bash
# Blossom picks OpenGL 3.2, GLES 3.0 or GLES 2.0, whichever the GPU really supports (Pi 3: GLES 2.0).
# BLOSSOM_GL=gl|gles3|gles2 forces one. Don't set MESA_GL_VERSION_OVERRIDE: it makes the driver
# claim features it lacks, which shows up as broken text and missing redraws.
cd"\$(dirname "\$(readlink -f "\$0")")/$demo"
exec "./$demo" "\$@"
EOF
  chmod +x "$OUT/run-$demo.sh" "$OUT/$demo/$demo"
done

cat > "$OUT/README.txt" << 'EOF'
Blossom demos for Raspberry Pi (64-bit Raspberry Pi OS, linux-arm64)

Copy this whole folder to the Pi, then run one of:
  ./run-ApplicationDemo.sh      (Blossom Gallery; --benchmark for the benchmark views)
  ./run-ReactiveLab.sh
  ./run-Snake.MonoGame.sh

Each app is self-contained: no .NET install needed.

Needs a desktop session (Wayland or X11) and the Pi's GPU driver, which Raspberry Pi OS ships.
The apps use the best OpenGL the GPU has: OpenGL 3.2, else GLES 3.0 (Pi 4/5), else GLES 2.0 (Pi 3).
blossom.log next to each app says which one ("OpenGL backend: ...").
EOF

echo
echo "=== Checking every binary targets ARM64 ==="
bad=0
while IFS= read -r f; do
  if file -b "$f" | grep -q "ELF" && ! file -b "$f" | grep -q "ARM aarch64"; then
    echo "  not arm64: ${f#$OUT/} ($(file -b "$f" | cut -d, -f1-2))"
    bad=1
  fi
done < <(find "$OUT" -type f \( -name "*.so*" -o -perm -u+x \) ! -name "*.sh")
if [ "$bad" = "1" ]; then
  echo "Some native files are not arm64; the app may fail to start on the Pi."
  exit 1
fi
echo "  all native binaries are ARM aarch64"

echo
echo "=== Done: $OUT ($(du -sh "$OUT" | cut -f1)) ==="
for demo in "${DEMOS[@]}"; do echo "  $demo: $(du -sh "$OUT/$demo" | cut -f1)"; done
echo "Copy it with, e.g.:  scp -r \"$OUT\" pi@raspberrypi.local:~/"
