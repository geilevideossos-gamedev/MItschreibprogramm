# Testing

## Selbsttest ohne Pen (vier Schichten)

1. Synthetischer Pen: `tools/pen-sim.ps1` injiziert echte Pen-Eingaben (Position, Druck, Seitentaste, invertiert, Radierer) in das Fenster der laufenden App.
2. Debug-Log: App mit `MSP_DEBUG_LOG=<pfad>` starten. Pro fertigem Strich eine Zeile (Punkte, min/max Druck, Breite, Modus, Seite, Zoom), dazu Zeilen für Undo/Redo. Zeigt, ob Eingaben als Stylus und nicht als Maus ankommen.
3. Screenshots: `tools/screenshot.ps1` speichert das App-Fenster als PNG nach tmp/. Bild anschauen.
4. xUnit: `dotnet test` für Models/Services.

Voraussetzungen: Laptop an, angemeldet, Bildschirm entsperrt, niemand benutzt währenddessen Maus oder Tastatur. Vor jedem Injektionslauf kurze Meldung im Chat. Die Skripte holen das Fenster selbst nach vorn und brechen ab, wenn der Zielpixel nicht zur App gehört.

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
| 3 | Seitenmodell: xUnit für Konvertierung, Screenshot beider Modi, Auto-Seite per injiziertem Strich unten | offen |
| 4 | Datei: xUnit Round-Trip, Datei mit injizierten Strichen speichern, neu laden, Screenshot vergleichen | offen |
| 5 | PDF-Export: xUnit Seitenzahl, PDF in Bild wandeln (pdftoppm) und anschauen | offen |
| 6 | exe aus dist/ starten, Selbsttest komplett gegen die exe wiederholen | offen |

### Ergebnis Checkpoint 1

- Druckrampe 100..1000 kommt als `device=stylus pmin=0.098 pmax=0.977` an, im Screenshot links dünn, rechts dick.
- Gehaltene Seitentaste (`barrel=True`) und invertierter Stift (`device=stylus-inverted`) radieren den gekreuzten Strich, danach zeichnet der Stift wieder.
- Taste E radiert, P zeichnet. Maus: `device=mouse`, Druck konstant 0,5. Checkbox Druck aus: `pressure=False`, Breite konstant.

### Ergebnis Checkpoint 2

- Screenshots Liniert blau / schwarz, Strichliert, Blanko korrekt, Linien scharf bei 100 % und 200 %.
- Ctrl+Z / Ctrl+Y laut Log `undo strokes=0`, `redo strokes=1`.
- 200 %: 300 px Bildschirm = 150 Seitenpixel. Zoom um den Zeiger hält den Punkt auf beiden Achsen (340.1,291.4 zu 340.2,291.3), sobald die Seite breiter als das Fenster ist, vorher nur vertikal (siehe decisions.md).
- Pan mit mittlerer Maustaste, Leertaste+Ziehen (zeichnet nicht), Shift+Mausrad und Mausrad verschieben die Ansicht wie erwartet.

## Manuelle Checkliste

(wird mit den Features ergänzt)

## Offen mit echtem Pen

- "Windows Ink verwenden" im Wacom-Treiber: kommt der Druck an (Log `device=stylus`, pmin ungleich pmax)?
- Untere Seitentaste gehalten radiert, mit Treiber-Belegung "Rechtsklick" (Barrel) und mit "Radieren" (invertiert).
- Seitentaste mitten im Strich gedrückt: der laufende Strich wird verworfen (WPF-Verhalten). Stört das beim Schreiben?
- Strichbeginn ohne Verzögerung (Press-and-Hold ist für die Schreibfläche abgeschaltet).
