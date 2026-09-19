Lies zuerst CLAUDE.md komplett. Alles darin gilt für die gesamte Arbeit an diesem Projekt.

# Ziel

Ein Handschrift-Notizprogramm für Windows in C# / WPF / .NET 8, bedient mit einem Wacom Intuos Small über Windows Ink. Am Ende steht eine einzelne exe in dist/ plus eine README, mit der jemand ohne Vorwissen die exe benutzen kann. Kein Bulk, nur die Features aus diesem Prompt. Diese Liste wird 1:1 zu docs/features.md.

# Hardware, mit der gearbeitet wird

- Wacom Intuos Small CTL-4100K-S, USB. Stift Wacom Pen 4K: 4096 Druckstufen, zwei Seitentasten, kein Tilt, kein Radierer am Stiftende.
- 4 ExpressKeys am Tablet, vom Treiber als Tastendrücke gesendet. Die App sieht sie als normale Tastatur.
- Mögliche Pen-Inputs für die App: Position, Druck, Seitentaste 1, Seitentaste 2, Hover. Sonst nichts. Tilt und Rotation nie implementieren.
- Druck kommt nur mit aktivem "Windows Ink verwenden" im Wacom-Treiber an. Alles dazu und alle Fallstricke sammelst du in docs/pen-input.md.

# Selbsttest ohne Pen

Daniel hat das Tablet nicht immer dabei. Du testest deshalb selbst, in vier Schichten:

1. Synthetischer Pen: tools/pen-sim.ps1 (PowerShell, Add-Type mit P/Invoke) injiziert über CreateSyntheticPointerDevice(PT_PEN) und InjectSyntheticPointerInput echte Pen-Eingaben ins System: Position, Druck 0 bis 1024, penFlags für Seitentaste (BARREL), invertiert (INVERTED) und Radierer (ERASER). Das Skript holt sich das Fenster der laufenden App (FindWindow, GetWindowRect) und zeichnet definierte Striche: Linie mit steigendem Druck, Strich mit gehaltener Seitentaste über einen bestehenden Strich, Strich invertiert, Strich bei 200 % Zoom. Windows 10 1809+, kein Admin. Structs und Flags über Context7 bzw. Win32-Doku, nicht raten.
2. Debug-Log: mit Umgebungsvariable MSP_DEBUG_LOG=<pfad> schreibt die App pro fertigem Strich eine Zeile (Punkte, min/max Druck, Breite, Modus, Seite, Zoom). Ohne Variable läuft nichts davon. Damit prüfst du, ob Druck und Tasten wirklich als Stylus ankommen und nicht als Maus.
3. Screenshots: tools/screenshot.ps1 speichert das App-Fenster als PNG nach tmp/, du schaust das Bild an. Für Hintergrundstile, Linienfarbe, Zoom, Dark Mode, Seitenmodell, Toolbar.
4. xUnit-Projekt Mitschreibprogramm.Tests für Models/Services: .msp Round-Trip, Seiten zu Endlos und zurück, PDF-Export erzeugt gültige Datei mit erwarteter Seitenzahl, Settings laden und speichern. Kein UI dort.

Als Erstes in Schritt 2 prüfen, ob WPFs Standard-Stylus-Stack die synthetische Eingabe als Stylus mit Druck meldet. Falls nicht: Pointer-Stack über den AppContext-Switch Switch.System.Windows.Input.Stylus.EnablePointerSupport, nur aktiv wenn MSP_DEBUG_LOG gesetzt ist, Entscheidung in docs/decisions.md. Dann bleibt der Standard-Stack ein Rest-Risiko für den echten Pen und kommt in docs/testing.md unter "Offen mit echtem Pen".

Voraussetzungen für den Selbsttest: Laptop an, angemeldet, Bildschirm entsperrt, App-Fenster im Vordergrund, Daniel benutzt währenddessen nicht die Maus. Vor jedem Injektionslauf eine kurze Meldung, dass du jetzt Eingaben injizierst.

