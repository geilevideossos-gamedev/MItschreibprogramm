# Features

Vollständiger Scope. Status: todo / wip / done / getestet (siehe track.md).

## Stift und Canvas

| ID | Feature | Status |
|----|---------|--------|
| F01 | Basis WPF InkCanvas. Pen über Windows Ink / Stylus-Events, Maus funktioniert immer | done |
| F02 | Genau vier Farben: Schwarz, Blau, Rot, Grün. Hex-Werte an einer Stelle als Konstanten | done |
| F03 | Strichbreite: Slider 1 bis 12 px in 0,5er-Schritten, Presets dünn (1,5), mittel (3), dick (6), aktuelle Breite sichtbar | done |
| F04 | Stiftdruck moduliert die Breite um die Basisbreite (IgnorePressure = false). Checkbox "Druck" zum Abschalten. Maus = konstante Breite | done |
| F05 | Radierer ganzer Strich (EraseByStroke). Aktivierung auf drei Wegen: Toolbar-Button, Taste E (bleibt an bis P oder E), oder untere Seitentaste am Pen gedrückt halten = Radierer nur solange gedrückt. Meldet der Wacom-Treiber die Taste als invertierten Stift (Tastenfunktion "Radieren" im Treiber), wirkt das ebenfalls als Radierer. Wie WPF Seitentasten und Inverted liefert, steht in docs/pen-input.md | done |
| F06 | Glättung an (FitToCurve) | done |
| F07 | Undo / Redo strichbasiert, Ctrl+Z / Ctrl+Y, Verlauf bis zum letzten Öffnen oder Neu (seit 2026-09-28: bis zum letzten Heftwechsel) | done |

## Seite und Hintergrund

| ID | Feature | Status |
|----|---------|--------|
| F08 | Hintergrundstil: Blanko, Liniert, Kariert. Liniert: Linienabstand 8 mm als Konstante. Kariert: Karo-Gitter aus durchgezogenen dünnen waagrechten und senkrechten Linien, Abstand 5 mm als Konstante. (Kariert ersetzt seit 2026-09-19 auf Daniels Wunsch den Stil Strichliert aus der ersten Spezifikation) | done |
| F09 | Linienfarbe Schwarz oder Blau, jeweils dezent (Alpha), umschaltbar | done |
| F10 | Hintergrund wird gezeichnet, ist kein Strich, nicht radierbar | done |

## Seitenmodell

| ID | Feature | Status |
|----|---------|--------|
| F11 | Modus "Seiten": A4 (210 x 297 mm bei 96 DPI), Seiten untereinander mit Abstand, Seitenzähler in der Statusleiste, neue Seite per Button und Ctrl+Enter, automatisch neue Seite beim Schreiben in den unteren 15 % der letzten Seite | done |
| F12 | Modus "Endlos": eine Fläche, wächst automatisch nach unten und rechts, wenn ein Strich in Randnähe kommt | done |
| F13 | Umschalten pro Dokument in der Toolbar. Seiten zu Endlos: Seiten werden untereinander zusammengeführt (Y-Offset = Seitenhöhe mal Index). Endlos zu Seiten: Fläche wird in A4-Höhen geschnitten, ein Strich gehört zur Seite, in der sein erster Punkt liegt. Einfachste korrekte Umsetzung, begründet in docs/decisions.md | done |

## Zoom und Pan

| ID | Feature | Status |
|----|---------|--------|
| F14 | Zoom 25 % bis 400 %: Ctrl+Mausrad, Ctrl+Plus, Ctrl+Minus, Ctrl+0 = 100 %. Zoom um die Mausposition. Zoomstufe in der Statusleiste | done |
| F15 | Pan: Mausrad vertikal, Shift+Mausrad horizontal, mittlere Maustaste ziehen, Leertaste halten + ziehen | done |
| F16 | Zoom über Transform, kein Rasterisieren, Striche in jeder Stufe scharf | done |

## Datei

