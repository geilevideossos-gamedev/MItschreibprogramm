# Testing

## Selbsttest ohne Pen (vier Schichten)

1. Synthetischer Pen: `tools/pen-sim.ps1` injiziert echte Pen-Eingaben (Position, Druck, Seitentaste, invertiert, Radierer) in das Fenster der laufenden App.
2. Debug-Log: App mit `MSP_DEBUG_LOG=<pfad>` starten. Pro fertigem Strich eine Zeile (Punkte, min/max Druck, Breite, Modus, Seite, Zoom), dazu Zeilen für Undo/Redo und für `window activated` / `window deactivated`. Zeigt, ob Eingaben als Stylus und nicht als Maus ankommen.
3. Screenshots: `tools/screenshot.ps1` speichert das App-Fenster als PNG nach tmp/. Bild anschauen.
4. xUnit: `dotnet test` für Models/Services.

Voraussetzungen: Laptop an, angemeldet, Bildschirm entsperrt, niemand benutzt währenddessen Maus oder Tastatur. Vor jedem Injektionslauf kurze Meldung im Chat. Die Skripte holen das Fenster selbst nach vorn und brechen ab, wenn der Zielpixel nicht zur App gehört oder ein fremdes Fenster in den Vordergrund kommt. `selftest.ps1` startet erst nach 15 s ohne Eingabe und gibt sonst mit Exit-Code 99 auf. Bei gesperrtem Bildschirm scheitert schon das Aktivieren des Fensters.

## Ablauf

- Alles auf einmal: `powershell -ExecutionPolicy Bypass -File tools/selftest.ps1` (Debug-Build) oder mit `-Exe dist/Mitschreibprogramm.exe`. Einzelne Checkpoints: `-Checkpoint "1,2"`.
- Ausgabe: PASS/FAIL je Prüfung, am Ende `SELFTEST OK` oder Fehlerzahl (Exit-Code). Screenshots und Log liegen in tmp/selftest/.
- Einzelschritte von Hand: `tools/app-control.ps1` (Fenster platzieren, Tasten, Maus, Controls per AutomationId), `tools/pen-sim.ps1`, `tools/screenshot.ps1`. Koordinaten sind WPF-Einheiten relativ zum Client-Bereich.
- Das Testfenster liegt bei 20,10 mit 1500 x 1000 physischen Pixeln. Bei 125 % Skalierung beginnt die Seite dann bei Client-Koordinate ca. 188,59.

## Checkpoints

| Nr | Inhalt | Ergebnis |
|----|--------|----------|
| 1 | Pen: synthetischer Strich mit steigendem Druck, Seitentaste als Radierer, invertierter Stift. Druckverlauf im Debug-Log, Screenshot | bestanden 2026-09-19, 7/7 |
| 2 | Hintergrund, Zoom, Pan, Undo/Redo: Screenshot pro Stil und Zoomstufe, Strich bei 200 % injizieren, Koordinaten im Debug-Log prüfen | bestanden 2026-09-19, 13/13 |
| 3 | Seitenmodell: xUnit für Konvertierung, Screenshot beider Modi, Auto-Seite per injiziertem Strich unten | bestanden 2026-09-19, 11/11, xUnit 7/7 |
| 4 | Datei: xUnit Round-Trip, Datei mit injizierten Strichen speichern, neu laden, Screenshot vergleichen | bestanden 2026-09-19, 18/18, xUnit 15/15 |
| 5 | PDF-Export: xUnit Seitenzahl, PDF in Bild wandeln (pdftoppm) und anschauen | bestanden 2026-09-19, 5/5, xUnit 22/22 |
| 6 | exe aus dist/ starten, Selbsttest komplett gegen die exe wiederholen | bestanden 2026-09-19 gegen die finale exe, 65/65 (Teil 1: 31/31, Teil 2: 34/34) |

### Ergebnis Checkpoint 1

- Druckrampe 100..1000 kommt als `device=stylus pmin=0.098 pmax=0.977` an, im Screenshot links dünn, rechts dick.
- Gehaltene Seitentaste (`barrel=True`) und invertierter Stift (`device=stylus-inverted`) radieren den gekreuzten Strich, danach zeichnet der Stift wieder.
- Taste E radiert, P zeichnet. Maus: `device=mouse`, Druck konstant 0,5. Checkbox Druck aus: `pressure=False`, Breite konstant.

### Ergebnis Checkpoint 2

- Screenshots Liniert blau / schwarz, Strichliert, Blanko korrekt, Linien scharf bei 100 % und 200 %.
- Ctrl+Z / Ctrl+Y laut Log `undo strokes=0`, `redo strokes=1`.
- 200 %: 300 px Bildschirm = 150 Seitenpixel. Zoom um den Zeiger hält den Punkt auf beiden Achsen (340.1,291.4 zu 340.2,291.3), sobald die Seite breiter als das Fenster ist, vorher nur vertikal (siehe decisions.md).
- Pan mit mittlerer Maustaste, Leertaste+Ziehen (zeichnet nicht), Shift+Mausrad und Mausrad verschieben die Ansicht wie erwartet.

