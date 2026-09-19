# Mitschreibprogramm

Handschriftliche Notizen unter Windows, gemacht für ein Wacom Intuos Small und Windows Ink. Eine einzelne exe, keine Installation, keine Cloud, kein Konto. Schreiben, radieren, blättern, als PDF exportieren, fertig.

## Voraussetzungen

- Windows 10 oder Windows 11, 64 Bit.
- Wacom-Treiber installiert (gebaut für das Wacom Intuos Small CTL-4100K-S mit dem Wacom Pen 4K). Mit der Maus funktioniert alles auch, nur ohne Druck.
- Sonst nichts. .NET steckt in der exe.

## Wacom einrichten

Alle Einstellungen stehen in den "Wacom Tablett-Eigenschaften" (Startmenü, bei neuen Treibern über das "Wacom Center").

1. **Windows Ink einschalten.** Werkzeug "Stift" wählen, Reiter "Projektion", Haken bei **"Windows Ink verwenden"**. Ohne diesen Haken sieht das Programm den Stift nur als Maus: kein Druck, kein Radieren per Seitentaste.
2. **Seitentasten belegen** (Reiter "Stift"). Empfehlung:
   - untere Taste: **"Radieren"**. Gedrückt halten und über einen Strich fahren löscht ihn, loslassen schreibt wieder. Die Belegung "Rechtsklick" funktioniert genauso.
   - obere Taste: "Tastenanschlag" **Strg+Z** (Rückgängig).
   - Das Programm kann die beiden Tasten nicht unterscheiden, Windows meldet nur "Seitentaste gedrückt". Welche Taste radiert, legt also allein der Treiber fest. Liegt auf beiden "Rechtsklick", radieren beide.
3. **ExpressKeys belegen** (Reiter "ExpressKeys", jeweils "Tastenanschlag"). Alle Shortcuts des Programms sind dafür gemacht. Vorschlag für die vier Tasten:

   | Taste | Tastenanschlag | Wirkung |
   |-------|----------------|---------|
   | 1 | Strg+Z | Rückgängig |
   | 2 | E | Radierer an / aus |
   | 3 | 1 | Farbe Schwarz (oder 2, 3, 4 für Blau, Rot, Grün) |
   | 4 | Strg+Enter | neue Seite |

4. **Fläche sinnvoll mappen** (Reiter "Projektion"). Das Intuos Small ist nur etwa 152 x 95 mm groß. Auf den ganzen Bildschirm gemappt wird aus einer kleinen Handbewegung ein großer Strich, die Schrift wirkt dann zittrig. Besser: Bildschirmbereich "Teilbereich" wählen und nur den Bereich nehmen, in dem die Seite liegt. "Proportionen erzwingen" einschalten, sonst werden Kreise zu Eiern.
5. **"Gedrückt halten für Rechtsklick" abschalten.** Windows wartet sonst bei jedem Aufsetzen kurz, ob ein Rechtsklick gemeint ist, und der Strich beginnt verzögert. Systemsteuerung, "Stift- und Fingereingabe", Eintrag "Gedrückt halten", "Einstellungen", Haken bei "Gedrückthalten für Rechtsklick aktivieren" entfernen. Für die Schreibfläche schaltet das Programm diese Funktion selbst ab, die Windows-Einstellung wirkt aber zusätzlich im Treiber und in anderen Programmen.

## Start

`Mitschreibprogramm.exe` doppelklicken. Keine Installation. Die exe darf irgendwo liegen, auch auf einem USB-Stick. Beim ersten Start entpackt .NET ein paar Hilfsdateien nach `%TEMP%\.net`, das dauert einmalig ein paar Sekunden.

Einstellungen (Farbe, Breite, Seitenstil, Dark Mode, Fensterlage, letzter Ordner) liegen in `%AppData%\Mitschreibprogramm\settings.json`. Wer die Datei löscht, bekommt die Standardwerte zurück.

## Bedienung

Die Toolbar von links nach rechts:

- **Neu, Öffnen, Speichern, Speichern unter.** Die Titelleiste zeigt den Dateinamen, ein `*` heißt: ungespeicherte Änderungen. Beim Schließen, bei Neu und bei Öffnen fragt das Programm dann nach (Speichern / Verwerfen / Abbrechen). Es gibt kein automatisches Speichern.
- **Vier Farbkreise:** Schwarz, Blau, Rot, Grün.
- **Breite:** Regler von 1 bis 12 px in halben Schritten, daneben die aktuelle Breite und die Voreinstellungen dünn (1,5), mittel (3), dick (6).
- **Druck:** mit Haken macht fester Druck den Strich breiter (etwa ein Viertel bis eindreiviertel der eingestellten Breite). Ohne Haken ist der Strich überall gleich breit. Die Maus schreibt immer gleich breit.
- **Stift / Radierer:** der Radierer löscht immer ganze Striche. Drei Wege: Button, Taste E, oder die untere Seitentaste am Stift gedrückt halten.
- **Seitenstil:** Blanko, Liniert (Linienabstand 8 mm), Kariert (Karo-Gitter mit 5 mm, wie im Rechenheft). **Linien: Blau / Schwarz** schaltet die Linienfarbe um. Die Linien sind nur Hintergrund und lassen sich nicht wegradieren.
- **Modus: Seiten / Endlos.** "Seiten" sind A4-Blätter untereinander, **+ Seite** hängt eines an. Wer in den unteren 15 % des letzten Blatts schreibt, bekommt automatisch ein neues. "Endlos" ist eine einzige Fläche, die nach unten und rechts mitwächst. Umschalten geht jederzeit: Seiten werden untereinander zusammengelegt, die Endlos-Fläche wird in A4-Blätter geschnitten (ein Strich landet auf dem Blatt, auf dem er beginnt).
- **Dunkel:** Dark Mode. Die Seite wird dunkelgrau, Schwarz wird weiß angezeigt. Gespeichert und exportiert wird trotzdem Schwarz auf Weiß.
- **PDF-Export:** A4-Seiten mit scharfen Vektorstrichen, auf Wunsch mit den Hintergrundlinien (Linien oder Karo-Gitter). Eine Endlos-Fläche wird dafür in A4-Blätter geschnitten.

