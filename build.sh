#!/bin/bash
set -e

# Define directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

SELF_CONTAINED="true"
TARGET_OS=""

# Parse command line arguments
for arg in "$@"; do
  if [ "$arg" = "--framework-dependent" ] || [ "$arg" = "-fd" ]; then
    SELF_CONTAINED="false"
  elif [ "$arg" = "--self-contained" ] || [ "$arg" = "-s" ]; then
    SELF_CONTAINED="true"
  elif [ "$arg" = "--windows" ] || [ "$arg" = "--win" ] || [ "$arg" = "-w" ]; then
    TARGET_OS="windows"
  elif [ "$arg" = "--linux" ] || [ "$arg" = "-l" ]; then
    TARGET_OS="linux"
  fi
done

# If no target OS is specified, auto-detect the host OS
if [ -z "$TARGET_OS" ]; then
  if [ -n "$COMSPEC" ] || [ -n "$SystemRoot" ] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]] || uname -s | grep -iqE "(MINGW|MSYS|CYGWIN|Windows)"; then
    TARGET_OS="windows"
  else
    TARGET_OS="linux"
  fi
fi

echo "=== Cleaning previous production build ==="
rm -rf ./production
rm -rf ./dist
rm -f ./Blossom
rm -f ./Blossom.exe
rm -f ./Blossom.bat

if [ "$TARGET_OS" = "windows" ]; then
  echo "=== Compiling Blossom for Windows (Release, x64) ==="
  dotnet publish Blossom.csproj -c Release -r win-x64 --self-contained "$SELF_CONTAINED" -p:PublishReadyToRun=true -o ./production

  echo "=== Configuring native libraries ==="
  rm -f production/glfw3.dll
  cp glfw/glfw3-x64.dll production/glfw3.dll

  echo "=== Creating root launcher scripts ==="
  cat << 'EOF' > ./Blossom
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/production"
exec ./Blossom.exe "$@"
EOF
  chmod +x ./Blossom

  cat << 'EOF' > ./Blossom.bat
@echo off
cd /d "%~dp0production"
"%~dp0production\Blossom.exe" %*
EOF

  echo "=== Compilation Complete! ==="
  echo "You can run the application now with: ./Blossom (in Git Bash/bash) or Blossom.bat (in CMD/PowerShell)"
  echo "Or run benchmarks with: ./Blossom --benchmark"

else
  echo "=== Compiling Blossom for Linux (Release, x64) ==="
  dotnet publish Blossom.csproj -c Release -r linux-x64 --self-contained "$SELF_CONTAINED" -p:PublishReadyToRun=true -p:TieredCompilation=true -o ./production

  echo "=== Configuring native libraries ==="
  rm -f production/libglfw.so.3
  cp glfw/libglfw.so.3.3 production/libglfw.so.3
  chmod +x production/libglfw.so.3

  if [ -d "./assets" ]; then
    mkdir -p production/assets
    cp -r ./assets/* production/assets/
  fi

  chmod +x production/Blossom

  echo "=== Creating root launcher script ==="
  cat << 'EOF' > ./Blossom
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/production"
exec ./Blossom "$@"
EOF
  chmod +x ./Blossom

  echo "=== Compilation Complete! ==="
  echo "Production build located in: ./production"
  echo "You can run the application with: ./production/Blossom (or ./Blossom)"
  echo "Logs are written to: ./production/blossom.log"
fi