### Ergebnis Checkpoint 3

- xUnit `PageModeConverterTests`: Offset pro Seite, Round-Trip punktgenau, Zuordnung nach erstem Punkt, leere Zwischenseiten, Inhalt rechts der A4-Breite.
- Strich bei Seiten-Y 1040 (untere 15 %) erzeugt Seite 2. Ctrl+Enter und "+ Seite" hängen an, Statusleiste zählt mit.
- Moduswechsel per Button, Ctrl+Z nimmt ihn zurück (alle Striche bleiben), Ctrl+Y stellt ihn wieder her.
- Endlos wächst nach rechts: nach einem Strich bei X 600..760 ist ein Strich bei X 900 möglich. Zurück im Seitenmodus: 5 Seiten (4 Zeilen plus eine Seite für den Inhalt rechts).
- Gefunden und behoben: Hilfslinien verschwanden bei Zoom unter 100 % bandweise.

### Ergebnis Checkpoint 4

- xUnit: .msp Round-Trip aller Felder, exakte JSON-Form, tolerantes Laden, neuere Version wird abgelehnt. Settings Round-Trip, Standardwerte bei fehlender oder kaputter Datei.
- Zwei injizierte Striche, Ctrl+S über den Windows-Dialog, Ctrl+N, Ctrl+O: Screenshot nach dem Laden weicht 0,019 % vom Screenshot vor dem Speichern ab. Koordinaten und Druck in der Datei passen zum Debug-Log.
- Titel: `Unbenannt`, `Unbenannt*`, `cp4.msp`, `cp4.msp*`. Alt+F4 mit Änderungen zeigt den Dialog, Abbrechen bleibt, Speichern speichert und beendet, Verwerfen beendet ohne zu speichern.
- settings.json nach dem Schließen: Farbe, Breite, Druck, Stil, Linienfarbe, Modus, letzter Ordner. Neustart stellt Fensterlage und alle Werte wieder her, ein neuer Strich nutzt sie.

### Ergebnis Checkpoint 5

- xUnit: Seitenzahl für Seiten- und Endlos-Dokumente (auch Inhalt rechts der A4-Breite), A4-Maße, leeres Dokument, Hilfslinien nur auf Wunsch, Punkt- und Konstantstriche, Druckverlauf auf geraden Zwei-Punkt-Strichen.
- Export über Ctrl+E, Export-Dialog und Windows-Dialog schreibt die Datei. Ohne Haken ist sie kleiner und das gerenderte Bild hat weniger Farbe (1,28 % statt 4,49 %).
- pdftoppm-Bild neben dem Screenshot: vier Farben, Druckverlauf dünn zu dick und dick zu dünn, Linien an derselben Stelle, 2 Seiten.
- Gefunden und behoben: gerader Druckstrich kam im PDF mit konstanter Breite an.

### Ergebnis Abschnitt 7 (Dark Mode, läuft als `-Checkpoint 7` mit)

- 11/11 am 2026-09-19. Pixelprobe am Bildschirm: schwarzer Strich hell 0, dunkel 765 (weiß), Seite 765 zu 129, Blau unverändert.
- Neuer Strich im Dark Mode erscheint weiß. Undo, Themenwechsel, Redo: der Strich kommt in der Farbe des aktuellen Themas zurück.
- Gespeicherte Datei enthält weiter `black`. PDF aus dem Dark Mode ist Schwarz auf Weiß. darkMode steht in settings.json, der Neustart kommt dunkel hoch.
- Gefunden und behoben: Toggle reagierte nur auf Click, nicht auf UI Automation.

### Ergebnis Checkpoint 6 (gegen dist/Mitschreibprogramm.exe)

- Teil 1, `-Checkpoint "1,2,3"`: 31/31 am 2026-09-19 gegen die finale exe (Checkpoint 1: 7/7, Checkpoint 2: 13/13, Checkpoint 3: 11/11), kein `window deactivated` im Log.
- Vorgeschichte: In drei früheren Läufen gegen die exe blieb je eine injizierte Geste wirkungslos (zweimal ein Pen-Strich nach einem Zoomwechsel, einmal das Ziehen mit der mittleren Maustaste), jedes Mal an anderer Stelle. Alle drei fielen in die Zeit, in der parallel Hintergrund-Agenten dauernd Prozesse starteten. Gegen Debug-Build und eine zweite Release-exe (74 Gesten, auch unter künstlicher CPU-Last) trat es nie auf, ohne Parallelbetrieb auch gegen die exe nicht. Die Ursache ist damit eingegrenzt, aber nicht bewiesen, deshalb steht der Punkt zusätzlich unter "Offen mit echtem Pen".
- Teil 2, `-Checkpoint "4,5,7"`: 34/34 am 2026-09-19 gegen dieselbe exe (Checkpoint 4: 18/18, Checkpoint 5: 5/5, Dark Mode: 11/11). Ein erster Anlauf war an einem Pfadfehler der aufgeteilten Testskripte gescheitert (behoben, kein App-Fehler).
- Der Lauf ist in zwei Teile geteilt, weil ein Werkzeugaufruf hier höchstens 10 Minuten dauern darf. Von Hand geht alles in einem: `tools/selftest.ps1 -Exe dist/Mitschreibprogramm.exe`.

