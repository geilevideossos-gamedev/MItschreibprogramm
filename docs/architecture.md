# Architektur

## Projektstruktur

- `Mitschreibprogramm/`: WPF-App, net8.0-windows. Ordner Models/, Views/, Services/, Rendering/.
- `Mitschreibprogramm.Tests/`: xUnit, nur Models/Services.
- `tools/`: Selbsttest. pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1 mit den Checkpoints in `tools/selftest/`, gemeinsamer Win32-Code als partielle Klasse in MspNative.Window.cs, MspNative.Pen.cs, MspNative.Input.cs.
- `docs/`, `build.sh`, `dist/` (Build-Ausgabe, nicht im Repo).

## Klassen

### Models

- `AppConstants`: alle Maße und Grenzen (A4 in Pixeln bei 96 DPI, Linienabstand 8 mm, Gitterabstand 5 mm, Strichbreiten, Zoomgrenzen und -stufen, Mindestgröße, Haltezeit und Ruhetoleranz der Formerkennung).
- `RuleLines`: Positionen der Hintergrundlinien je Seitenstil. `Rows` (Liniert 8 mm, Kariert 5 mm, pro A4-Höhe neu gestartet) und `Columns` (nur Kariert). Wird vom Bildschirm-Hintergrund und vom PDF-Export benutzt.
- Enums `PenColor`, `PageStyle`, `LineColor`, `PageMode`.
- `AppSettings`: alles, was settings.json speichert, mit Standardwerten.
- `NoteDocument` / `NotePage` / `NoteStroke`: reine Datenklassen ohne WPF. Punkte als `[x, y, druck]`. Sie sind zugleich das Dateiformat. `NoteStroke.FitToCurve` ist bei erkannten Formen false.
- `NoteImage`: ein Bild einer Seite (x, y, Breite, Höhe in Seitenpixeln, PNG als base64). `NotePage.Images` liegt unter `NotePage.Strokes`.
- `ShapeKind` (Linie, Kreis, Ellipse, Rechteck, Dreieck) und `RecognizedShape` (Art plus Umriss als Punktliste): Ergebnis der Formerkennung.
- `NotebookIndex` / `NotebookEntry`: Inhalt von notes/index.json (zuletzt offenes Heft, je Heft id, Name, Änderungszeit, Scrollposition).

### Rendering

- `Palette`: Stift-, Seiten- und Hilfslinienfarben für hell und dunkel, dazu die Umrechnung logische Farbe zu angezeigter Farbe und zurück.
- `Theme`: UI-Farben beider Themen, tauscht die Pinsel in den Application-Resources und färbt die Titelleiste (DWM).
- `PageBackground`: FrameworkElement, zeichnet Seite und Linien in OnRender (GuidelineSet für scharfe 1-px-Linien). Kein Strich, nicht radierbar.

### Views

