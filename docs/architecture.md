# Architektur

## Projektstruktur

- `Mitschreibprogramm/`: WPF-App, net8.0-windows. Ordner Models/, Views/, Services/, Rendering/.
- `Mitschreibprogramm.Tests/`: xUnit, nur Models/Services.
- `tools/`: Selbsttest. pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1 mit den Checkpoints in `tools/selftest/`, gemeinsamer Win32-Code als partielle Klasse in MspNative.Window.cs, MspNative.Pen.cs, MspNative.Input.cs.
- `docs/`, `build.sh`, `dist/` (Build-Ausgabe, nicht im Repo).

## Klassen

### Models

- `AppConstants`: alle Maße und Grenzen (A4 in Pixeln bei 96 DPI, Linienabstand 8 mm, Gitterabstand 5 mm, Strichbreiten, Zoomgrenzen und -stufen).
- `RuleLines`: Positionen der Hintergrundlinien je Seitenstil. `Rows` (Liniert 8 mm, Kariert 5 mm, pro A4-Höhe neu gestartet) und `Columns` (nur Kariert). Wird vom Bildschirm-Hintergrund und vom PDF-Export benutzt.
- Enums `PenColor`, `PageStyle`, `LineColor`, `PageMode`.
- `AppSettings`: alles, was settings.json speichert, mit Standardwerten.
- `NoteDocument` / `NotePage` / `NoteStroke`: reine Datenklassen ohne WPF. Punkte als `[x, y, druck]`. Sie sind zugleich das Dateiformat.
- `NotebookIndex` / `NotebookEntry`: Inhalt von notes/index.json (zuletzt offenes Heft, je Heft id, Name, Änderungszeit, Scrollposition).

### Rendering

- `Palette`: Stift-, Seiten- und Hilfslinienfarben für hell und dunkel, dazu die Umrechnung logische Farbe zu angezeigter Farbe und zurück.
- `Theme`: UI-Farben beider Themen, tauscht die Pinsel in den Application-Resources und färbt die Titelleiste (DWM).
- `PageBackground`: FrameworkElement, zeichnet Seite und Linien in OnRender (GuidelineSet für scharfe 1-px-Linien). Kein Strich, nicht radierbar.

### Views

- `MainWindow`: Toolbar (Border + WrapPanel), Statusleiste, ScrollViewer. Verdrahtet Controls mit `DocumentView`, hält die Shortcut-Tabelle (`Dictionary<(ModifierKeys, Key), Action>`) und die `AppSettings` als einzigen Zustand für Werkzeug und Seite. Lädt die Settings im Konstruktor, speichert sie samt Fensterlage in OnClosing.
- `DocumentView` (StackPanel): hält die `PageView`-Liste, Seitenmodus, das gemeinsame `DrawingAttributes`-Objekt, Radierer-/Pan-Zustand, Undo-Verlauf und den Zoom (LayoutTransform). `Load(NoteDocument)` / `ToDocument()` wandeln zwischen Ansicht und Datenklassen, `SetMode` konvertiert, `AddPage` hängt an, Auto-Seite und Endlos-Wachstum hängen an StrokeCollected.
- `PageView` (Grid): eine Seite = `PageBackground` + transparenter `InkCanvas`, auf Seitengröße geclippt. `GrowToFit` vergrößert die Endlos-Fläche.
- `FileSession`: aktueller Dateipfad, Dirty-Flag, Neu / Öffnen / Speichern / Speichern unter mit den Windows-Dateidialogen, Nachfrage bei ungespeicherten Änderungen. Meldet `StateChanged` (Titel) und `DocumentLoaded` (Toolbar abgleichen).
- `UnsavedChangesDialog` / `UnsavedChoice`: Dialog Speichern / Verwerfen / Abbrechen.
- `ExportDialog`: Checkbox "Hintergrundlinien mit exportieren", danach fragt `FileSession.ExportPdf` den Zielpfad ab.
- `SideButtonWatcher`: verfolgt die Seitentaste des Stifts (Barrel) über die Preview-Stylus-Events.
- `ZoomPanController`: Mausrad, Ctrl/Shift+Mausrad, mittlere Maustaste, Leertaste+Ziehen, Zoom um den Zeiger.
- `StrokeLogger`: nur mit MSP_DEBUG_LOG erzeugt, schreibt pro Strich eine Logzeile.

### Services

- `UndoHistory` / `UndoStep`: zwei Stapel aus Undo-/Redo-Aktionspaaren.
- `PageModeConverter`: Seiten zu Endlos und zurück auf `NoteDocument`.
- `StrokeMapper`: WPF-`Stroke` zu `NoteStroke` und zurück (Koordinaten auf 2, Druck auf 3 Nachkommastellen gerundet).
- `MspFileService`: .msp lesen und schreiben (System.Text.Json, Format in file-format.md). `JsonFormat`: gemeinsame JSON-Optionen (camelCase, Enums als Text). `PageStyleJsonConverter`: liest den früheren Stilwert `dashed` als `Squared`. `AtomicFile`: schreibt `.tmp` und verschiebt sie über das Ziel.
- `NotebookLibrary`: der Ordner notes/ mit einer .msp je Heft und index.json (file-format.md). Lädt und gleicht den Index mit dem Ordner ab, legt Hefte an (auch per Import), benennt um, löscht, kennt das offene Heft (`LastOpen`) und dessen Änderungsflag (`HasChanges`), `Save` schreibt die Datei nur nach einer Änderung und die Scrollposition immer. Bekommt einen `TimeProvider` für die Zeitstempel, damit die Tests ihn stellen können.
- `AppPaths`: Ort von settings.json und notes/ (%AppData%, mit MSP_DEBUG_LOG neben dem Log).
- `SettingsService`: settings.json laden (mit Standardwerten bei Fehler) und speichern.
- `PdfExporter`: `NoteDocument` zu PDF (PDFsharp). `PressureSegments` liefert die Linienstücke eines Druckstrichs.
- `DebugLog`: hängt Zeilen an die Datei aus MSP_DEBUG_LOG an, kulturinvariant.

## Datenfluss

- Toolbar oder Shortcut setzt den Zustand an `DocumentView`, die ihn auf alle Seiten anwendet.
- `DocumentView.Changed` meldet jede Dokumentänderung (Strich, Radieren, Undo/Redo, Seite, Stil, Modus), `FileSession` setzt damit das Dirty-Flag.
- InkCanvas sammelt Striche selbst (Maus und Stylus). `DocumentView` hängt sich an StrokeCollected / StrokeErasing für Undo.
- Editiermodus je Seite: Pan aktiv = None, Radierer oder Seitentaste = EraseByStroke, sonst Ink. EditingModeInverted = EraseByStroke.

## NuGet-Pakete

- App: PDFsharp 6.2.4 (Core-Build, MIT). Sonst keine.
- Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk.
