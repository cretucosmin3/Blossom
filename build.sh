#!/bin/bash
set -e

# Define directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

SELF_CONTAINED="false"
TARGET_OS=""

# Parse command line arguments
for arg in "$@"; do
  if [ "$arg" = "--self-contained" ] || [ "$arg" = "-s" ]; then
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

echo "=== Cleaning previous distribution ==="
rm -rf ./dist
rm -f ./Blossom
rm -f ./Blossom.exe
rm -f ./Blossom.bat

if [ "$TARGET_OS" = "windows" ]; then
  echo "=== Compiling Blossom for Windows (Release, x64) ==="
  dotnet publish -c Release -r win-x64 --self-contained "$SELF_CONTAINED" -p:PublishReadyToRun=true -o ./dist

  echo "=== Configuring native libraries ==="
  # Remove the NuGet-provided glfw3.dll if present
  rm -f dist/glfw3.dll
  # Copy the custom workspace glfw3-x64.dll which is known to work
  cp glfw/glfw3-x64.dll dist/glfw3.dll

  echo "=== Creating root launcher scripts ==="
  # Create bash launcher script (for Git Bash/bash on Windows users)
  cat << 'EOF' > ./Blossom
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
exec ./dist/Blossom.exe "$@"
EOF
  chmod +x ./Blossom

  # Create batch file launcher (for Command Prompt / PowerShell users)
  cat << 'EOF' > ./Blossom.bat
@echo off
"%~dp0dist\Blossom.exe" %*
EOF

  echo "=== Compilation Complete! ==="
  echo "You can run the application now with: ./Blossom (in Git Bash/bash) or Blossom.bat (in CMD/PowerShell)"
  echo "Or run benchmarks with: ./Blossom --benchmark"

else
  echo "=== Compiling Blossom for Linux (Release, x64) ==="
  dotnet publish -c Release -r linux-x64 --self-contained "$SELF_CONTAINED" -p:PublishReadyToRun=true -o ./dist

  echo "=== Configuring native libraries ==="
  # Remove the NuGet-provided libglfw.so.3 which has context issues in some environments
  rm -f dist/libglfw.so.3
  # Copy the custom workspace libglfw.so.3.3 which is known to work as libglfw.so.3
  cp glfw/libglfw.so.3.3 dist/libglfw.so.3

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
fi
