# Status

Stand: 2026-09-29

## Aktuell

- Auftrag vom 2026-09-29 erledigt: Hardware-Korrektur auf One by Wacom CTL-672 (CLAUDE.md, pen-input.md, README ohne ExpressKeys), Formerkennung (F56 bis F62), Bilder mit Strg+V (F63 bis F68, Formatversion 2), Lasso-Auswahl (F69 bis F74). Dazu auf Daniels Wunsch die Änderungszeit in der Heftliste kurz und relativ, und seine Änderung an mega-prompt-claude-code-start.md als docs-Commit.
- `dotnet build` 0 Warnings, 0 Errors. `dotnet test` 94 Tests grün (neu: Formerkennung mit Linien, Kreisen, Rechtecken, Dreiecken und Handschrift, die frei bleiben muss; Stillhalten; Bilder im .msp, alte Version 1 lädt; Bilder im PDF; Auswahl-Rechtecke; relative Zeit).
- Selbsttest gegen die neu gepublishte dist/Mitschreibprogramm.exe: Checkpoint 8 11/11, Checkpoint 9 20/20, Checkpoint 4, 5, 7 50/50, Checkpoint 1, 2, 3 31/31 (im ersten Anlauf 28/31 nach einem verlorenen Eingabe-Event, Details testing.md „Lauf 2026-09-29“). F56 bis F74 stehen auf `done`.
- Die obere Seitentaste (Barrel) radiert nicht mehr, sie ist jetzt das Lasso. Radiert wird mit der unteren Taste, belegt als „Radieren“ im Treiber (invertiert). README und pen-input.md erklären die Belegung.
- Nichts gepusht seit dem Push vom 2026-09-28.

## Nächster Schritt

- Daniel testet mit der echten One by Wacom nach testing.md, Abschnitt "Offen mit echtem Pen" (mit MSP_DEBUG_LOG liegt die Bibliothek neben dem Log), vor allem die Belegung der Seitentasten (oben „Rechtsklick“ = Lasso, unten „Radieren“), das Stillhalten für Formen und das Lasso. Was dort bestätigt ist, wandert in features.md von `done` auf `getestet`.
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
11. Hardware-Korrektur, relative Änderungszeit, Formerkennung, Bilder, Lasso, Selbsttest Checkpoint 8 und 9 gegen die exe (fertig)

## Offene Punkte

- Context7 war in der Session vom 2026-09-19 nicht erreichbar (DNS). API-Fakten stammen aus Primärquellen (dotnet/wpf Quelltext, learn.microsoft.com, nuget.org) und sind in pen-input.md / decisions.md mit Quelle notiert.
- Selbsttest: ein injizierter Strich geht hin und wieder verloren (2026-09-19 unter Parallelbetrieb, 2026-09-28 zweimal in frühen Läufen ohne). Seit 2026-09-28 wiederholt `Pen` ihn einmal und meldet RETRY. Steht auch unter "Offen mit echtem Pen".
- Zweite Instanz wird über notes/.lock abgewiesen (`LibraryLock`). Nicht abgedeckt: zwei Rechner, die denselben Ordner über einen Sync-Dienst teilen.
- Speichern läuft synchron auf dem UI-Thread; bei sehr großen Heften ein kurzer Ruckler alle 60 s (decisions.md).

## Bekannte Bugs

- Offen seit Daniels Test am 2026-09-29: Bilder ließen sich nicht löschen. Ursache (Diagnose mit dem synthetischen Stift gegen die exe vom 2026-09-29): im Auswahl-Modus wählt ein Stift-Tippen, das sich gar nicht bewegt, nichts aus, weder mit der Spitze noch mit gedrückter Seitentaste. Ohne Auswahl hat Entf nichts zu löschen. Mit der Maus, mit einem Tippen mit 4 oder 9 px Bewegung und per Lasso (auch Bild und Strich zusammen) wird das Bild gewählt und Entf löscht es samt Undo. InkCanvas nimmt das Tippen im Select-Modus über die aus dem Stift hochgestufte Maus-Eingabe an (`EditingCoordinator.OnInkCanvasDeviceDown`); ein ruhiges Tippen meldet WPF vermutlich erst beim Abheben als Mausklick, dann ist der Stift schon in der Luft. Dieser letzte Schritt ist aus dem Quelltext abgeleitet, nicht gemessen. Dazu radiert der Radierer (Werkzeug und untere Taste) nur Striche, Bilder blieben liegen.
- Behoben am 2026-09-28 (vor dem Selbsttest, aus den Reviews): Löschen des offenen Hefts öffnete kein Folgeheft; Striche ohne offenes Heft gingen verloren; Rechtsklick öffnete das Heft vor dem Menü; zwei Instanzen überschrieben sich den Index; MSP-Export in den Bibliotheksordner überschrieb Hefte.
- Behoben am 2026-09-19: Hilfslinien verschwanden bandweise bei Zoom unter 100 %. Gerader Druckstrich kam im PDF mit konstanter Breite an. Dark-Mode-Toggle reagierte nicht auf UI Automation. Kaputte .msp (null-Listen, Riesenkoordinaten, Enum als Zahl) konnte abstürzen. Gehaltene Taste ließ Umschalter (E, Strg+D, Strg+L) flackern.
