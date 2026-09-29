# Entscheidungen (Fortsetzung)

Fortsetzung von decisions.md, gleiche Regeln: chronologisch, je Eintrag Entscheidung, Begründung, verworfene Alternative.

## 2026-09-29 Hardware-Korrektur: One by Wacom CTL-672

- Das Tablet ist kein Intuos Small, sondern ein One by Wacom (Medium) CTL-672: aktive Fläche ca. 216 x 135 mm, 2048 Druckstufen, zwei Seitentasten, kein Tilt, keine ExpressKeys. Korrigiert in CLAUDE.md, pen-input.md, README, testing.md.
- Am Code ändert das nichts: WPF normiert den Druck auf 0..1, die Stufenzahl spielt keine Rolle. Die Shortcuts bleiben Einzeltasten.
- Der ExpressKeys-Abschnitt ist aus der README und aus "Offen mit echtem Pen" entfernt. mega-prompt-claude-code-start.md bleibt als ursprünglicher Auftrag unverändert, features.md F35 und F41 tragen einen Hinweis.

## 2026-09-29 Änderungszeit unter dem Heftnamen bleibt, relativ

- Daniel: anzeigen, kurz und relativ. Format `heute 10:04`, `gestern`, sonst `28.09.`, bei einem anderen Jahr `31.12.2025`. Rechnung in `Services/RelativeDate` (xUnit), Anzeige über `RelativeDateConverter`.
- Die Zeile wird beim Neuaufbau der Liste berechnet (jeder Wechsel, jedes Speichern mit Änderung). Bleibt die App über Mitternacht offen, steht bis dahin noch „heute“. Verworfen: eigener Timer nur dafür.

## 2026-09-29 Formerkennung

- Auslöser: Stillhalten am Strichende (Standard) oder Schalter „Formen“ / F (jeder Strich). Gemessen wird mit den Zeitstempeln der Eingabe-Ereignisse (`InputEventArgs.Timestamp`): wie lange lag die letzte Bewegung über 4 Einheiten vor dem Abheben? Positionen in Fenster-Einheiten, damit die Toleranz bei jedem Zoom gleich groß wirkt. Ersetzt wird beim Abheben. Verworfen: schon während des Haltens ersetzen, dafür müsste man in die laufende Strichsammlung von InkCanvas eingreifen.
- Methode (`Services/ShapeRecognizer`, rein rechnerisch, xUnit):
  1. Mindestgröße: die längere Seite des Rahmens muss 40 Bildschirm-Einheiten erreichen (ca. 10,6 mm bei 100 %). Buchstaben und Ziffern auf 8-mm-Linien sind kleiner, das ist der wichtigste Schutz der Handschrift.
  2. Linie: kein Punkt weiter als 6 % der Sehnenlänge von der Strecke Anfang–Ende (Abstand zur Strecke, nicht zur Geraden, damit Zurückfahren zählt). Verworfen: Verhältnis Sehne zu Pfadlänge, das schlägt schon bei Zittern an.
  3. Geschlossen: das Ende kommt bis auf 25 % der Rahmengröße an den Anfang zurück. Ein Überschießen darf höchstens 15 % des Umfangs lang sein und wird abgeschnitten; ein längerer Schwanz (Abstrich eines „a“, Spirale) heißt: keine Form.
  4. Ecken: Douglas-Peucker (Toleranz 5 % der Rahmendiagonale) auf beiden Hälften der Schleife, geteilt am vom Start entferntesten Punkt. Ecken näher als 12 % der Diagonale zählen einmal, Ecken mit weniger als 30° Richtungswechsel fallen weg (Start mitten auf einer Kante, abgerundete Ecke).
  5. 3 Ecken: Dreieck, wenn jeder Winkel mindestens 15° hat und der mittlere Abstand der Punkte zum Dreieck unter 4 % der Diagonale liegt. 4 Ecken: Rechteck, wenn jeder Winkel 90° ± 25° ist, das Viereck konvex ist und der Umriss so gut passt. Die Lage ergibt sich aus den über Kantenlängen gemittelten Kantenrichtungen (auf eine Vierteldrehung gefaltet), die Seiten aus den gemittelten Ecken.
  6. Sonst Ellipse: Achsen aus den zweiten Momenten der gleichmäßig nachabgetasteten Schleife, Radien aus der Ausdehnung entlang der Achsen. Mittlerer radialer Fehler höchstens 7 %, größter höchstens 20 %, Achsenverhältnis mindestens 0,2. Ab Achsenverhältnis 0,85 wird ein Kreis daraus.
  7. Linien, Rechtecke und Ellipsen, die weniger als 8° von den Achsen abweichen, werden gerade gestellt.
