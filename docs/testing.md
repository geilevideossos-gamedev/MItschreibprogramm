# Testing

## Selbsttest ohne Pen (vier Schichten)

1. Synthetischer Pen: `tools/pen-sim.ps1` injiziert echte Pen-Eingaben (Position, Druck, Seitentaste, invertiert, Radierer) in das Fenster der laufenden App.
2. Debug-Log: App mit `MSP_DEBUG_LOG=<pfad>` starten. Pro fertigem Strich eine Zeile (Punkte, min/max Druck, Breite, Modus, Seite, Zoom), dazu Zeilen für Undo/Redo und für `window activated` / `window deactivated`. Zeigt, ob Eingaben als Stylus und nicht als Maus ankommen.
3. Screenshots: `tools/screenshot.ps1` speichert das App-Fenster als PNG nach tmp/. Bild anschauen.
4. xUnit: `dotnet test` für Models/Services.

Voraussetzungen: Laptop an, angemeldet, Bildschirm entsperrt, niemand benutzt währenddessen Maus oder Tastatur. Vor jedem Injektionslauf kurze Meldung im Chat. Die Skripte holen das Fenster selbst nach vorn und brechen ab, wenn der Zielpixel nicht zur App gehört oder ein fremdes Fenster in den Vordergrund kommt. `selftest.ps1` startet erst nach 15 s ohne Eingabe und gibt sonst mit Exit-Code 99 auf. Läuft schon eine Instanz der App (zum Beispiel Daniels), startet er gar nicht (Exit-Code 98), weil er jede Instanz beenden würde. Bei gesperrtem Bildschirm scheitert schon das Aktivieren des Fensters.

## Ablauf

- Alles auf einmal: `powershell -ExecutionPolicy Bypass -File tools/selftest.ps1` (Debug-Build) oder mit `-Exe dist/Mitschreibprogramm.exe`. Einzelne Checkpoints: `-Checkpoint "1,2"`.
- Ausgabe: PASS/FAIL je Prüfung, am Ende `SELFTEST OK` oder Fehlerzahl (Exit-Code). Screenshots und Log liegen in tmp/selftest/.
- Einzelschritte von Hand: `tools/app-control.ps1` (Fenster platzieren, Tasten, Maus, Controls per AutomationId), `tools/pen-sim.ps1`, `tools/screenshot.ps1`. Koordinaten sind WPF-Einheiten relativ zum Client-Bereich.
- Das Testfenster liegt bei 20,10 mit 1500 x 1000 physischen Pixeln. Bei 125 % Skalierung beginnt die Seite dann bei Client-Koordinate ca. 188,59. `Start-App` blendet die Seitenleiste dafür über settings.json aus; Checkpoint 4 startet mit `-Panel`, dann liegt die Seite 110 Einheiten weiter rechts und die Striche gehen über `Get-PageOrigin`.
- Jeder Start ohne `-KeepSettings` beginnt mit leerer Bibliothek (tmp/selftest/notes/ wird gelöscht). Für die Seitenleiste kann app-control.ps1 Listeneinträge per id anklicken (`-Click <id>`, ids aus index.json), doppelt klicken (`-DoubleClick`), rechts klicken (`-RightClick`, Kontextmenü, Menüpunkte danach per `-Click RenameMenuItem` usw.), Listen lesen (`-Items NotebookList`), Sichtbarkeit prüfen (`-Exists`, gibt True/False als Text) und den Dateinamen im Windows-Dialog lesen (`-Value 1001`, per WM_GETTEXT, weil UI Automation das Feld nicht zeigt; danach die Auswahl mit Strg+A erneuern, bevor ein Pfad getippt wird).
- Ein injizierter Strich kommt hin und wieder nicht an (siehe Checkpoint 6). `Pen` wiederholt ihn dann einmal, meldet `RETRY` und zählt ihn in der Schlusszeile mit. Die Skripte bleiben reines ASCII, weil Windows PowerShell 5.1 Dateien ohne BOM in der ANSI-Codepage liest.

## Checkpoints

