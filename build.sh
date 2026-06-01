#!/bin/bash
set -e

# Define directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

SELF_CONTAINED="false"
for arg in "$@"; do
  if [ "$arg" = "--self-contained" ] || [ "$arg" = "-s" ]; then
    SELF_CONTAINED="true"
  fi
done

echo "=== Cleaning previous distribution ==="
rm -rf ./dist
rm -f ./Blossom

echo "=== Compiling Blossom for Linux (Release, x64) ==="
dotnet publish -c Release -r linux-x64 --self-contained "$SELF_CONTAINED" -p:PublishReadyToRun=true -o ./dist

echo "=== Configuring native libraries ==="
# Remove the NuGet-provided libglfw.so.3 which has context issues in some environments
rm -f dist/libglfw.so.3
# Copy the custom workspace libglfw.so.3.3 which is known to work
cp glfw/libglfw.so.3.3 dist/

echo "=== Creating root launcher script ==="
cat << 'EOF' > ./Blossom
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
exec ./dist/Blossom "$@"
EOF

chmod +x ./Blossom

echo "=== Compilation Complete! ==="
echo "You can run the application now with: ./Blossom"
echo "Or run benchmarks with: ./Blossom --benchmark"