## Manuelle Checkliste

Für einen schnellen Durchgang von Hand (Maus reicht), alles andere deckt `tools/selftest.ps1` ab:

- Starten, mit jeder der vier Farben schreiben, Breite über Regler, Presets und + / - ändern.
- E, Strich überfahren, P. Strg+Z mehrfach, Strg+Y.
- Strg+L dreimal, "Linien" umschalten, Strg+D hin und zurück.
- Strg+Mausrad, Strg+0, mittlere Maustaste ziehen, Leertaste + ziehen.
- Unten auf der Seite schreiben (neue Seite erscheint), Strg+Enter, Modus auf Endlos und zurück.
- Speichern, Neu, Öffnen. Etwas ändern, Fenster schließen: Dialog mit Speichern / Verwerfen / Abbrechen.
- PDF-Export mit und ohne Hintergrundlinien, PDF ansehen.
- Programm neu starten: Farbe, Breite, Stil, Dark Mode und Fensterlage sind wieder da.

## Offen mit echtem Pen

Nur mit dem Wacom Intuos Small prüfbar. features.md bleibt bis dahin auf `done`, danach `getestet`. Zum Prüfen die App mit `MSP_DEBUG_LOG=<pfad>` starten und die Logzeilen mitlesen.

- **Windows Ink im Wacom-Treiber:** Mit Haken "Windows Ink verwenden" muss das Log `device=stylus` und unterschiedliche `pmin` / `pmax` zeigen. Ohne Haken: `device=mouse`, konstante Breite, Seitentaste radiert nicht. Steht so in der README, ist aber nur mit der Simulation belegt.
- **Echte Seitentasten:** untere Taste mit Treiber-Belegung "Radieren" (erwartet `device=stylus-inverted mode=erase`) und mit "Rechtsklick" (erwartet `barrel=True mode=erase`). Radiert es nur, solange die Taste gehalten wird? Öffnet "Rechtsklick" irgendwo ein Kontextmenü oder stört die Hochstufung zur rechten Maustaste?
- **Seitentaste mitten im Strich:** WPF verwirft dann den laufenden Strich. Passiert das beim normalen Schreiben aus Versehen (Finger liegt auf der Taste)? Falls ja, Umschalten nur im Hover erlauben.
- **Druckkurve und Gefühl:** Faktor 0,25 bis 1,75 der Basisbreite. Fühlt sich dünn / mittel / dick richtig an, ist der Einsatzpunkt (leichtes Aufsetzen) brauchbar? Feinabstimmung ginge über die Druckkurve im Wacom-Treiber.
- **Glättung (FitToCurve):** verändert kleine Schrift beim Abheben sichtbar? Bei der Simulation (gerade Linien) nicht beurteilbar.
- **Latenz:** Strichbeginn ohne Verzögerung (Press-and-Hold ist für die Schreibfläche aus), nasse Tinte folgt dem Stift flüssig, auch bei 200 % Zoom und auf einer langen Endlos-Fläche.
- **ExpressKeys:** die vier Tasten mit Strg+Z, E, 1, Strg+Enter belegen. Kommen sie als Tastendruck an, auch während der Stift im Hover über der Seite ist?
- **Mapping der Fläche:** Tablett auf einen Bildschirm-Teilbereich mappen. Stimmen Stiftspitze und Strich weiter überein (auch bei 125 % Skalierung und auf einem zweiten Monitor mit anderer Skalierung, PerMonitorV2)?
- **Handballen / Hover:** Hover über der Toolbar und Wechsel zurück auf die Seite, Stift verlässt den Erfassungsbereich mit gedrückter Seitentaste (Radierer darf nicht hängen bleiben).
- **Strich direkt nach Zoomwechsel:** zoomen (Strg+Mausrad, Strg+Plus) und sofort schreiben. Geht dabei je ein Strich verloren? Im Selbsttest trat das nur auf, während parallel andere Prozesse Fenster öffneten (siehe Ergebnis Checkpoint 6).
- **Pan mit Stift:** Leertaste halten und mit dem Stift ziehen verschiebt die Ansicht, ohne zu zeichnen.
