# Status

Stand: 2026-09-19

## Aktuell

- Alle Schritte 1 bis 9 fertig, alle 45 Features in features.md auf `done`.
- Änderung nach dem Abschluss (2026-09-19): Seitenstil Strichliert wurde durch Kariert (5-mm-Gitter) ersetzt, siehe decisions.md. xUnit und gerendertes PDF sind geprüft, die Bildschirm-Screenshots laufen mit dem nächsten Selbsttest.
- `dotnet build`: 0 Warnings, 0 Errors. `dotnet test`: 28 Tests grün (Konvertierung, .msp Round-Trip und tolerantes Laden, PDF-Export, Settings).
- Checkpoint 1 bis 6 per Selbsttest bestanden, Checkpoint 6 gegen die finale dist/Mitschreibprogramm.exe mit 65/65 (Details in testing.md).
- `./build.sh` erzeugt dist/Mitschreibprogramm.exe (141 MB, einzige Datei). README.md ist geschrieben.
- Unabhängiger Code-Review und ein Abschluss-Audit (README, docs, Feature-Abdeckung, Projektregeln) sind gelaufen, alle bestätigten Funde behoben (decisions.md).
- Selbsttest-Werkzeuge in tools/: pen-sim.ps1, screenshot.ps1, app-control.ps1, selftest.ps1 (gemeinsamer Win32-Code in MspNative.*.cs, Checkpoints in tools/selftest/).
- Nichts gepusht. origin/main steht unverändert auf dem GitHub-Commit `Initial commit`.

## Nächster Schritt

- Daniel testet mit dem echten Wacom Intuos nach testing.md, Abschnitt "Offen mit echtem Pen". Was dort bestätigt ist, wandert in features.md von `done` auf `getestet`.
- Push nur auf ausdrückliche Anweisung: `ALLOW_PUSH=1 git push origin main`.

## Schrittplan

1. Projekt-Skeleton (fertig)
2. Pen, Radierer, Glättung, Debug-Log, tools/ (fertig, Checkpoint 1 bestanden)
3. Hintergrund, Zoom, Pan, Undo/Redo (fertig, Checkpoint 2 bestanden)
4. Seitenmodell Seiten / Endlos (fertig, Checkpoint 3 bestanden)
5. Datei, Settings, Schließen-Dialog (fertig, Checkpoint 4 bestanden)
6. PDF-Export (fertig, Checkpoint 5 bestanden)
7. Dark Mode, Shortcuts, Statusleiste (fertig)
8. Publish, build.sh, README (fertig, Checkpoint 6 bestanden)
9. Abschluss: features.md, status.md, testing.md "Offen mit echtem Pen" (fertig)

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.

- Selbsttest: In drei frühen Läufen gegen die exe blieb je eine injizierte Geste wirkungslos, nur während parallel Hintergrundprozesse liefen. Ursache eingegrenzt (fremdes Fenster deaktiviert die App), nicht bewiesen. Ohne Parallelbetrieb 65/65. Steht auch unter "Offen mit echtem Pen".

## Bekannte Bugs

- keine offenen.
- Behoben am 2026-09-19: Hilfslinien verschwanden bandweise bei Zoom unter 100 %. Gerader Druckstrich kam im PDF mit konstanter Breite an. Dark-Mode-Toggle reagierte nicht auf UI Automation. Kaputte .msp (null-Listen, Riesenkoordinaten, Enum als Zahl) konnte abstürzen. Gehaltene Taste ließ Umschalter (E, Strg+D, Strg+L) flackern.