- `MainWindow`: Toolbar (Border + WrapPanel), links das `NotebookPanel`, Statusleiste, ScrollViewer. Verdrahtet Controls mit `DocumentView` und `NotebookSession`, hält die Shortcut-Tabelle (`Dictionary<(ModifierKeys, Key), Action>`) und die `AppSettings` als einzigen Zustand für Werkzeug, Seite und Seitenleiste. Lädt die Settings im Konstruktor, speichert sie in OnClosing, nachdem die Session das Heft gesichert hat. `WindowPlacement`: Fensterlage merken und wiederherstellen. `ScrollPosition`: Dokumentpunkt oben links im Fenster erfassen und wiederherstellen (beim Start erst nach dem ersten Layout, 0/0 = ganz oben).
- `DocumentView` (StackPanel): hält die `PageView`-Liste, Seitenmodus, das gemeinsame `DrawingAttributes`-Objekt, Radierer-/Pan-Zustand, Undo-Verlauf und den Zoom (LayoutTransform). `Load(NoteDocument)` / `ToDocument()` wandeln zwischen Ansicht und Datenklassen, `SetMode` konvertiert, `AddPage` hängt an, Auto-Seite und Endlos-Wachstum hängen an StrokeCollected. `Record(UndoStep)` nimmt Änderungen auf, die nicht aus den InkCanvas-Ereignissen kommen (Formen, Bilder, Auswahl).
- `PageView` (Grid): eine Seite = `PageBackground` + transparenter `InkCanvas`, auf Seitengröße geclippt. Lädt ihren Inhalt (Striche und Bilder) aus einer `NotePage`, gibt ihn mit `ToModel` zurück und färbt ihn mit `ApplyTheme` ein. `GrowToFit` vergrößert die Endlos-Fläche.
- `PageImage`: ein Bild ist ein `Image`-Kind des InkCanvas der Seite (InkCanvas.Left/Top, Width/Height, das PNG im Tag). Erzeugen aus `NoteImage`, zurück in `NoteImage`, Rahmen.
- `ImageInserter`: Strg+V. Liest die Zwischenablage (Format „PNG“, sonst das Bitmap ohne Alphakanal) und speichert immer ein 8-Bit-PNG, legt das Bild auf die Seite in der Mitte des sichtbaren Bereichs (höchstens 80 % der Seitenbreite und -höhe, Seitenverhältnis bleibt, innerhalb der Seite) und nimmt es über `DocumentView.Record` in den Undo-Verlauf.
- `ShapeAssist`: verfolgt Aufsetzen, Bewegung und Abheben (Stift und echte Maus, Positionen in Fenster-Einheiten) mit einem `HoldDetector`. Nach StrokeCollected ersetzt `Apply` den Strich durch die erkannte Form, wenn „Formen“ an ist oder der Stift am Ende stillstand, und legt dafür einen eigenen Undo-Schritt an.
- `NotebookSession`: hält das offene Heft der `NotebookLibrary` in der `DocumentView`. Start (zuletzt offenes Heft, dann die anderen, neueste zuerst, sonst ein neues „Unbenannt“), Wechsel mit Autosave ohne Dialog, Neu, Umbenennen (`RenameDialog`), Löschen (`ConfirmDialog`, beim offenen Heft wird das nächste geöffnet), Autosave-Timer (`AppConstants.AutosaveIntervalSeconds`, setzt aus, solange ein Strich läuft), Scrollposition merken (Dokumentpunkt oben links, `TranslatePoint`) und nach dem Laden wiederherstellen (beim Start erst nach dem ersten Layout). Schlägt das Speichern fehl, gehen Wechsel, Neu, Import und Beenden nur nach Rückfrage weiter, solange Änderungen offen sind; war nur der Index betroffen, geht es ohne Nachfrage weiter. Meldet `Changed` (Liste, Titel) und `DocumentLoaded` (Toolbar abgleichen).
- `NotebookTransfer`: Import einer externen .msp als Kopie in die Bibliothek, Export eines Hefts als .msp oder PDF über die Windows-Dateidialoge, vorgeschlagener Dateiname = Heftname (ungültige Zeichen werden ersetzt). Das offene Heft kommt aus der Ansicht, andere von der Platte.
- `NotebookPanel` (UserControl): „+ Neues Heft“, ListBox der Hefte (Name, Änderungszeit), das offene Heft ist markiert, Klick öffnet, Doppelklick benennt um, Kontextmenü mit Umbenennen, Löschen, Als PDF exportieren, Als MSP exportieren. Eigene Vorlagen für ListBoxItem und Menü, damit der Dark Mode stimmt. Liste und Einträge sind nicht fokussierbar, Klicks werden im Preview-Handler behandelt (ein Klick öffnet, zwei benennen um, Rechtsklick nur Menü): das Fenster sieht weiter jede Taste. UI Automation öffnet über die Auswahl (`SelectionChanged`). Meldet nur Wünsche (`OpenRequested` usw.), die Session entscheidet.
- `RelativeDateConverter`: bindet `RelativeDate` an die Zeile unter dem Heftnamen.
- `RenameDialog`, `ConfirmDialog` (Löschen und Beenden trotz Schreibfehler), `ErrorMessage` (Warn-MessageBox und `Try` für Datei-Operationen).
- `ExportDialog`: Checkbox "Hintergrundlinien mit exportieren", danach fragt `NotebookTransfer.ExportPdf` den Zielpfad ab (Vorschlag: Heftname.pdf).
- `SideButtonWatcher`: verfolgt die Seitentaste des Stifts (Barrel) über die Preview-Stylus-Events.
- `EditingModes`: setzt den InkCanvas-Modus aller Seiten. Select (Lasso), solange das Werkzeug „Auswahl“ an ist, die Seitentaste (Barrel) gehalten wird oder etwas ausgewählt ist; sonst Radierer oder Stift, beim Verschieben der Ansicht None. `EditingModeInverted` ist immer EraseByStroke.
- `SelectionEditor`: eine Auswahl über alle Seiten (eine neue Auswahl hebt die auf dem anderen Blatt auf, ein Klick auf ein anderes Blatt beendet sie nur). Korrigiert in SelectionMoving/-Resizing das Rechteck (`SelectionBounds`), macht vorher und nachher eine `SelectionSnapshot` und legt daraus den Undo-Schritt an, löscht mit Entf (Striche und Bilder an ihren alten Platz zurück beim Undo), hebt mit Esc auf.
- `SelectionSnapshot`: Punkte der gewählten Striche und Rahmen der gewählten Bilder, zum Wiederherstellen.
- `ImageDrag`: zieht ein allein gewähltes Bild an seiner Mitte (Stift und Maus), weil InkCanvas das dann nicht tut.
- `ZoomPanController`: Mausrad, Ctrl/Shift+Mausrad, mittlere Maustaste, Leertaste+Ziehen, Zoom um den Zeiger.
- `StrokeLogger`: nur mit MSP_DEBUG_LOG erzeugt, schreibt pro Strich eine Logzeile.

