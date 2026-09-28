# Status

Stand: 2026-09-28

## Aktuell

- Hefte-Bibliothek mit Seitenleiste (Auftrag vom 2026-09-28, F46 bis F55) ist fertig und per Selbsttest geprüft: Checkpoint 4 (neu) 31/31, Checkpoint 1, 2, 3 31/31, Checkpoint 5 und 7 19/19, alle gegen den Debug-Build vom selben Stand. `dotnet build` 0 Warnings, `dotnet test` 55 Tests grün. F46 bis F55 stehen auf `done`.
- Was sich für den Benutzer ändert: kein Datei-Dialog mehr im Alltag. Hefte liegen in %AppData%/Mitschreibprogramm/notes/ (eine .msp je Heft plus index.json), werden beim Wechsel, beim Schließen und alle 60 s automatisch gespeichert, die Seitenleiste (Strg+B) listet sie nach zuletzt bearbeitet. Öffnen/Speichern unter heißen jetzt Importieren/MSP-Export. Kein Schließen-Dialog, kein Stern im Titel, Löschen in den Papierkorb, eine zweite Instanz beendet sich mit Meldung.
- Zwei Reviews (Design, fünf Blickwinkel; Code, fünf Blickwinkel mit je zwei Gegenprüfern) sind eingearbeitet, siehe decisions.md „Nach dem Design-Review“ und „Nach dem Code-Review“.
- `./build.sh` hat dist/Mitschreibprogramm.exe (141 MB) aus diesem Stand gebaut. Der Selbsttest lief gegen den Debug-Build; die exe hat denselben App-Code.
- Vorher (2026-09-19): alle 45 Features `done`, Checkpoint 1 bis 6 bestanden, Seitenstil Strichliert durch Kariert ersetzt.
- Selbsttest-Werkzeuge in tools/: pen-sim.ps1, screenshot.ps1, app-control.ps1 (neu: -RightClick, -DoubleClick, -Exists, -Items, -Value), selftest.ps1 (startet nicht, solange eine Instanz läuft; wiederholt einen verlorenen Strich einmal), Checkpoints in tools/selftest/.
- Am 2026-09-28 auf Daniels Freigabe gepusht: origin/main steht auf dem Stand dieses Commits (94 Commits seit `Initial commit`). Die lokale Historie wurde vorher einmal neu geschrieben, damit jeder Commit baut (decisions.md).

## Nächster Schritt

- Daniel testet mit dem echten Wacom Intuos nach testing.md, Abschnitt "Offen mit echtem Pen" (mit MSP_DEBUG_LOG liegt die Bibliothek neben dem Log). Was dort bestätigt ist, wandert in features.md von `done` auf `getestet`.
- Offen zur Entscheidung: die Änderungszeit unter dem Heftnamen (nicht in der Spezifikation, eine Zeile in NotebookPanel.xaml) behalten oder streichen.
- Weitere Pushes nur auf ausdrückliche Anweisung: `ALLOW_PUSH=1 git push origin main`.

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
10. Hefte-Bibliothek: Library + Tests, Session, Seitenleiste, Dialoge, Import/Export, Reviews, Selbsttest Checkpoint 4, docs (fertig)

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.
- Selbsttest: ein injizierter Strich geht hin und wieder verloren (2026-09-19 unter Parallelbetrieb, 2026-09-28 zweimal in frühen Läufen ohne). Seit 2026-09-28 wiederholt `Pen` ihn einmal und meldet RETRY. Steht auch unter "Offen mit echtem Pen".
- Zweite Instanz wird über notes/.lock abgewiesen (`LibraryLock`). Nicht abgedeckt: zwei Rechner, die denselben Ordner über einen Sync-Dienst teilen.
- Speichern läuft synchron auf dem UI-Thread; bei sehr großen Heften ein kurzer Ruckler alle 60 s (decisions.md).

## Bekannte Bugs

- keine offenen.
- Behoben am 2026-09-28 (vor dem Selbsttest, aus den Reviews): Löschen des offenen Hefts öffnete kein Folgeheft; Striche ohne offenes Heft gingen verloren; Rechtsklick öffnete das Heft vor dem Menü; zwei Instanzen überschrieben sich den Index; MSP-Export in den Bibliotheksordner überschrieb Hefte.
- Behoben am 2026-09-19: Hilfslinien verschwanden bandweise bei Zoom unter 100 %. Gerader Druckstrich kam im PDF mit konstanter Breite an. Dark-Mode-Toggle reagierte nicht auf UI Automation. Kaputte .msp (null-Listen, Riesenkoordinaten, Enum als Zahl) konnte abstürzen. Gehaltene Taste ließ Umschalter (E, Strg+D, Strg+L) flackern.