- Verworfen: Vorlagenvergleich ($1-Recognizer) oder ein Klassifikator. Mehr Code, Trainings- oder Vorlagendaten, und die Ablehnung von Handschrift wäre schwerer zu begründen als mit Größe, Geschlossenheit und Passfehler.
- Grenzen: eine sehr große, saubere „0“ oder ein großes „D“ kann als Ellipse durchgehen, nur Formen aus einem Strich werden erkannt, Vielecke mit mehr als vier Ecken nicht.
- Ergebnis ist ein normaler Strich mit dem Stift des Originals, Druck 0,5 an jedem Punkt (Faktor 1, also genau die eingestellte Breite) und `FitToCurve = false`. Ohne das Flag würde WPFs Bezier-Fit die Ecken runden, nach dem Laden und im PDF. Deshalb steht `fitToCurve` jetzt pro Strich in der .msp (fehlt es, gilt true; ältere Versionen des Programms ignorieren das Feld). Die Formatversion bleibt dafür 1.
- Undo: das Aufnehmen des Strichs und das Ersetzen sind zwei Schritte. Das erste Strg+Z holt den Freihand-Strich zurück, das zweite entfernt ihn.
- Der Schalter „Formen“ wird nicht in settings.json gemerkt, das stand nicht im Auftrag.

## 2026-09-29 Bilder

- Ein Bild ist ein `Image`-Element im InkCanvas der Seite (InkCanvas.Children). So wählt, verschiebt und skaliert die InkCanvas-Auswahl (Lasso, F69 ff.) Striche und Bilder mit denselben Bordmitteln, und Bilder liegen unter der Tinte. Verworfen: eigene Bild-Ebene unter dem InkCanvas, die Auswahl hätte Bilder dann selbst verwalten müssen.
- Zwischenablage: zuerst das Format „PNG“ (Snipping Tool, Browser, mit Transparenz), sonst `Clipboard.GetImage` ohne Alphakanal (Bgr32): Bitmaps von der Druck-Taste oder aus WinForms haben oft einen Alphakanal voller Nullen und kämen unsichtbar an. Gespeichert wird immer ein 8-Bit-PNG (verlustfrei neu kodiert), weil PDFsharp 16-Bit-PNGs nicht einbetten kann (`ImageImporterPng`, Recherche 2026-09-29). Ein Zugriffsfehler (ein anderes Programm hält die Zwischenablage) heißt wie „kein Bild“: es passiert nichts.
- Größe: die Bildgröße in geräteunabhängigen Einheiten (Pixel bei 96 DPI), verkleinert auf höchstens 80 % der Seitenbreite. Zusätzlich höchstens 80 % der Seitenhöhe: ein sehr hoher Screenshot würde sonst im Seitenmodus am Blattende abgeschnitten. Lage: Mitte des sichtbaren Bereichs auf der Seite in der Fenstermitte, in die Seite geschoben. Auf der Endlos-Fläche wächst die Fläche mit.
- Format: `images[]` pro Seite, Formatversion 2. Version-1-Dateien laden unverändert und werden beim nächsten Speichern als 2 geschrieben. Ältere Programmversionen lehnen Version 2 ab, statt Bilder beim Speichern still zu verlieren.
- PDF: PDFsharp 6.2.4 (Core) liest PNG über `XImage.FromStream`, der Test prüft das Bild-XObject im erzeugten PDF. Kein zusätzliches Paket.
- Bilder werden mit `BitmapScalingMode.HighQuality` gezeichnet, damit verkleinerte Screenshots lesbar bleiben.
