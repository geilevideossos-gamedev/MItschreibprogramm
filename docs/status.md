# Status

Stand: 2026-09-19

## Aktuell

- Schritt 1 bis 3 fertig und committet: Skeleton, Pen (Druck, Radierer über Toolbar / E / Seitentaste / invertiert, Glättung), Debug-Log, Hintergrundstile mit Linienfarbe, Undo/Redo, Zoom und Pan.
- Selbsttest-Werkzeuge in tools/: pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1 (gemeinsamer Win32-Code in MspNative.cs).
- Checkpoint 1 und 2 per Selbsttest bestanden (Details in testing.md).
- Wichtigster Befund: WPFs Standard-Stylus-Stack nimmt den synthetischen Pen als Stylus mit Druck an. Kein Pointer-Stack-Switch nötig.

## Nächster Schritt

- Schritt 4: Seitenmodell Seiten / Endlos, Umschalten, neue Seite, Auto-Seite, Seitenzähler. Danach Checkpoint 3 (xUnit für Konvertierung, Screenshots, Auto-Seite per injiziertem Strich).

## Schrittplan

1. Projekt-Skeleton (fertig)
2. Pen, Radierer, Glättung, Debug-Log, tools/ (fertig, Checkpoint 1 bestanden)
3. Hintergrund, Zoom, Pan, Undo/Redo (fertig, Checkpoint 2 bestanden)
4. Seitenmodell Seiten / Endlos (Checkpoint 3)
5. Datei, Settings, Schließen-Dialog (Checkpoint 4)
6. PDF-Export (Checkpoint 5)
7. Dark Mode, Shortcuts, Statusleiste
8. Publish, build.sh, README (Checkpoint 6)
9. Abschluss: features.md, status.md, testing.md "Offen mit echtem Pen"

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.
- app.manifest für PerMonitorV2 fehlt noch (kommt mit Schritt 8, siehe decisions.md).

## Bekannte Bugs

- keine
