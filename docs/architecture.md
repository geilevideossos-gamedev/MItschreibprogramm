# Architektur

(wird in Schritt 1 gefüllt)

## Projektstruktur

- `Mitschreibprogramm/`: WPF-App, net8.0-windows. Ordner Models/, Views/, Services/, Rendering/.
- `Mitschreibprogramm.Tests/`: xUnit, nur Models/Services.
- `tools/`: pen-sim.ps1, screenshot.ps1 (Selbsttest).
- `docs/`, `build.sh`, `dist/` (Build-Ausgabe, nicht im Repo).

## NuGet-Pakete

- App: PDFsharp (Version wird in Schritt 6 über Context7 festgelegt). Sonst keine.
- Tests: xUnit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk.
