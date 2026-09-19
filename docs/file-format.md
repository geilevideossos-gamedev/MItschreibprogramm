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
| pageStyle | `blank`, `lined`, `dashed` | Hintergrundstil |
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

## settings.json

Ort: `%AppData%/Mitschreibprogramm/settings.json`, JSON eingerückt. Code: `Services/SettingsService`, Datenklasse `Models/AppSettings`. Fehlt die Datei oder ist sie kaputt, gelten die Standardwerte. Enums stehen in beiden Formaten als Text, Zahlen und unbekannte Werte gelten als kaputt (.msp: Fehlermeldung beim Öffnen).

| Feld | Standard | Bedeutung |
|------|----------|-----------|
| penColor | `black` | letzte Stiftfarbe |
| strokeWidth | 3 | letzte Breite, wird auf 1 bis 12 begrenzt |
| pressureEnabled | true | Checkbox Druck |
| pageStyle | `lined` | Seitenstil für neue Dokumente |
| lineColor | `blue` | Linienfarbe für neue Dokumente |
| pageMode | `pages` | Seitenmodus für neue Dokumente |
| darkMode | false | Dark Mode |
| windowLeft, windowTop, windowWidth, windowHeight | null | Fensterlage im Normalzustand (WPF-Einheiten). null = Standardgröße, zentriert |
| windowMaximized | false | Fenster war maximiert |
| lastFolder | null | zuletzt benutzter Ordner für Öffnen / Speichern / Export |

Mit gesetztem MSP_DEBUG_LOG liegt settings.json neben der Logdatei statt in %AppData%, damit Selbsttests die echten Einstellungen nicht anfassen.
