#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# Blossom - Linux Native Production Build Script
# ==============================================================================
# Compiles a high-performance, self-contained, native ReadyToRun Linux build.
# Output destination: ./production
# ==============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$SCRIPT_DIR"
OUTPUT_DIR="$ROOT_DIR/production"

cd "$ROOT_DIR"

# Detect architecture (default to linux-x64)
ARCH="$(uname -m)"
case "$ARCH" in
  x86_64)
    RID="linux-x64"
    ;;
  aarch64|arm64)
    RID="linux-arm64"
    ;;
  *)
    RID="linux-x64"
    ;;
esac

echo "=================================================================="
echo " Starting Native Linux Production Build for Blossom ($RID)"
echo "=================================================================="

# Check for dotnet SDK
if ! command -v dotnet &> /dev/null; then
  echo "Error: 'dotnet' SDK is not installed or not in PATH."
  exit 1
fi

DOTNET_VERSION="$(dotnet --version)"
echo "Using .NET SDK version: $DOTNET_VERSION"

# Clean previous production build
echo "--> Cleaning previous production build at: $OUTPUT_DIR"
rm -rf "$OUTPUT_DIR"
rm -f "$ROOT_DIR/Blossom"

# Perform native ReadyToRun release build
echo "--> Compiling with dotnet publish (Release, ReadyToRun, Self-Contained)..."
dotnet publish Blossom.csproj \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishReadyToRun=true \
  -p:PublishReadyToRunShowWarnings=true \
  -p:TieredCompilation=true \
  -o "$OUTPUT_DIR"

# Configure native libraries (ensure tested GLFW library is used)
echo "--> Configuring native libraries and dependencies..."
if [ -f "$ROOT_DIR/glfw/libglfw.so.3.3" ]; then
  rm -f "$OUTPUT_DIR/libglfw.so.3"
  cp "$ROOT_DIR/glfw/libglfw.so.3.3" "$OUTPUT_DIR/libglfw.so.3"
  chmod +x "$OUTPUT_DIR/libglfw.so.3"
fi

# Ensure assets directory is present in production
if [ -d "$ROOT_DIR/assets" ]; then
  echo "--> Copying runtime assets..."
  mkdir -p "$OUTPUT_DIR/assets"
  cp -r "$ROOT_DIR/assets/"* "$OUTPUT_DIR/assets/"
fi

# Ensure binary is executable
if [ -f "$OUTPUT_DIR/Blossom" ]; then
  chmod +x "$OUTPUT_DIR/Blossom"
fi

# Create convenience launcher in repo root
cat << 'EOF' > "$ROOT_DIR/Blossom"
#!/usr/bin/env bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/production"
exec ./Blossom "$@"
EOF
chmod +x "$ROOT_DIR/Blossom"

TOTAL_SIZE="$(du -sh "$OUTPUT_DIR" | cut -f1)"

echo "=================================================================="
echo " Build Succeeded!"
echo " Output directory: $OUTPUT_DIR ($TOTAL_SIZE)"
echo " Executable:       $OUTPUT_DIR/Blossom"
echo " Root Launcher:    $ROOT_DIR/Blossom"
echo " Log Destination:  $OUTPUT_DIR/blossom.log"
echo "=================================================================="
echo " Run production build directly with:"
echo "   ./production/Blossom"
echo " Or from repo root:"
echo "   ./Blossom"
echo "=================================================================="
