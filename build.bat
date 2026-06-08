@echo off
cd /d "%~dp0"
setlocal enabledelayedexpansion

set "SELF_CONTAINED=false"
for %%a in (%*) do (
    if "%%a"=="--self-contained" set "SELF_CONTAINED=true"
    if "%%a"=="-s" set "SELF_CONTAINED=true"
)

echo === Cleaning previous distribution ===
if exist dist rmdir /s /q dist
if exist Blossom del Blossom
if exist Blossom.exe del Blossom.exe
if exist Blossom.bat del Blossom.bat

echo === Compiling Blossom for Windows (Release, x64) ===
dotnet publish -c Release -r win-x64 --self-contained %SELF_CONTAINED% -p:PublishReadyToRun=true -o .\dist
if %errorlevel% neq 0 (
    echo Compilation failed!
    goto end
)

echo === Configuring native libraries ===
if exist dist\glfw3.dll del dist\glfw3.dll
copy glfw\glfw3-x64.dll dist\glfw3.dll > nul

echo === Creating root launcher script ===
echo @echo off > Blossom.bat
echo "%%~dp0dist\Blossom.exe" %%* >> Blossom.bat

echo === Compilation Complete! ===
echo You can run the application now with: Blossom.bat
echo Or run benchmarks with: Blossom.bat --benchmark

:end
rem Pause if the script was launched by double-clicking in Explorer
echo %cmdcmdline% | findstr /i /c:"cmd /c" >nul
if %errorlevel% equ 0 (
    echo.
    echo Press any key to exit...
    pause > nul
)
