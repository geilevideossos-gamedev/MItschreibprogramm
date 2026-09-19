# Build

## Voraussetzungen

- Windows 10/11, .NET 8 SDK (`dotnet --version`, geprüft mit 8.0.300), Git Bash für build.sh.
- Internet beim ersten Restore (NuGet-Pakete) und beim ersten Publish (Runtime-Packs für win-x64).

## Entwicklung

```bash
dotnet build      # muss mit 0 Warning(s) und 0 Error(s) enden
dotnet test       # xUnit, Mitschreibprogramm.Tests
```

- Beide Projekte setzen `TreatWarningsAsErrors` und `MSBuildTreatWarningsAsErrors`. Eine Warnung bricht den Build ab.
- Start aus dem Build: `Mitschreibprogramm/bin/Debug/net8.0-windows/Mitschreibprogramm.exe`.

## Auslieferung

Alles in einem Schritt: `./build.sh` (Build, Tests, Publish, Prüfung, dass dist/ genau eine Datei enthält).

Der Publish-Befehl darin:

```bash
dotnet publish Mitschreibprogramm/Mitschreibprogramm.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

- Ergebnis: `dist/Mitschreibprogramm.exe`, ca. 141 MB, self-contained (kein .NET auf dem Zielrechner nötig), keine Installation.
- Immer das Projekt publishen, nicht die .sln: `-o` zusammen mit einer Solution lehnt das SDK ab (NETSDK1194).
- Kein Trimming. WPF unterstützt es nicht (das SDK bricht mit NETSDK1168 ab).
- `IncludeNativeLibrariesForSelfExtract` packt die fünf nativen WPF-DLLs mit in die exe. Beim ersten Start entpackt .NET sie nach %TEMP%/.net.
- Keine .pdb in dist/: `DebugType=embedded` steht in der csproj.
- `SatelliteResourceLanguages=de` lässt die Sprachressourcen aller anderen Sprachen weg.
- dist/, bin/, obj/, tmp/ stehen in .gitignore.

## Projektdateien

- `Mitschreibprogramm/app.manifest`: PerMonitorV2-DPI. Ohne Manifest startet WPF nur system-DPI-aware und wird auf einem zweiten Monitor mit anderer Skalierung unscharf.
- Pakete: PDFsharp 6.2.4 (App), xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 17.14.1 (Tests).

## Selbsttest gegen die exe

```bash
powershell -ExecutionPolicy Bypass -File tools/selftest.ps1 -Exe dist/Mitschreibprogramm.exe
```

Details in testing.md.
