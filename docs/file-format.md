# Dateiformate

## .msp

JSON, UTF-8 ohne BOM, kompakt in einer Zeile. Code: `Services/MspFileService`, Datenklassen `Models/NoteDocument`, `NotePage`, `NoteStroke`.

```json
{
  "version": 1,
  "pageMode": "pages",
  "pageStyle": "lined",
  "lineColor": "blue",
  "pages": [
    {
      "strokes": [
        {
          "color": "black",
          "width": 3,
          "pressureEnabled": true,
          "points": [[112.5, 241, 0.098], [122.5, 241, 0.12]]
        }
      ]
    }
  ]
}
```

| Feld | Werte | Bedeutung |
|------|-------|-----------|
| version | 1 | Formatversion. Eine höhere Version lehnt das Programm beim Öffnen ab |
| pageMode | `pages`, `endless` | Seitenmodus. Bei `endless` gibt es genau eine Seite, die ganze Fläche |
| pageStyle | `blank`, `lined`, `squared` | Hintergrundstil (Blanko, Liniert 8 mm, Kariert 5 mm). Der frühere Wert `dashed` wird als `squared` gelesen |
| lineColor | `black`, `blue` | Farbe der Hilfslinien |
| pages[] | | Seiten in Reihenfolge |
| strokes[] | | Striche in Zeichenreihenfolge (späterer Strich liegt oben) |
| color | `black`, `blue`, `red`, `green` | logische Stiftfarbe. Im Dark Mode wird `black` weiß dargestellt, gespeichert bleibt `black` |
| width | 1 bis 12 | Basisbreite in Seitenpixeln |
| pressureEnabled | true / false | true: Druck moduliert die Breite (Faktor 1,5 x Druck + 0,25). false: konstante Breite |
| points[] | `[x, y, druck]` | x, y in Seitenpixeln bei 100 % (96 DPI, A4 = 793,70 x 1122,52), Ursprung oben links auf der Seite. druck 0 bis 1, Maus = 0,5 |

- Rundung beim Speichern: x, y auf 2, druck auf 3 Nachkommastellen.
- Hilfslinien stehen nicht in der Datei, sie ergeben sich aus pageStyle und lineColor.
- Die Größe der Endlos-Fläche steht nicht in der Datei. Beim Öffnen wächst sie so weit, dass alle Striche plus Rand hineinpassen.
- Speichern schreibt erst `<name>.msp.tmp` und verschiebt sie dann über das Ziel.
- Tolerant beim Öffnen: Punkte ohne Druck bekommen 0,5, Punkte mit weniger als zwei Zahlen und Striche ohne Punkte fallen weg, Breite und Druck werden in ihre Grenzen geholt. `null` statt einer Liste gilt als leere Liste, `null`-Einträge fallen weg. Koordinaten werden auf plus/minus 1.000.000 begrenzt, damit eine kaputte Datei keine Millionen Seiten erzeugt.

## Hefte-Bibliothek: notes/

Ort: `%AppData%/Mitschreibprogramm/notes/`. Code: `Services/NotebookLibrary`, Datenklassen `Models/NotebookIndex`, `NotebookEntry`.

- Jedes Heft ist eine eigene `.msp`-Datei im Format oben, Dateiname `<id>.msp`. Die id ist eine GUID ohne Bindestriche (32 Hex-Zeichen) und ändert sich nie, auch nicht beim Umbenennen.
- `index.json` (JSON eingerückt, camelCase) hält, was nicht ins Dokument gehört:

```json
{
  "lastOpen": "3f2c9d1e8a4b4c6d9e0f1a2b3c4d5e6f",
  "notebooks": [
    { "id": "3f2c9d1e8a4b4c6d9e0f1a2b3c4d5e6f", "name": "Mathe", "modified": "2026-09-28T10:00:00+00:00", "scrollX": 0, "scrollY": 1234.5 }
  ]
}
```