| Nr | Inhalt | Ergebnis |
|----|--------|----------|
| 1 | Pen: synthetischer Strich mit steigendem Druck, Seitentaste als Radierer, invertierter Stift. Druckverlauf im Debug-Log, Screenshot | bestanden 2026-09-19, 7/7 |
| 2 | Hintergrund, Zoom, Pan, Undo/Redo: Screenshot pro Stil und Zoomstufe, Strich bei 200 % injizieren, Koordinaten im Debug-Log prüfen | bestanden 2026-09-19, 13/13 |
| 3 | Seitenmodell: xUnit für Konvertierung, Screenshot beider Modi, Auto-Seite per injiziertem Strich unten | bestanden 2026-09-19, 11/11, xUnit 7/7 |
| 4 | Hefte-Bibliothek (seit 2026-09-28, vorher Datei-Befehle): mehrere Hefte anlegen, Striche injizieren, umbenennen (Doppelklick und Kontextmenü), wechseln, Sortierung, .msp-Export und -Import, PDF aus dem Kontextmenü mit vorgeschlagenem Namen, Löschen mit Rückfrage, Strg+B, schließen und neu starten: Hefte, Striche und Scrollposition wieder da, Screenshot der Seitenleiste. xUnit für Index und Autosave | bestanden 2026-09-28, 31/31, xUnit 55/55 (alte Fassung 2026-09-19, 18/18) |
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

### Ergebnis Checkpoint 4 (Hefte-Bibliothek, 2026-09-28)

- 31/31 gegen den Debug-Build. Erster Start legt „Unbenannt“ an (Titel, Statusleiste, index.json, notes/<id>.msp). Ctrl+N legt ein zweites Heft an, das erste liegt danach mit beiden injizierten Strichen auf der Platte. Doppelklick öffnet den Umbenennen-Dialog, danach heißt es „Mathe“.
- Nach zwei neuen Seiten und fünf Rasten Mausrad steht Mathe auf „Seite 3 von 3“. Klick auf das erste Heft öffnet es (Mathe gespeichert, 3 Seiten), die Liste zeigt Mathe oben. Zurück in Mathe liegt der Seitenursprung wieder bei Y −179 wie beim Verlassen, nach Alt+F4 und Neustart ebenso (index.json: scrollY gemerkt, lastOpen = Mathe).
- Ctrl+Umschalt+S schlägt „Unbenannt.msp“ vor und schreibt die Datei mit 2 Strichen, Ctrl+O kopiert sie als Heft „cp4-export“ in die Bibliothek. „+ Neues Heft“ legt ein viertes an. Rechtsklick öffnet das Menü ohne Heftwechsel, Löschen fragt nach (Abbrechen ändert nichts), Löschen entfernt Datei und Eintrag. Menü-Umbenennen eines fremden Hefts lässt das offene offen, Menü-PDF schlägt „Physik.pdf“ vor und liefert 3 Seiten. Strg+B blendet aus und ein. Löschen des offenen Hefts öffnet das nächste.
- Screenshots: cp4-panel.png, cp4-panel-hidden.png, cp4-panel-restart.png, cp4-rename-dialog.png, cp4-delete-dialog.png, dazu cp7-dark-panel.png im Dark Mode.
- Vorläufe: einmal ging der erste injizierte Strich verloren (kein Log, Ursprung NaN, Lauf abgebrochen), einmal der zweite. Seither wiederholt `Pen` einen verlorenen Strich einmal. Zwei Skriptfehler aus dem Code-Review vorab behoben (Umlaut im Skript, Boolean gegen "False").
- Checkpoints 1, 2, 3 (31/31) und 5, 7 (19/19) laufen mit ausgeblendeter Seitenleiste weiter durch.

### Ergebnis Checkpoint 4 (Datei-Befehle, Fassung bis 2026-09-28)

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
- 2026-09-28: 13/13 mit der Hefte-Bibliothek, Ctrl+S schreibt still in notes/, die Seitenleiste ist im Dark Mode korrekt eingefärbt (cp7-dark-panel.png).

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
- Zweites Heft anlegen, im ersten etwas schreiben, hin- und herwechseln (landet an derselben Stelle), umbenennen, Rechtsklick-Menü, Strg+B. Fenster schließen: keine Nachfrage, nach dem Neustart ist alles da.
- Exportieren, Importieren: die Datei kommt als neues Heft herein.
- PDF-Export mit und ohne Hintergrundlinien, PDF ansehen.
- Programm neu starten: Farbe, Breite, Stil, Dark Mode und Fensterlage sind wieder da.

## Offen mit echtem Pen