Die Statusleiste unten zeigt Zoom, "Seite x von y" und die Datei.

**Zoomen und Verschieben:** Strg+Mausrad zoomt um den Mauszeiger (25 % bis 400 %). Mausrad scrollt, Umschalt+Mausrad scrollt seitlich. Zum Verschieben die mittlere Maustaste ziehen oder die Leertaste halten und ziehen.

## Shortcuts

| Taste | Wirkung |
|-------|---------|
| 1 / 2 / 3 / 4 | Schwarz / Blau / Rot / Grün |
| P | Stift |
| E | Radierer an, noch einmal E oder P: wieder Stift |
| + / - | Strich breiter / schmaler |
| Strg+Z / Strg+Y | Rückgängig / Wiederholen |
| Strg+L | Seitenstil durchschalten (Blanko, Liniert, Kariert) |
| Strg+Enter | neue Seite (nur im Modus Seiten) |
| Strg+D | Dark Mode |
| Strg+Plus / Strg+Minus / Strg+0 | größer / kleiner / 100 % |
| Strg+Mausrad | Zoom um den Mauszeiger |
| Mausrad / Umschalt+Mausrad | senkrecht / waagrecht scrollen |
| Mittlere Maustaste ziehen, Leertaste halten + ziehen | Ansicht verschieben |
| Strg+N / Strg+O | Neu / Öffnen |
| Strg+S / Strg+Umschalt+S | Speichern / Speichern unter |
| Strg+E | PDF-Export |

Einzeltasten sind absichtlich Einzeltasten, damit sie auf die ExpressKeys passen.

## Dateiformat

Notizen sind `.msp`-Dateien: JSON als Klartext in UTF-8 mit Seitenmodus, Seitenstil, Linienfarbe und pro Seite den Strichen (Farbe, Breite, Druck an/aus, Punkte als `[x, y, druck]` in Seitenpixeln bei 100 %). Die genaue Beschreibung steht in [docs/file-format.md](docs/file-format.md).

## Google Drive

Das Programm selbst kennt keine Cloud. Für Sync und Backup "Google Drive für Desktop" installieren und die Notizen in den Drive-Ordner speichern (zum Beispiel `G:\Meine Ablage\Mitschriften`). Drive lädt sie im Hintergrund hoch. Das Programm merkt sich den zuletzt benutzten Ordner, beim nächsten Speichern steht der Dialog also schon dort.

## Build aus dem Quelltext

Braucht das .NET 8 SDK und Git Bash:

```bash
./build.sh
```

Das baut, testet und legt `dist/Mitschreibprogramm.exe` an. Der Befehl dahinter und alle Details stehen in [docs/build.md](docs/build.md). Wie das Programm ohne Tablet getestet wird (synthetischer Stift, Debug-Log, Screenshots), steht in [docs/testing.md](docs/testing.md).

## Bekannte Einschränkungen

- Mit echtem Wacom-Stift noch nicht abgenommen. Geprüft ist alles mit einem simulierten Windows-Ink-Stift, die offenen Punkte stehen in docs/testing.md unter "Offen mit echtem Pen".
- Der Radierer löscht nur ganze Striche, kein Teilradieren. Kein Neigen (Tilt), keine Rotation: der Pen 4K liefert beides nicht.
- Wer die Seitentaste mitten im Strich drückt, verliert den angefangenen Strich. Erst drücken, dann aufsetzen.
- Kein automatisches Speichern und keine Wiederherstellung nach einem Absturz. Rückgängig reicht bis zum letzten Öffnen oder Neu.
- Zoom um den Mauszeiger hält den Punkt waagrecht erst, wenn die Seite breiter als das Fenster ist. Vorher bleibt die Seite zentriert.
- Ein Strich gehört immer zu genau einem Blatt. Ragt er nach dem Umwandeln von Endlos in Seiten über den Blattrand, wird er dort abgeschnitten dargestellt (die Punkte bleiben erhalten).
- PDF nur A4 hoch. Ein Fenster, ein Dokument.
- Die exe ist etwa 140 MB groß, weil .NET und WPF darin stecken.
