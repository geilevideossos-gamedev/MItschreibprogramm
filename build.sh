#!/usr/bin/env bash
# Builds, tests and publishes dist/Mitschreibprogramm.exe (Git Bash on Windows, needs the .NET 8 SDK).
set -euo pipefail
cd "$(dirname "$0")"

export DOTNET_CLI_UI_LANGUAGE=en
export DOTNET_NOLOGO=1

dotnet build Mitschreibprogramm.sln -c Debug
dotnet test Mitschreibprogramm.sln -c Debug --no-build

# Publish the project, not the solution: -o together with a solution is rejected by the SDK.
rm -rf dist
dotnet publish Mitschreibprogramm/Mitschreibprogramm.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist

if [ "$(ls -A dist | wc -l)" -ne 1 ] || [ ! -f dist/Mitschreibprogramm.exe ]; then
  echo "FEHLER: dist/ muss genau Mitschreibprogramm.exe enthalten:" >&2
  ls -la dist >&2
  exit 1
fi
echo "OK: dist/Mitschreibprogramm.exe ($(du -h dist/Mitschreibprogramm.exe | cut -f1))"