Nur mit dem One by Wacom (CTL-672) prüfbar. features.md bleibt bis dahin auf `done`, danach `getestet`. Zum Prüfen die App mit `MSP_DEBUG_LOG=<pfad>` starten und die Logzeilen mitlesen. Achtung: mit gesetzter Variable benutzt die App auch eine eigene Hefte-Bibliothek neben der Logdatei (`<Logordner>/notes/`), die Seitenleiste startet also leer und die echten Hefte in %AppData% bleiben unberührt. Dafür einen Pfad außerhalb von tmp/selftest/ nehmen (zum Beispiel tmp/pen/debug.log), weil der Selbsttest tmp/selftest/notes/ löscht.

- **Windows Ink im Wacom-Treiber:** Mit Haken "Windows Ink verwenden" muss das Log `device=stylus` und unterschiedliche `pmin` / `pmax` zeigen. Ohne Haken: `device=mouse`, konstante Breite, Seitentaste radiert nicht. Steht so in der README, ist aber nur mit der Simulation belegt.
- **Echte Seitentasten:** untere Taste mit Treiber-Belegung "Radieren" (erwartet `device=stylus-inverted mode=erase`), obere mit "Rechtsklick" (erwartet eine Lasso-Auswahl, Logzeile `selection page=… strokes=…`). Radiert es nur, solange die untere Taste gehalten wird? Öffnet "Rechtsklick" irgendwo ein Kontextmenü oder stört die Hochstufung zur rechten Maustaste? Kommt die obere Taste bei der One by Wacom wirklich als Barrel an und die untere als invertiert?
- **Lasso mit echter oberer Taste:** Taste halten, Kreis um Striche ziehen, Taste loslassen, Auswahl in der Mitte ziehen, an einer Ecke skalieren, Entf, Strg+Z. Fühlt sich das Loslassen vor dem Ziehen natürlich an? Bleibt die Auswahl beim Abheben des Stifts (Stift verlässt den Erfassungsbereich) erhalten?
- **Seitentaste mitten im Strich:** WPF verwirft dann den laufenden Strich. Passiert das beim normalen Schreiben aus Versehen (Finger liegt auf der Taste)? Falls ja, Umschalten nur im Hover erlauben.
- **Druckkurve und Gefühl:** Faktor 0,25 bis 1,75 der Basisbreite. Fühlt sich dünn / mittel / dick richtig an, ist der Einsatzpunkt (leichtes Aufsetzen) brauchbar? Feinabstimmung ginge über die Druckkurve im Wacom-Treiber.
- **Glättung (FitToCurve):** verändert kleine Schrift beim Abheben sichtbar? Bei der Simulation (gerade Linien) nicht beurteilbar.
- **Latenz:** Strichbeginn ohne Verzögerung (Press-and-Hold ist für die Schreibfläche aus), nasse Tinte folgt dem Stift flüssig, auch bei 200 % Zoom und auf einer langen Endlos-Fläche.
- **Mapping der Fläche:** Tablett auf einen Bildschirm-Teilbereich mappen. Stimmen Stiftspitze und Strich weiter überein (auch bei 125 % Skalierung und auf einem zweiten Monitor mit anderer Skalierung, PerMonitorV2)?
- **Handballen / Hover:** Hover über der Toolbar und Wechsel zurück auf die Seite, Stift verlässt den Erfassungsbereich mit gedrückter Seitentaste (das Lasso darf nicht hängen bleiben).
- **Strich direkt nach Zoomwechsel:** zoomen (Strg+Mausrad, Strg+Plus) und sofort schreiben. Geht dabei je ein Strich verloren? Im Selbsttest trat das nur auf, während parallel andere Prozesse Fenster öffneten (siehe Ergebnis Checkpoint 6).
- **Pan mit Stift:** Leertaste halten und mit dem Stift ziehen verschiebt die Ansicht, ohne zu zeichnen.
- **Formen durch Stillhalten:** Fühlt sich eine halbe Sekunde richtig an (`AppConstants.ShapeHoldMilliseconds`)? Reicht die Ruhetoleranz von 4 Einheiten für eine echte, leicht zitternde Hand (`ShapeHoldTolerance`), ohne dass beim normalen Schreiben aus Versehen eine Form entsteht (Log-Zeile `shape trigger=hold`)? Werden echt gezeichnete Kreise, Rechtecke und Dreiecke erkannt, und bleibt Handschrift mit eingeschaltetem „Formen“ unverändert?
