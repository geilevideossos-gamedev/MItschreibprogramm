# Features

Vollständiger Scope. Status: todo / wip / done / getestet (siehe track.md).

## Stift und Canvas

| ID | Feature | Status |
|----|---------|--------|
| F01 | Basis WPF InkCanvas. Pen über Windows Ink / Stylus-Events, Maus funktioniert immer | done |
| F02 | Genau vier Farben: Schwarz, Blau, Rot, Grün. Hex-Werte an einer Stelle als Konstanten | done |
| F03 | Strichbreite: Slider 1 bis 12 px in 0,5er-Schritten, Presets dünn (1,5), mittel (3), dick (6), aktuelle Breite sichtbar | done |
| F04 | Stiftdruck moduliert die Breite um die Basisbreite (IgnorePressure = false). Checkbox "Druck" zum Abschalten. Maus = konstante Breite | done |
| F05 | Radierer ganzer Strich (EraseByStroke) über Toolbar-Button, Taste E (bleibt an bis P oder E) und untere Seitentaste gehalten (nur solange gedrückt). Invertierter Stift wirkt ebenfalls als Radierer | done |
| F06 | Glättung an (FitToCurve) | done |
| F07 | Undo / Redo strichbasiert, Ctrl+Z / Ctrl+Y, Verlauf bis zum letzten Öffnen oder Neu | done |

## Seite und Hintergrund

| ID | Feature | Status |
|----|---------|--------|
| F08 | Hintergrundstil Blanko, Liniert, Strichliert (gestrichelt). Linienabstand 8 mm als Konstante | done |
| F09 | Linienfarbe Schwarz oder Blau, jeweils dezent (Alpha), umschaltbar | done |
| F10 | Hintergrund wird gezeichnet, ist kein Strich, nicht radierbar | done |

## Seitenmodell

| ID | Feature | Status |
|----|---------|--------|
| F11 | Modus "Seiten": A4 (210 x 297 mm bei 96 DPI), Seiten untereinander mit Abstand, Seitenzähler in der Statusleiste, neue Seite per Button und Ctrl+Enter, automatisch neue Seite beim Schreiben in den unteren 15 % der letzten Seite | done |
| F12 | Modus "Endlos": eine Fläche, wächst automatisch nach unten und rechts, wenn ein Strich in Randnähe kommt | done |
| F13 | Umschalten pro Dokument in der Toolbar. Seiten zu Endlos: Y-Offset = Seitenhöhe mal Index. Endlos zu Seiten: Schnitt in A4-Höhen, Strich gehört zur Seite seines ersten Punkts | done |

## Zoom und Pan

| ID | Feature | Status |
|----|---------|--------|
| F14 | Zoom 25 % bis 400 %: Ctrl+Mausrad, Ctrl+Plus, Ctrl+Minus, Ctrl+0 = 100 %. Zoom um die Mausposition. Zoomstufe in der Statusleiste | done |
| F15 | Pan: Mausrad vertikal, Shift+Mausrad horizontal, mittlere Maustaste ziehen, Leertaste halten + ziehen | done |
| F16 | Zoom über Transform, kein Rasterisieren, Striche in jeder Stufe scharf | done |

## Datei

| ID | Feature | Status |
|----|---------|--------|
| F17 | Format `.msp`, JSON, UTF-8: version, pageMode, pageStyle, lineColor, pages[] mit strokes[] (color, width, pressureEnabled, points [x, y, pressure]), Koordinaten in Seitenpixeln bei 100 % | done |
| F18 | Neu Ctrl+N, Öffnen Ctrl+O, Speichern Ctrl+S, Speichern unter Ctrl+Shift+S. Titelleiste zeigt Dateiname und `*` bei ungespeicherten Änderungen | done |
| F19 | Schließen mit Änderungen: Dialog Speichern / Verwerfen / Abbrechen | done |
| F20 | Standard-Speicherort: zuletzt verwendeter Ordner. Kein Cloud-Code in der App | done |
| F21 | Settings in %AppData%/Mitschreibprogramm/settings.json: Farbe, Breite, Druck, Seitenstil, Linienfarbe, Seitenmodus, Dark Mode, Fenstergröße und -position, letzter Ordner | done |

## PDF-Export

| ID | Feature | Status |
|----|---------|--------|
| F22 | Export mit Ctrl+E über PDFsharp (einziges externes Paket) | done |
| F23 | A4-Seiten, Striche als Vektorpfade, Druck als Breite sichtbar | done |
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
| F32 | Ctrl+L = Seitenstil durchschalten (Blanko, Liniert, Strichliert) | done |
| F33 | Ctrl+Enter = neue Seite (nur Seitenmodus) | done |
| F34 | Ctrl+D = Dark Mode | done |
| F35 | Ctrl+Z / Ctrl+Y, Ctrl+N / O / S / Shift+S / E, Ctrl+Plus / Minus / 0. Einzeltasten bleiben Einzeltasten (ExpressKeys) | done |

## UI

| ID | Feature | Status |
|----|---------|--------|
| F36 | Ein Fenster, schlanke Toolbar: Datei-Buttons, vier Farbkreise, Breiten-Slider mit Anzeige, drei Presets, Druck-Checkbox, Stift/Radierer, Seitenstil-Dropdown, Linienfarbe-Toggle, Seitenmodus-Toggle, Dark-Mode-Toggle, Export | done |
| F37 | Statusleiste: Zoom, Seite x von y, Dateiname | done |
| F38 | Native WPF-Controls, minimale Styles, kein Ribbon, keine Menüleiste außer Datei | done |

## Build und Auslieferung

| ID | Feature | Status |
|----|---------|--------|
| F39 | `dotnet publish` (win-x64, self-contained, single file) ergibt dist/Mitschreibprogramm.exe, kein Trimming | done |
| F40 | build.sh für Git Bash, Befehl auch in docs/build.md | done |
| F41 | README.md auf Deutsch mit allen geforderten Abschnitten | done |

## Selbsttest

| ID | Feature | Status |
|----|---------|--------|
| F42 | tools/pen-sim.ps1: synthetischer Pen (Position, Druck, BARREL, INVERTED, ERASER) über InjectSyntheticPointerInput | done |
| F43 | Debug-Log hinter MSP_DEBUG_LOG: eine Zeile pro fertigem Strich (Punkte, min/max Druck, Breite, Modus, Seite, Zoom) | done |
| F44 | tools/screenshot.ps1: App-Fenster als PNG nach tmp/ | done |
| F45 | xUnit-Projekt Mitschreibprogramm.Tests: .msp Round-Trip, Seiten zu Endlos und zurück, PDF-Seitenzahl, Settings | done |