# Feature-Spezifikation

## Stift und Canvas

- Basis ist WPF InkCanvas. Pen über Windows Ink / Stylus-Events, Maus muss immer auch funktionieren.
- Genau vier Farben: Schwarz, Blau, Rot, Grün. Hex-Werte an einer Stelle als Konstanten.
- Strichbreite: Slider 1 bis 12 px in 0,5er-Schritten, dazu drei Presets dünn (1,5), mittel (3), dick (6). Aktuelle Breite sichtbar.
- Stiftdruck moduliert die Breite um die eingestellte Basisbreite (IgnorePressure = false). Checkbox "Druck" zum Abschalten. Maus = konstante Breite.
- Radierer: ganzer Strich (EraseByStroke). Aktivierung auf drei Wegen: Toolbar-Button, Taste E (bleibt an bis P oder E), oder untere Seitentaste am Pen gedrückt halten = Radierer nur solange gedrückt. Meldet der Wacom-Treiber die Taste als invertierten Stift (Tastenfunktion "Radieren" im Treiber), muss das ebenfalls als Radierer wirken. Wie WPF Seitentasten und Inverted liefert: Context7, dann in docs/pen-input.md.
- Glättung an (FitToCurve).
- Undo / Redo strichbasiert, Ctrl+Z / Ctrl+Y, Verlauf bis zum letzten Öffnen oder Neu.

## Seite und Hintergrund

- Hintergrundstil: Blanko, Liniert, Strichliert (gestrichelte Linien). Linienabstand 8 mm als Konstante.
- Linienfarbe: Schwarz oder Blau, jeweils dezent (Alpha), umschaltbar.
- Hintergrund wird gezeichnet, ist kein Strich, ist nicht radierbar.

## Seitenmodell

- Modus "Seiten": A4 (210 x 297 mm bei 96 DPI), Seiten untereinander mit Abstand, Seitenzähler in der Statusleiste. Neue Seite per Button und Ctrl+Enter. Automatisch neue Seite, wenn auf der letzten Seite in den unteren 15 % geschrieben wird.
- Modus "Endlos": eine Fläche, wächst automatisch nach unten und rechts, wenn ein Strich in Randnähe kommt.
- Umschalten pro Dokument in der Toolbar. Seiten zu Endlos: Seiten werden untereinander zusammengeführt (Y-Offset = Seitenhöhe mal Index). Endlos zu Seiten: Fläche wird in A4-Höhen geschnitten, ein Strich gehört zur Seite, in der sein erster Punkt liegt. Einfachste korrekte Umsetzung wählen und in docs/decisions.md begründen.

## Zoom und Pan

- Zoom 25 % bis 400 %. Ctrl+Mausrad, Ctrl+Plus, Ctrl+Minus, Ctrl+0 = 100 %. Zoom um die Mausposition. Zoomstufe in der Statusleiste.
- Pan: Mausrad vertikal, Shift+Mausrad horizontal, mittlere Maustaste ziehen, Leertaste halten + ziehen.
- Zoom über Transform, kein Rasterisieren. Striche bleiben in jeder Stufe scharf.

## Datei

- Eigenes Format `.msp`, JSON, UTF-8: version, pageMode, pageStyle, lineColor, pages[] mit strokes[] mit color, width, pressureEnabled, points als [x, y, pressure]. Koordinaten in Seitenpixeln bei 100 %. Genau dokumentieren in docs/file-format.md.
- Neu Ctrl+N, Öffnen Ctrl+O, Speichern Ctrl+S, Speichern unter Ctrl+Shift+S. Titelleiste zeigt Dateiname und `*` bei ungespeicherten Änderungen.
- Schließen mit Änderungen: Dialog Speichern / Verwerfen / Abbrechen.
- Standard-Speicherort: zuletzt verwendeter Ordner. Kein Cloud-Code in der App. Google-Drive-Sync passiert über den Drive-Desktop-Ordner, das kommt nur in die README.
- Settings in %AppData%/Mitschreibprogramm/settings.json: letzte Farbe, Breite, Druck an/aus, Seitenstil, Linienfarbe, Seitenmodus, Dark Mode, Fenstergröße und -position, letzter Ordner.

