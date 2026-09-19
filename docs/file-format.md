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
- Tolerant beim Öffnen: Punkte ohne Druck bekommen 0,5, Punkte mit weniger als zwei Zahlen und Striche ohne Punkte fallen weg, Breite und Druck werden in ihre Grenzen geholt.

## settings.json

Ort: %AppData%/Mitschreibprogramm/settings.json. (wird mit den Settings gefüllt)
