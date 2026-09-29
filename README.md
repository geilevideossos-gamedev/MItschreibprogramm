# Mitschreibprogramm

Handschriftliche Notizen unter Windows, gemacht für ein One by Wacom und Windows Ink. Eine einzelne exe, keine Installation, keine Cloud, kein Konto. Ein Heft pro Fach, schreiben, radieren, blättern, automatisch gespeichert, als PDF exportieren, fertig.

## Voraussetzungen

- Windows 10 oder Windows 11, 64 Bit.
- Wacom-Treiber installiert (gebaut für das One by Wacom Medium CTL-672 mit seinem Stift, 2048 Druckstufen, zwei Seitentasten). Mit der Maus funktioniert alles auch, nur ohne Druck.
- Sonst nichts. .NET steckt in der exe.

## Wacom einrichten

Alle Einstellungen stehen in den "Wacom Tablett-Eigenschaften" (Startmenü, bei neuen Treibern über das "Wacom Center").

1. **Windows Ink einschalten.** Werkzeug "Stift" wählen, Reiter "Projektion", Haken bei **"Windows Ink verwenden"**. Ohne diesen Haken sieht das Programm den Stift nur als Maus: kein Druck, kein Radieren per Seitentaste.
2. **Seitentasten belegen** (Reiter "Stift"). Empfehlung:
   - untere Taste: **"Radieren"**. Gedrückt halten und über einen Strich fahren löscht ihn, loslassen schreibt wieder. Die Belegung "Rechtsklick" funktioniert genauso.
   - obere Taste: "Tastenanschlag" **Strg+Z** (Rückgängig).
   - Das Programm kann die beiden Tasten nicht unterscheiden, Windows meldet nur "Seitentaste gedrückt". Welche Taste radiert, legt also allein der Treiber fest. Liegt auf beiden "Rechtsklick", radieren beide.
3. **Fläche sinnvoll mappen** (Reiter "Projektion"). Die aktive Fläche der One by Wacom ist etwa 216 x 135 mm groß. Auf einen großen Bildschirm gemappt wird aus einer kleinen Handbewegung ein großer Strich, die Schrift wirkt dann zittrig. Besser: Bildschirmbereich "Teilbereich" wählen und nur den Bereich nehmen, in dem die Seite liegt. "Proportionen erzwingen" einschalten, sonst werden Kreise zu Eiern.
4. **"Gedrückt halten für Rechtsklick" abschalten.** Windows wartet sonst bei jedem Aufsetzen kurz, ob ein Rechtsklick gemeint ist, und der Strich beginnt verzögert. Systemsteuerung, "Stift- und Fingereingabe", Eintrag "Gedrückt halten", "Einstellungen", Haken bei "Gedrückthalten für Rechtsklick aktivieren" entfernen. Für die Schreibfläche schaltet das Programm diese Funktion selbst ab, die Windows-Einstellung wirkt aber zusätzlich im Treiber und in anderen Programmen.

## Start

`Mitschreibprogramm.exe` doppelklicken. Keine Installation. Die exe darf irgendwo liegen, auch auf einem USB-Stick. Beim ersten Start entpackt .NET ein paar Hilfsdateien nach `%TEMP%\.net`, das dauert einmalig ein paar Sekunden.

