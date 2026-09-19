# Status

Stand: 2026-09-19

## Aktuell

- Schritt 1 bis 4 fertig und committet: Skeleton, Pen (Druck, Radierer über Toolbar / E / Seitentaste / invertiert, Glättung), Debug-Log, Hintergrundstile mit Linienfarbe, Undo/Redo, Zoom und Pan, Seitenmodell (Seiten / Endlos, Konvertierung, Auto-Seite, Seitenzähler).
- xUnit-Projekt Mitschreibprogramm.Tests steht (Konvertierung, .msp Round-Trip).
- Schritt 5 läuft: Dateiformat .msp und `SettingsService` fertig und getestet. Es fehlen: Datei-Befehle mit Dialogen, Settings im Fenster verdrahten.
- Selbsttest-Werkzeuge in tools/: pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1 (gemeinsamer Win32-Code in MspNative.cs).
- Checkpoint 1 bis 3 per Selbsttest bestanden (Details in testing.md).
- Wichtigster Befund: WPFs Standard-Stylus-Stack nimmt den synthetischen Pen als Stylus mit Druck an. Kein Pointer-Stack-Switch nötig.

## Nächster Schritt

- Schritt 5: Dateiformat .msp, Neu / Öffnen / Speichern / Speichern unter, Titelleiste mit `*`, Schließen-Dialog, Settings. Danach Checkpoint 4.

## Schrittplan

1. Projekt-Skeleton (fertig)
2. Pen, Radierer, Glättung, Debug-Log, tools/ (fertig, Checkpoint 1 bestanden)
3. Hintergrund, Zoom, Pan, Undo/Redo (fertig, Checkpoint 2 bestanden)
4. Seitenmodell Seiten / Endlos (fertig, Checkpoint 3 bestanden)
5. Datei, Settings, Schließen-Dialog (Checkpoint 4)
6. PDF-Export (Checkpoint 5)
7. Dark Mode, Shortcuts, Statusleiste
8. Publish, build.sh, README (Checkpoint 6)
9. Abschluss: features.md, status.md, testing.md "Offen mit echtem Pen"

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.
- app.manifest für PerMonitorV2 fehlt noch (kommt mit Schritt 8, siehe decisions.md).

## Bekannte Bugs

- keine offenen. Behoben: Hilfslinien verschwanden bandweise bei Zoom unter 100 % (fix 2026-09-19, siehe decisions.md).