## PDF-Export

- Ctrl+E. PDFsharp aus NuGet, Version für .NET 8 über Context7 prüfen. Einziges erlaubtes externes Paket.
- A4-Seiten, Striche als Vektorpfade, Druckbreite über Segmente mit eigener Breite oder einfachste Variante, die gut aussieht. Entscheidung in docs/decisions.md.
- Endlos-Dokument wird für den Export in A4-Höhen geschnitten.
- Checkbox im Export-Dialog: Hintergrundlinien mit exportieren (Standard an).
- Export immer mit logischen Farben: Schwarz auf Weiß, auch wenn Dark Mode aktiv ist.

## Dark Mode

- Toggle in der Toolbar und Ctrl+D, wird gespeichert.
- UI dunkel, Seite dunkelgrau, Hintergrundlinien heller. Stift "Schwarz" wird auf dunkler Seite weiß dargestellt, gespeichert und exportiert bleibt Schwarz. Blau, Rot, Grün unverändert.

## Shortcuts

- 1 / 2 / 3 / 4 = Schwarz / Blau / Rot / Grün
- P = Stift, E = Radierer
- Plus / Minus (ohne Ctrl) = Breite auf / ab
- Ctrl+L = Seitenstil durchschalten (Blanko, Liniert, Strichliert)
- Ctrl+Enter = neue Seite (nur Seitenmodus)
- Ctrl+D = Dark Mode
- Ctrl+Z / Ctrl+Y, Ctrl+N / O / S / Shift+S / E, Ctrl+Plus / Minus / 0
- Einzeltasten bleiben Einzeltasten, damit sie auf die ExpressKeys gelegt werden können. Alle Shortcuts stehen als Tabelle in der README.

## UI

- Ein Fenster. Schlanke Toolbar oben: Datei-Buttons, vier Farbkreise, Breiten-Slider mit Anzeige, drei Presets, Druck-Checkbox, Stift/Radierer, Seitenstil-Dropdown, Linienfarbe-Toggle, Seitenmodus-Toggle, Dark-Mode-Toggle, Export.
- Statusleiste unten: Zoom, Seite x von y, Dateiname.
- Native WPF-Controls, minimale Styles, kein Ribbon, keine Menüleiste außer Datei.

## Build und Auslieferung

- `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist` ergibt dist/Mitschreibprogramm.exe. Kein Trimming (WPF).
- build.sh für Git Bash im Repo, Befehl auch in docs/build.md.
- README.md am Ende, Deutsch, mit diesen Abschnitten:
  - Was ist das, Voraussetzungen (Windows 10/11, Wacom-Treiber installiert)
  - Wacom-Einrichtung: "Windows Ink verwenden" im Wacom-Treiber aktivieren, empfohlene Belegung der Seitentasten (untere = Radierer halten), Vorschlag für die 4 ExpressKeys (z. B. Ctrl+Z, E, 1, Ctrl+Enter), Tipp zum Mapping der kleinen Fläche auf einen Bildschirmbereich, Windows-Einstellung "Gedrückt halten für Rechtsklick" abschalten wegen Strichverzögerung
  - Start (exe, keine Installation), Bedienung, Shortcut-Tabelle, Dateiformat
  - Google-Drive-Tipp (Notizen in den Drive-Desktop-Ordner speichern)
  - Build aus Source, bekannte Einschränkungen

# Erste Session, in dieser Reihenfolge