Einstellungen (Farbe, Breite, Seitenstil, Dark Mode, Fensterlage, letzter Ordner, Seitenleiste) liegen in `%AppData%\Mitschreibprogramm\settings.json`. Wer die Datei löscht, bekommt die Standardwerte zurück. Die Hefte liegen daneben in `%AppData%\Mitschreibprogramm\notes\`, eine `.msp`-Datei pro Heft plus `index.json` mit den Namen.

## Bedienung

Die Toolbar von links nach rechts:

- **Hefte.** Ganz links der Schalter für die Seitenleiste (auch Strg+B). Dort steht ein Heft pro Fach: Mathe, Englisch, Deutsch. Ein Klick öffnet ein Heft, das vorherige wird dabei automatisch gespeichert, und man landet an der Stelle, an der man es verlassen hat. **+ Neues Heft** legt ein Heft „Unbenannt“ an, Doppelklick benennt es um, Rechtsklick bietet Umbenennen, Löschen (mit Rückfrage, die Datei landet im Papierkorb), Als PDF exportieren und Als MSP exportieren. Die Liste ist nach zuletzt bearbeitet sortiert, unter jedem Namen steht, wann es zuletzt geändert wurde („heute 10:04“, „gestern“, „28.09.“). Das offene Heft ist markiert, die Titelleiste zeigt seinen Namen. Gespeichert wird von selbst: beim Heftwechsel, beim Schließen und alle 60 Sekunden, wenn sich etwas geändert hat. Beim Schließen fragt nichts mehr nach.
- **Neu, Importieren, MSP-Export.** Neu legt ein Heft an (Strg+N). Importieren kopiert eine `.msp`-Datei von außerhalb als neues Heft in die Bibliothek, die Datei bleibt liegen (Strg+O). MSP-Export schreibt das offene Heft als `.msp`-Datei irgendwohin, zum Weitergeben oder als Backup (Strg+Umschalt+S).
- **Vier Farbkreise:** Schwarz, Blau, Rot, Grün.
- **Breite:** Regler von 1 bis 12 px in halben Schritten, daneben die aktuelle Breite und die Voreinstellungen dünn (1,5), mittel (3), dick (6).
- **Druck:** mit Haken macht fester Druck den Strich breiter (etwa ein Viertel bis eindreiviertel der eingestellten Breite). Ohne Haken ist der Strich überall gleich breit. Die Maus schreibt immer gleich breit.
- **Stift / Radierer:** der Radierer löscht immer ganze Striche. Drei Wege: Button, Taste E, oder die untere Seitentaste am Stift gedrückt halten.
- **Bilder:** Strg+V fügt ein Bild aus der Zwischenablage ein, zum Beispiel einen Screenshot aus dem Snipping Tool (Win+Umschalt+S). Es landet in der Mitte des sichtbaren Bereichs, höchstens 80 % der Seite breit, und liegt unter den Strichen: man kann darauf schreiben. Verschieben, Größe ändern und Löschen geht über die Auswahl. Bilder werden im Heft gespeichert und sind im PDF-Export dabei.
- **Formen:** am Ende eines Strichs den Stift etwa eine halbe Sekunde stillhalten, dann wird aus einer gezeichneten Linie, einem Kreis, einer Ellipse, einem Rechteck oder einem Dreieck eine saubere Form. Mit dem Schalter **Formen** (Taste F) passiert das bei jedem Strich schon beim Loslassen. Handschrift bleibt Handschrift: kleine Striche und alles, was nicht klar nach Form aussieht, bleiben unverändert. Strg+Z direkt danach holt den freihand gezeichneten Strich zurück.
- **Seitenstil:** Blanko, Liniert (Linienabstand 8 mm), Kariert (Karo-Gitter mit 5 mm, wie im Rechenheft). **Linien: Blau / Schwarz** schaltet die Linienfarbe um. Die Linien sind nur Hintergrund und lassen sich nicht wegradieren.
- **Modus: Seiten / Endlos.** "Seiten" sind A4-Blätter untereinander, **+ Seite** hängt eines an. Wer in den unteren 15 % des letzten Blatts schreibt, bekommt automatisch ein neues. "Endlos" ist eine einzige Fläche, die nach unten und rechts mitwächst. Umschalten geht jederzeit: Seiten werden untereinander zusammengelegt, die Endlos-Fläche wird in A4-Blätter geschnitten (ein Strich landet auf dem Blatt, auf dem er beginnt).
- **Dunkel:** Dark Mode. Die Seite wird dunkelgrau, Schwarz wird weiß angezeigt. Gespeichert und exportiert wird trotzdem Schwarz auf Weiß.
- **PDF-Export:** A4-Seiten mit scharfen Vektorstrichen, auf Wunsch mit den Hintergrundlinien (Linien oder Karo-Gitter). Eine Endlos-Fläche wird dafür in A4-Blätter geschnitten. Vorgeschlagen wird der Heftname als Dateiname; über das Rechtsklick-Menü geht das auch für ein Heft, das gerade nicht offen ist.

Die Statusleiste unten zeigt Zoom, "Seite x von y" und den Heftnamen.

**Zoomen und Verschieben:** Strg+Mausrad zoomt um den Mauszeiger (25 % bis 400 %). Mausrad scrollt, Umschalt+Mausrad scrollt seitlich. Zum Verschieben die mittlere Maustaste ziehen oder die Leertaste halten und ziehen.

## Shortcuts

| Taste | Wirkung |
|-------|---------|
| 1 / 2 / 3 / 4 | Schwarz / Blau / Rot / Grün |
| P | Stift |
| E | Radierer an, noch einmal E oder P: wieder Stift |
| F | Formen erkennen an / aus (sonst: am Strichende stillhalten) |
| + / - | Strich breiter / schmaler |
| Strg+Z / Strg+Y | Rückgängig / Wiederholen |
| Strg+L | Seitenstil durchschalten (Blanko, Liniert, Kariert) |
| Strg+Enter | neue Seite (nur im Modus Seiten) |
| Strg+D | Dark Mode |
| Strg+Plus / Strg+Minus / Strg+0 | größer / kleiner / 100 % |
| Strg+Mausrad | Zoom um den Mauszeiger |
| Mausrad / Umschalt+Mausrad | senkrecht / waagrecht scrollen |
| Mittlere Maustaste ziehen, Leertaste halten + ziehen | Ansicht verschieben |
| Strg+B | Seitenleiste mit den Heften ein- oder ausblenden |
| Strg+N / Strg+O | neues Heft / .msp-Datei importieren |
| Strg+S / Strg+Umschalt+S | jetzt speichern (passiert sonst automatisch) / als .msp exportieren |
| Strg+E | PDF-Export |
| Strg+V | Bild aus der Zwischenablage einfügen |

## Dateiformat

Jedes Heft ist eine `.msp`-Datei: JSON als Klartext in UTF-8 mit Seitenmodus, Seitenstil, Linienfarbe und pro Seite den Strichen (Farbe, Breite, Druck an/aus, Punkte als `[x, y, druck]` in Seitenpixeln bei 100 %). Exportierte und importierte Dateien haben dasselbe Format wie die Hefte in `notes\`; dort kommt nur `index.json` mit Namen, Änderungszeit und Scrollposition dazu. Die genaue Beschreibung steht in [docs/file-format.md](docs/file-format.md).

## Google Drive

Das Programm selbst kennt keine Cloud. Für ein Backup den Ordner `%AppData%\Mitschreibprogramm\notes\` sichern, er enthält alle Hefte. Zum Teilen oder Sichern einzelner Hefte "Google Drive für Desktop" installieren und das Heft mit MSP-Export in den Drive-Ordner schreiben (zum Beispiel `G:\Meine Ablage\Mitschriften`), Drive lädt es im Hintergrund hoch; Importieren holt so eine Datei wieder als Heft herein. Das Programm merkt sich den zuletzt benutzten Ordner, beim nächsten Export steht der Dialog also schon dort.

## Build aus dem Quelltext

Braucht das .NET 8 SDK und Git Bash:

```bash
./build.sh
```

Das baut, testet und legt `dist/Mitschreibprogramm.exe` an. Der Befehl dahinter und alle Details stehen in [docs/build.md](docs/build.md). Wie das Programm ohne Tablet getestet wird (synthetischer Stift, Debug-Log, Screenshots), steht in [docs/testing.md](docs/testing.md).

## Bekannte Einschränkungen

- Mit echtem Wacom-Stift noch nicht abgenommen. Geprüft ist alles mit einem simulierten Windows-Ink-Stift, die offenen Punkte stehen in docs/testing.md unter "Offen mit echtem Pen".
- Der Radierer löscht nur ganze Striche, kein Teilradieren. Kein Neigen (Tilt), keine Rotation: der Stift der One by Wacom liefert beides nicht.
- Wer die Seitentaste mitten im Strich drückt, verliert den angefangenen Strich. Erst drücken, dann aufsetzen.
- Gespeichert wird beim Heftwechsel, beim Schließen und alle 60 Sekunden. Nach einem Absturz fehlt also höchstens die letzte Minute. Rückgängig reicht bis zum letzten Heftwechsel.
- Zoom um den Mauszeiger hält den Punkt waagrecht erst, wenn die Seite breiter als das Fenster ist. Vorher bleibt die Seite zentriert.
- Ein Strich gehört immer zu genau einem Blatt. Ragt er nach dem Umwandeln von Endlos in Seiten über den Blattrand, wird er dort abgeschnitten dargestellt (die Punkte bleiben erhalten).
- PDF nur A4 hoch. Ein Fenster, ein offenes Heft. Das Programm läuft nur einmal: ein zweiter Start meldet sich kurz und beendet sich, weil zwei Instanzen sich die Heftliste gegenseitig überschreiben würden.
- Die exe ist etwa 140 MB groß, weil .NET und WPF darin stecken.
