# Architektur

## Projektstruktur

- `Mitschreibprogramm/`: WPF-App, net8.0-windows. Ordner Models/, Views/, Services/, Rendering/.
- `Mitschreibprogramm.Tests/`: xUnit, nur Models/Services.
- `tools/`: Selbsttest (pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1, MspNative.cs).
- `docs/`, `build.sh`, `dist/` (Build-Ausgabe, nicht im Repo).

## Klassen

### Models

- `AppConstants`: alle Maße und Grenzen (A4 in Pixeln bei 96 DPI, Linienabstand 8 mm, Strichbreiten, Zoomgrenzen und -stufen).
- Enums `PenColor`, `PageStyle`, `LineColor`, `PageMode`.
- `AppSettings`: alles, was settings.json speichert, mit Standardwerten.
- `NoteDocument` / `NotePage` / `NoteStroke`: reine Datenklassen ohne WPF. Punkte als `[x, y, druck]`. Sie sind zugleich das Dateiformat.

### Rendering

- `Palette`: einzige Stelle mit Farb-Hexwerten (Stiftfarben, Seite, Hilfslinien mit Alpha).
- `RuleLines`: Y-Positionen der Hilfslinien, pro A4-Höhe neu gestartet. Wird vom Bildschirm-Hintergrund und später vom PDF-Export benutzt.
- `PageBackground`: FrameworkElement, zeichnet Seite und Linien in OnRender (GuidelineSet für scharfe 1-px-Linien). Kein Strich, nicht radierbar.

### Views

- `MainWindow`: Toolbar (Border + WrapPanel), Statusleiste, ScrollViewer. Verdrahtet Controls mit `DocumentView`, hält die Shortcut-Tabelle (`Dictionary<(ModifierKeys, Key), Action>`).
- `DocumentView` (StackPanel): hält die `PageView`-Liste, Seitenmodus, das gemeinsame `DrawingAttributes`-Objekt, Radierer-/Pan-Zustand, Undo-Verlauf und den Zoom (LayoutTransform). `Load(NoteDocument)` / `ToDocument()` wandeln zwischen Ansicht und Datenklassen, `SetMode` konvertiert, `AddPage` hängt an, Auto-Seite und Endlos-Wachstum hängen an StrokeCollected.
- `PageView` (Grid): eine Seite = `PageBackground` + transparenter `InkCanvas`, auf Seitengröße geclippt. `GrowToFit` vergrößert die Endlos-Fläche.
- `FileSession`: aktueller Dateipfad, Dirty-Flag, Neu / Öffnen / Speichern / Speichern unter mit den Windows-Dateidialogen, Nachfrage bei ungespeicherten Änderungen. Meldet `StateChanged` (Titel) und `DocumentLoaded` (Toolbar abgleichen).
- `UnsavedChangesDialog` / `UnsavedChoice`: Dialog Speichern / Verwerfen / Abbrechen.
- `SideButtonWatcher`: verfolgt die Seitentaste des Stifts (Barrel) über die Preview-Stylus-Events.
- `ZoomPanController`: Mausrad, Ctrl/Shift+Mausrad, mittlere Maustaste, Leertaste+Ziehen, Zoom um den Zeiger.
- `StrokeLogger`: nur mit MSP_DEBUG_LOG erzeugt, schreibt pro Strich eine Logzeile.

### Services

- `UndoHistory` / `UndoStep`: zwei Stapel aus Undo-/Redo-Aktionspaaren.
- `PageModeConverter`: Seiten zu Endlos und zurück auf `NoteDocument`.
- `StrokeMapper`: WPF-`Stroke` zu `NoteStroke` und zurück (Koordinaten auf 2, Druck auf 3 Nachkommastellen gerundet).
- `MspFileService`: .msp lesen und schreiben (System.Text.Json, Format in file-format.md). `JsonFormat`: gemeinsame JSON-Optionen (camelCase, Enums als Text).
- `SettingsService`: settings.json laden (mit Standardwerten bei Fehler) und speichern.
- `DebugLog`: hängt Zeilen an die Datei aus MSP_DEBUG_LOG an, kulturinvariant.

## Datenfluss

- Toolbar oder Shortcut setzt den Zustand an `DocumentView`, die ihn auf alle Seiten anwendet.
- `DocumentView.Changed` meldet jede Dokumentänderung (Strich, Radieren, Undo/Redo, Seite, Stil, Modus), `FileSession` setzt damit das Dirty-Flag.
- InkCanvas sammelt Striche selbst (Maus und Stylus). `DocumentView` hängt sich an StrokeCollected / StrokeErasing für Undo.
- Editiermodus je Seite: Pan aktiv = None, Radierer oder Seitentaste = EraseByStroke, sonst Ink. EditingModeInverted = EraseByStroke.

## NuGet-Pakete

- App: PDFsharp 6.2.4 (Core-Build, MIT), kommt mit Schritt 6. Sonst keine.
- Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk.
