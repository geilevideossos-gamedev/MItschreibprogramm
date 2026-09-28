# Status

Stand: 2026-09-28

## Aktuell

- Neuer Auftrag vom 2026-09-28: Hefte-Bibliothek mit Seitenleiste (F46 bis F55 in features.md). Implementiert, `dotnet build` 0 Warnings, `dotnet test` 52 Tests grün (14 neue für `NotebookLibrary` und `LibraryLock`: Index laden/speichern, Abgleich mit dem Ordner, Autosave nur nach Änderung, Fehlerfälle, Sperre). Ein Design-Review mit fünf Blickwinkeln ist eingearbeitet (decisions.md „Nach dem Design-Review“). Der Selbsttest (Checkpoint 4 neu geschrieben, Checkpoint 7 angepasst) steht noch aus, deshalb `wip`.
- Was sich für den Benutzer ändert: kein Datei-Dialog mehr im Alltag. Hefte liegen in %AppData%/Mitschreibprogramm/notes/, werden beim Wechsel, beim Schließen und alle 60 s automatisch gespeichert, die Seitenleiste (Strg+B) listet sie nach zuletzt bearbeitet. Öffnen/Speichern unter heißen jetzt Importieren/Exportieren. Kein Schließen-Dialog mehr, kein Stern im Titel.
- Vorher (2026-09-19): alle 45 Features `done`, Checkpoint 1 bis 6 bestanden, Checkpoint 6 gegen dist/Mitschreibprogramm.exe mit 65/65. README geschrieben. Seitenstil Strichliert durch Kariert ersetzt.
- `./build.sh` erzeugt dist/Mitschreibprogramm.exe (141 MB, einzige Datei). Die exe in dist/ ist noch der Stand vom 2026-09-19 ohne Bibliothek.
- Selbsttest-Werkzeuge in tools/: pen-sim.ps1, screenshot.ps1, app-control.ps1 (neu: -RightClick, -DoubleClick, -Exists, -Items, -Value), selftest.ps1 (startet nicht, solange eine Instanz der App läuft), Checkpoints in tools/selftest/.
- Nichts gepusht. origin/main steht unverändert auf dem GitHub-Commit `Initial commit`.

## Nächster Schritt

- Selbsttest laufen lassen: `powershell -ExecutionPolicy Bypass -File tools/selftest.ps1 -Checkpoint 4` und danach `-Checkpoint "1,2,3,5,7"` (Seitenleiste wird für die alten Checkpoints ausgeblendet). Ergebnis in testing.md, F46 bis F55 auf `done`.
- `./build.sh` für eine neue dist/Mitschreibprogramm.exe, solange keine alte Instanz aus dist/ läuft (die Datei ist sonst gesperrt).
- Daniel testet mit dem echten Wacom Intuos nach testing.md, Abschnitt "Offen mit echtem Pen". Was dort bestätigt ist, wandert in features.md von `done` auf `getestet`.
- Push nur auf ausdrückliche Anweisung: `ALLOW_PUSH=1 git push origin main`.

## Schrittplan

1. Projekt-Skeleton (fertig)
2. Pen, Radierer, Glättung, Debug-Log, tools/ (fertig, Checkpoint 1 bestanden)
3. Hintergrund, Zoom, Pan, Undo/Redo (fertig, Checkpoint 2 bestanden)
4. Seitenmodell Seiten / Endlos (fertig, Checkpoint 3 bestanden)
5. Datei, Settings, Schließen-Dialog (fertig, Checkpoint 4 bestanden; seit 2026-09-28 durch die Hefte-Bibliothek ersetzt)
6. PDF-Export (fertig, Checkpoint 5 bestanden)
7. Dark Mode, Shortcuts, Statusleiste (fertig)
8. Publish, build.sh, README (fertig, Checkpoint 6 bestanden)
9. Abschluss: features.md, status.md, testing.md "Offen mit echtem Pen" (fertig)
10. Hefte-Bibliothek: Library + Tests (fertig), Session, Seitenleiste, Dialoge, Import/Export (fertig), Selbsttest Checkpoint 4 (offen), docs (laufend)

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.
- Selbsttest: In drei frühen Läufen gegen die exe blieb je eine injizierte Geste wirkungslos, nur während parallel Hintergrundprozesse liefen. Ursache eingegrenzt (fremdes Fenster deaktiviert die App), nicht bewiesen. Ohne Parallelbetrieb 65/65. Steht auch unter "Offen mit echtem Pen".
- Zweite Instanz wird über notes/.lock abgewiesen (`LibraryLock`). Nicht abgedeckt: zwei Rechner, die denselben Ordner über einen Sync-Dienst teilen.

## Bekannte Bugs

- keine offenen.
- Behoben am 2026-09-19: Hilfslinien verschwanden bandweise bei Zoom unter 100 %. Gerader Druckstrich kam im PDF mit konstanter Breite an. Dark-Mode-Toggle reagierte nicht auf UI Automation. Kaputte .msp (null-Listen, Riesenkoordinaten, Enum als Zahl) konnte abstürzen. Gehaltene Taste ließ Umschalter (E, Strg+D, Strg+L) flackern.