### Services

- `UndoHistory` / `UndoStep`: zwei Stapel aus Undo-/Redo-Aktionspaaren.
- `PageModeConverter`: Seiten zu Endlos und zurück auf `NoteDocument`, Bilder gehören zur Kachel ihrer linken oberen Ecke.
- `StrokeMapper`: WPF-`Stroke` zu `NoteStroke` und zurück (Koordinaten auf 2, Druck auf 3 Nachkommastellen gerundet).
- `MspFileService`: .msp lesen und schreiben (System.Text.Json, Format in file-format.md), prüft Bilder auf base64 und PNG-Signatur, hebt ältere Versionen beim Laden auf die aktuelle. `JsonFormat`: gemeinsame JSON-Optionen (camelCase, Enums als Text). `PageStyleJsonConverter`: liest den früheren Stilwert `dashed` als `Squared`. `AtomicFile`: schreibt `.tmp` und verschiebt sie über das Ziel.
- `NotebookLibrary`: der Ordner notes/ mit einer .msp je Heft und index.json (file-format.md). Lädt und gleicht den Index mit dem Ordner ab, legt Hefte an (auch per Import), benennt um, löscht, kennt das offene Heft (`LastOpen`) und dessen Änderungsflag (`HasChanges`), `Save` schreibt die Datei nur nach einer Änderung und die Scrollposition immer. Bekommt einen `TimeProvider` für die Zeitstempel, damit die Tests ihn stellen können.
- `AppPaths`: Ort von settings.json und notes/ (%AppData%, mit MSP_DEBUG_LOG neben dem Log).
- `LibraryLock`: hält notes/.lock exklusiv. `App.OnStartup` holt die Sperre, beendet eine zweite Instanz mit Meldung und erzeugt erst danach das Hauptfenster (kein StartupUri).
- `SettingsService`: settings.json laden (mit Standardwerten bei Fehler) und speichern.
- `PdfExporter`: `NoteDocument` zu PDF (PDFsharp). `PressureSegments` liefert die Linienstücke eines Druckstrichs. Striche mit `FitToCurve = false` (Formen) gehen ohne Bezier-Fit durch ihre Punkte. Bilder kommen per `XImage.FromStream` (PNG, Core-Build) vor den Strichen auf die Seite, gleiche Koordinaten.
- `SelectionBounds`: Seitenverhältnis beim Skalieren halten (die nicht gezogene Seite bleibt stehen) und eine Auswahl auf dem A4-Blatt halten bzw. auf der Endlos-Fläche nicht über den linken und oberen Rand.
- `ShapeRecognizer`: Linie, Kreis, Ellipse, Rechteck oder Dreieck in einer Punktliste, sonst null (Methode in decisions-2.md). `Polyline`: Geometrie dafür (Abstände, Douglas-Peucker, gleichmäßiges Nachabtasten, Drehung). `HoldDetector`: hat der Stift am Strichende lange genug stillgestanden?
- `RelativeDate`: Änderungszeit kurz und relativ („heute 10:04“, „gestern“, „28.09.“, Jahr nur wenn nicht das aktuelle).
- `DebugLog`: hängt Zeilen an die Datei aus MSP_DEBUG_LOG an, kulturinvariant.

## Datenfluss

- Toolbar oder Shortcut setzt den Zustand an `DocumentView`, die ihn auf alle Seiten anwendet.
- `DocumentView.Changed` meldet jede Dokumentänderung (Strich, Radieren, Undo/Redo, Seite, Stil, Modus), `NotebookLibrary.MarkChanged` setzt damit das Änderungsflag und stempelt `modified` (das Heft rückt in der Liste sofort nach oben). Geschrieben wird beim Heftwechsel, beim Schließen und per Timer alle 60 s, jeweils nur mit Änderung; die Scrollposition kommt bei jedem dieser Schritte in den Index.
- InkCanvas sammelt Striche selbst (Maus und Stylus). `DocumentView` hängt sich an StrokeCollected / StrokeErasing für Undo.
- Editiermodus je Seite (`EditingModes`): Pan aktiv = None, Auswahl-Werkzeug, Seitentaste oder offene Auswahl = Select, Radierer = EraseByStroke, sonst Ink. EditingModeInverted = EraseByStroke. Undo, Redo und jeder Seitenwechsel (Laden, Moduswechsel) heben die Auswahl zuerst auf.

## NuGet-Pakete

- App: PDFsharp 6.2.4 (Core-Build, MIT). Sonst keine.
- Tests: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk.
