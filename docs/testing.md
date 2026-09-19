# Testing

## Selbsttest ohne Pen (vier Schichten)

1. Synthetischer Pen: `tools/pen-sim.ps1` injiziert echte Pen-Eingaben (Position, Druck, Seitentaste, invertiert, Radierer) in das Fenster der laufenden App.
2. Debug-Log: App mit `MSP_DEBUG_LOG=<pfad>` starten. Pro fertigem Strich eine Zeile (Punkte, min/max Druck, Breite, Modus, Seite, Zoom). Zeigt, ob Eingaben als Stylus und nicht als Maus ankommen.
3. Screenshots: `tools/screenshot.ps1` speichert das App-Fenster als PNG nach tmp/. Bild anschauen.
4. xUnit: `dotnet test` für Models/Services.

Voraussetzungen: Laptop an, angemeldet, Bildschirm entsperrt, App-Fenster im Vordergrund, niemand benutzt währenddessen die Maus. Vor jedem Injektionslauf kurze Meldung im Chat.

## Checkpoints

| Nr | Inhalt | Ergebnis |
|----|--------|----------|
| 1 | Pen: synthetischer Strich mit steigendem Druck, Seitentaste als Radierer, invertierter Stift. Druckverlauf im Debug-Log, Screenshot | offen |
| 2 | Hintergrund, Zoom, Pan, Undo/Redo: Screenshot pro Stil und Zoomstufe, Strich bei 200 % injizieren, Koordinaten im Debug-Log prüfen | offen |
| 3 | Seitenmodell: xUnit für Konvertierung, Screenshot beider Modi, Auto-Seite per injiziertem Strich unten | offen |
| 4 | Datei: xUnit Round-Trip, Datei mit injizierten Strichen speichern, neu laden, Screenshot vergleichen | offen |
| 5 | PDF-Export: xUnit Seitenzahl, PDF in Bild wandeln (pdftoppm) und anschauen | offen |
| 6 | exe aus dist/ starten, Selbsttest komplett gegen die exe wiederholen | offen |

## Manuelle Checkliste

(wird mit den Features ergänzt)

## Offen mit echtem Pen

(alles, was nur mit dem Wacom Intuos prüfbar ist; wird laufend ergänzt)