| ID | Feature | Status |
|----|---------|--------|
| F17 | Eigenes Format `.msp`, JSON, UTF-8: version, pageMode, pageStyle, lineColor, pages[] mit strokes[] mit color, width, pressureEnabled, points als [x, y, pressure]. Koordinaten in Seitenpixeln bei 100 %. Genau dokumentiert in docs/file-format.md | done |
| F18 | Neu Ctrl+N, Öffnen Ctrl+O, Speichern Ctrl+S, Speichern unter Ctrl+Shift+S. Titelleiste zeigt Dateiname und `*` bei ungespeicherten Änderungen. (Überholt seit 2026-09-28 durch F49, F54, F55: Neu = neues Heft, Öffnen = Import, Speichern unter = Export, Ctrl+S = sofort speichern, Titel zeigt den Heftnamen ohne Stern) | done |
| F19 | Schließen mit Änderungen: Dialog Speichern / Verwerfen / Abbrechen. (Überholt seit 2026-09-28 durch F53: Schließen speichert automatisch, kein Dialog) | done |
| F20 | Standard-Speicherort: zuletzt verwendeter Ordner. Kein Cloud-Code in der App. Google-Drive-Sync passiert über den Drive-Desktop-Ordner, das kommt nur in die README. (Seit 2026-09-28 gilt der Ordner für Import und Export) | done |
| F21 | Settings in %AppData%/Mitschreibprogramm/settings.json: Farbe, Breite, Druck, Seitenstil, Linienfarbe, Seitenmodus, Dark Mode, Fenstergröße und -position, letzter Ordner (seit 2026-09-28 auch: Seitenleiste sichtbar) | done |

## PDF-Export

| ID | Feature | Status |
|----|---------|--------|
| F22 | Ctrl+E. PDFsharp aus NuGet, Version für .NET 8 geprüft (decisions.md). Einziges erlaubtes externes Paket | done |
| F23 | A4-Seiten, Striche als Vektorpfade, Druckbreite über Segmente mit eigener Breite oder einfachste Variante, die gut aussieht. Entscheidung in docs/decisions.md | done |
| F24 | Endlos-Dokument wird für den Export in A4-Höhen geschnitten | done |
| F25 | Export-Dialog mit Checkbox "Hintergrundlinien mit exportieren" (Standard an) | done |
| F26 | Export immer mit logischen Farben: Schwarz auf Weiß, auch im Dark Mode | done |

## Dark Mode

| ID | Feature | Status |
|----|---------|--------|
| F27 | Toggle in der Toolbar und Ctrl+D, wird gespeichert | done |
| F28 | UI dunkel, Seite dunkelgrau, Hintergrundlinien heller. Stift "Schwarz" wird weiß dargestellt, gespeichert und exportiert bleibt Schwarz. Blau, Rot, Grün unverändert | done |

## Shortcuts

| ID | Feature | Status |
|----|---------|--------|
| F29 | 1 / 2 / 3 / 4 = Schwarz / Blau / Rot / Grün | done |
| F30 | P = Stift, E = Radierer | done |
| F31 | Plus / Minus (ohne Ctrl) = Breite auf / ab | done |
| F32 | Ctrl+L = Seitenstil durchschalten (Blanko, Liniert, Kariert) | done |
| F33 | Ctrl+Enter = neue Seite (nur Seitenmodus) | done |
| F34 | Ctrl+D = Dark Mode | done |
| F35 | Ctrl+Z / Ctrl+Y, Ctrl+N / O / S / Shift+S / E, Ctrl+Plus / Minus / 0. Einzeltasten bleiben Einzeltasten, damit sie auf die ExpressKeys gelegt werden können. Alle Shortcuts stehen als Tabelle in der README. (Seit 2026-09-29: das Tablet ist ein One by Wacom CTL-672 ohne ExpressKeys, Einzeltasten bleiben) | done |

## UI

| ID | Feature | Status |
|----|---------|--------|
| F36 | Ein Fenster, schlanke Toolbar: Datei-Buttons, vier Farbkreise, Breiten-Slider mit Anzeige, drei Presets, Druck-Checkbox, Stift/Radierer, Seitenstil-Dropdown, Linienfarbe-Toggle, Seitenmodus-Toggle, Dark-Mode-Toggle, Export | done |
| F37 | Statusleiste: Zoom, Seite x von y, Dateiname (seit 2026-09-28: Heftname) | done |
| F38 | Native WPF-Controls, minimale Styles, kein Ribbon, keine Menüleiste außer Datei | done |

## Build und Auslieferung

