# Status

Stand: 2026-09-19

## Aktuell

- Schritt 1 bis 4 fertig und committet: Skeleton, Pen (Druck, Radierer über Toolbar / E / Seitentaste / invertiert, Glättung), Debug-Log, Hintergrundstile mit Linienfarbe, Undo/Redo, Zoom und Pan, Seitenmodell (Seiten / Endlos, Konvertierung, Auto-Seite, Seitenzähler).
- xUnit-Projekt Mitschreibprogramm.Tests steht (Konvertierung, .msp Round-Trip).
- Schritt 7 fertig: Dark Mode (Button, Ctrl+D, gespeichert), alle Shortcuts, Statusleiste mit Zoom, Seite und Datei.
- Schritt 6 fertig: PDF-Export mit PDFsharp 6.2.4 über Ctrl+E und Button.
- Schritt 5 fertig: Dateiformat .msp, Datei-Befehle mit Dialogen, Titel mit `*`, Settings (laden, anwenden, beim Schließen speichern, Fensterlage). Shortcuts 1-4 und Plus / Minus sind ebenfalls drin.
- Selbsttest-Werkzeuge in tools/: pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1 (gemeinsamer Win32-Code in MspNative.cs).
- Checkpoint 1 bis 5 per Selbsttest bestanden (Details in testing.md).
- Wichtigster Befund: WPFs Standard-Stylus-Stack nimmt den synthetischen Pen als Stylus mit Druck an. Kein Pointer-Stack-Switch nötig.

## Nächster Schritt

- Schritt 8: app.manifest (PerMonitorV2), Publish nach dist/, build.sh, docs/build.md, README. Danach Checkpoint 6: Selbsttest komplett gegen dist/Mitschreibprogramm.exe.

## Schrittplan

1. Projekt-Skeleton (fertig)
2. Pen, Radierer, Glättung, Debug-Log, tools/ (fertig, Checkpoint 1 bestanden)
3. Hintergrund, Zoom, Pan, Undo/Redo (fertig, Checkpoint 2 bestanden)
4. Seitenmodell Seiten / Endlos (fertig, Checkpoint 3 bestanden)
5. Datei, Settings, Schließen-Dialog (fertig, Checkpoint 4 bestanden)
6. PDF-Export (fertig, Checkpoint 5 bestanden)
7. Dark Mode, Shortcuts, Statusleiste (fertig)
8. Publish, build.sh, README (Checkpoint 6)
9. Abschluss: features.md, status.md, testing.md "Offen mit echtem Pen"

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.
- app.manifest für PerMonitorV2 fehlt noch (kommt mit Schritt 8, siehe decisions.md).

## Bekannte Bugs

- keine offenen. Behoben: Hilfslinien verschwanden bandweise bei Zoom unter 100 % (fix 2026-09-19, siehe decisions.md).