0. Heißt die Datei im Repo `claude.md`, mit `git mv` in `CLAUDE.md` umbenennen.
1. Umgebung prüfen: `dotnet --version` (mindestens 8), `git status`, `git remote -v`. Fehlt origin: `git remote add origin git@github.com:geilevideossos-gamedev/MItschreibprogramm.git`. Nichts pushen. Fehlt das .NET SDK: stoppen und melden.
2. .gitignore anlegen (bin/, obj/, dist/, tmp/, .vs/, *.user). .githooks/pre-push anlegen, das ohne `ALLOW_PUSH=1` mit Fehlermeldung abbricht, ausführbar machen, `git config core.hooksPath .githooks`. Testen, dass der Hook greift, ohne wirklich zu pushen (z. B. `git push --dry-run`).
3. docs/ nach CLAUDE.md anlegen. features.md enthält die komplette Liste von oben mit Status todo. pen-input.md startet mit den Hardware-Fakten von oben. track.md erklärt, wie Status, Checkpoints und Entscheidungen gepflegt werden. testing.md enthält die Checkpoints von unten, den Selbsttest-Ablauf und einen Abschnitt "Offen mit echtem Pen". Alle Dateien kurz.
4. Commit `chore: add gitignore, pre-push hook and docs skeleton`.
5. Schrittplan zeigen und auf "ok passt" warten. Danach ohne Nachfragen durcharbeiten bis zum nächsten Checkpoint.

# Vorgeschlagene Schritte und Checkpoints

1. Projekt-Skeleton: csproj (net8.0-windows, UseWPF), MainWindow mit InkCanvas, vier Farben, Breiten-Slider, Presets. Maus funktioniert. Build, Commit.
2. Pen: Druck, Seitentaste als Radierer, Inverted-Erkennung, Radierer-Modus, Glättung, Debug-Log, tools/pen-sim.ps1, tools/screenshot.ps1. CHECKPOINT 1: Selbsttest mit synthetischem Pen, Druckverlauf im Debug-Log prüfen, Screenshot anschauen.
3. Hintergrund (drei Stile, Linienfarbe), Zoom, Pan, Undo/Redo. CHECKPOINT 2: Screenshots pro Stil und Zoomstufe, Strich bei 200 % injizieren und Koordinaten im Debug-Log prüfen.
4. Seitenmodell Seiten / Endlos, Umschalten, neue Seite. CHECKPOINT 3: xUnit für Konvertierung, Screenshot beider Modi, Auto-Seite per injiziertem Strich unten.
5. Datei: Format, Neu / Öffnen / Speichern, Settings, Schließen-Dialog. CHECKPOINT 4: xUnit Round-Trip, Datei mit injizierten Strichen speichern, neu laden, Screenshot vergleichen.
6. PDF-Export. CHECKPOINT 5: xUnit Seitenzahl, PDF öffnen (pdftoppm o. ä. falls vorhanden, sonst mit PDFsharp lesen) und Bild anschauen.
7. Dark Mode, Shortcuts, Statusleiste.
8. Publish, build.sh, README. CHECKPOINT 6: exe aus dist/ starten, Selbsttest komplett gegen die exe wiederholen.
9. Abschluss: features.md auf done, status.md final, docs/testing.md "Offen mit echtem Pen" vollständig (Windows-Ink-Einstellung im Wacom-Treiber, echte Seitentasten, Druckkurve und Gefühl, ExpressKeys, Mapping der Fläche). `docs:`-Commit. Push nur, wenn Daniel es sagt.

An jedem Checkpoint: Selbsttest laufen lassen, Ergebnis in zwei Sätzen melden, bei Fehlern `fix:`-Commit und wiederholen. Ist Daniel mit Tablet da, zusätzlich sagen, was er testen soll, und auf "ok passt" warten. Sonst weiter.

Wenn etwas unklar ist, das eine Produktentscheidung wäre und weder hier noch in CLAUDE.md oder docs/ steht: fragen. Alles andere: Context7, entscheiden, in docs/decisions.md eintragen, weiter.