| ID | Feature | Status |
|----|---------|--------|
| F39 | `dotnet publish` (win-x64, self-contained, single file) ergibt dist/Mitschreibprogramm.exe, kein Trimming | done |
| F40 | build.sh für Git Bash, Befehl auch in docs/build.md | done |
| F41 | README.md am Ende, Deutsch, mit diesen Abschnitten: Was ist das, Voraussetzungen (Windows 10/11, Wacom-Treiber installiert). Wacom-Einrichtung ("Windows Ink verwenden", Seitentasten mit untere = Radierer halten, Vorschlag für die 4 ExpressKeys, Mapping der kleinen Fläche, "Gedrückt halten für Rechtsklick" abschalten). Start (exe, keine Installation), Bedienung, Shortcut-Tabelle, Dateiformat. Google-Drive-Tipp. Build aus Source, bekannte Einschränkungen. (Seit 2026-09-29 ohne ExpressKeys-Abschnitt, siehe decisions-2.md) | done |

## Selbsttest

| ID | Feature | Status |
|----|---------|--------|
| F42 | tools/pen-sim.ps1 (PowerShell, Add-Type mit P/Invoke): CreateSyntheticPointerDevice(PT_PEN) und InjectSyntheticPointerInput injizieren echte Pen-Eingaben: Position, Druck 0 bis 1024, penFlags BARREL, INVERTED, ERASER. Holt sich das Fenster der laufenden App und zeichnet definierte Striche: steigender Druck, Seitentaste über bestehendem Strich, invertiert, Strich bei 200 % Zoom (die Striche stehen in tools/selftest/, siehe decisions.md). Windows 10 1809+, kein Admin | done |
| F43 | Debug-Log: mit MSP_DEBUG_LOG=<pfad> schreibt die App pro fertigem Strich eine Zeile (Punkte, min/max Druck, Breite, Modus, Seite, Zoom). Ohne Variable läuft nichts davon | done |
| F44 | tools/screenshot.ps1 speichert das App-Fenster als PNG nach tmp/ (Hintergrundstile, Linienfarbe, Zoom, Dark Mode, Seitenmodell, Toolbar) | done |
| F45 | xUnit-Projekt Mitschreibprogramm.Tests für Models/Services: .msp Round-Trip, Seiten zu Endlos und zurück, PDF-Export erzeugt gültige Datei mit erwarteter Seitenzahl, Settings laden und speichern. Kein UI dort | done |

## Hefte-Bibliothek (Auftrag vom 2026-09-28)

| ID | Feature | Status |
|----|---------|--------|
| F46 | Hefte-Bibliothek mit Seitenleiste: ein Heft = ein Fach (Mathe, Englisch, Deutsch, ...), jedes Heft ist ein komplettes mehrseitiges Dokument wie bisher eine .msp-Datei | done |
| F47 | Links ein einklappbares Panel (Toggle-Button „Hefte“ in der Toolbar und Ctrl+B) mit der Liste aller Hefte. Sichtbarkeit wird in settings.json gemerkt | done |
| F48 | Heft anklicken öffnet es sofort, das aktuelle wird vorher automatisch gespeichert ohne Dialog, man landet auf der Seite, wo man zuletzt war (letzte Scrollposition pro Heft) | done |
| F49 | Oben im Panel „+ Neues Heft“ (auch Neu / Ctrl+N), legt ein Heft „Unbenannt“ an und öffnet es | done |
| F50 | Doppelklick = Umbenennen. Rechtsklick-Kontextmenü mit Umbenennen, Löschen (mit Rückfrage, die Datei landet im Papierkorb), Als PDF exportieren, Als MSP exportieren | done |
| F51 | Aktives Heft markiert, Sortierung nach zuletzt bearbeitet, neueste oben (unter dem Namen steht die Änderungszeit, damit die Reihenfolge nachvollziehbar ist) | done |
| F52 | Speicherung: alle Hefte intern als einzelne .msp in %AppData%/Mitschreibprogramm/notes/, Dateiname = id. Anzeigename, Zeitstempel und Scrollposition in index.json (Begründung in decisions.md) | done |
| F53 | Autosave beim Heftwechsel, beim Schließen der App und alle 60 Sekunden bei Änderungen. Beim App-Start wird das zuletzt offene Heft geladen | done |
| F54 | Öffnen (Ctrl+O, Toolbar „Importieren“) und Speichern unter (Ctrl+Umschalt+S, Toolbar „MSP-Export“) bleiben als Import / Export für externe .msp-Dateien, Import kopiert in die Bibliothek | done |
| F55 | PDF-Export (Ctrl+E und Kontextmenü) exportiert das ganze Heft wie bisher, vorgeschlagener Dateiname = Heftname. Titelleiste zeigt den Heftnamen | done |