| Feld | Bedeutung |
|------|-----------|
| lastOpen | id des zuletzt offenen Hefts, wird beim Start geöffnet. null oder unbekannt: das neueste Heft |
| id | Dateiname ohne `.msp` |
| name | Anzeigename, frei wählbar, darf doppelt vorkommen |
| modified | Zeitpunkt der letzten Inhaltsänderung (UTC, ISO 8601), gesetzt beim ersten Strich nach dem Öffnen, nicht erst beim Speichern. Umbenennen ändert ihn nicht. Sortierschlüssel der Seitenleiste, neueste oben |
| scrollX, scrollY | Seitenpixel bei 100 % des Dokumentpunkts, der beim Verlassen oben links im Fenster stand. Unabhängig vom Zoom |

- Beide Dateien werden wie die .msp erst als `.tmp` geschrieben und dann über das Ziel verschoben (`Services/AtomicFile`).
- Beim Start wird der Index mit dem Ordner abgeglichen: Einträge ohne Datei fallen weg, `.msp`-Dateien ohne Eintrag werden aufgenommen (name = Dateiname ohne Endung, bei einer eigenen id als Dateiname „Wiederhergestellt <Datum>“, modified = Änderungszeit der Datei). Unendliche oder absurde Scrollwerte werden 0. Fehlt der Index oder ist er kaputt, entsteht er so aus dem Ordner neu. Verloren gehen dann höchstens Namen und Scrollpositionen, nie ein Heft. Der Abgleich schreibt nichts.
- `.lock`: leere Datei, die die laufende Instanz exklusiv offen hält. Ein zweiter Start meldet „Mitschreibprogramm läuft bereits“ und beendet sich. Ein schreibgeschützter Ordner bleibt ohne Sperre benutzbar.
- Gelöschte Hefte wandern in den Papierkorb.
- Autosave: das Heft wird beim Heftwechsel, beim Schließen und alle 60 s geschrieben, wenn es seit dem letzten Schreiben eine Änderung gab. Scrollposition und lastOpen kommen bei jedem dieser Schritte in den Index.
- Mit gesetztem MSP_DEBUG_LOG liegt `notes/` neben der Logdatei statt in %AppData% (siehe settings.json).

## settings.json

Ort: `%AppData%/Mitschreibprogramm/settings.json`, JSON eingerückt. Code: `Services/SettingsService`, Datenklasse `Models/AppSettings`. Fehlt die Datei oder ist sie kaputt, gelten die Standardwerte. Enums stehen in beiden Formaten als Text, Zahlen und unbekannte Werte gelten als kaputt (.msp: Fehlermeldung beim Öffnen).

| Feld | Standard | Bedeutung |
|------|----------|-----------|
| penColor | `black` | letzte Stiftfarbe |
| strokeWidth | 3 | letzte Breite, wird auf 1 bis 12 begrenzt |
| pressureEnabled | true | Checkbox Druck |
| pageStyle | `lined` | Seitenstil für neue Dokumente, Werte wie in .msp (auch hier gilt `dashed` als `squared`) |
| lineColor | `blue` | Linienfarbe für neue Dokumente |
| pageMode | `pages` | Seitenmodus für neue Dokumente |
| darkMode | false | Dark Mode |
| notebookPanelVisible | true | Seitenleiste mit den Heften eingeblendet (Strg+B) |
| windowLeft, windowTop, windowWidth, windowHeight | null | Fensterlage im Normalzustand (WPF-Einheiten). null = Standardgröße, zentriert |
| windowMaximized | false | Fenster war maximiert |
| lastFolder | null | zuletzt benutzter Ordner für Importieren und Exportieren (.msp und PDF) |

Mit gesetztem MSP_DEBUG_LOG liegen settings.json und der Ordner notes/ neben der Logdatei statt in %AppData% (`Services/AppPaths`), damit Selbsttests weder die echten Einstellungen noch die echten Hefte anfassen.
