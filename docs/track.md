# Track: wie Fortschritt gepflegt wird

Zuerst lesen. Danach docs/status.md, dann nur die docs, die der aktuelle Schritt braucht.

## Regeln für docs/

- Jede Datei max. 200 Zeilen. Wird sie länger: aufteilen (z. B. decisions-2.md), nichts löschen.
- docs/ ist das Gedächtnis. Was beim nächsten Start gebraucht wird, steht hier, nicht im Chat.
- Deutsch, kurz, Fakten statt Prosa.

## Status

- docs/status.md: aktueller Stand, nächster Schritt, offene Punkte, bekannte Bugs. Nach jedem feat/fix aktualisieren, im selben Commit wie die Änderung.
- docs/features.md: eine Zeile pro Feature mit ID und Status.
  - `todo`: nicht begonnen
  - `wip`: in Arbeit
  - `done`: implementiert und per Selbsttest geprüft (pen-sim, Debug-Log, Screenshot, xUnit)
  - `getestet`: zusätzlich von Daniel mit echtem Pen bestätigt
- Was nicht in features.md steht, wird nicht gebaut.

## Checkpoints

- Liste und Ablauf in docs/testing.md.
- An jedem Checkpoint: Selbsttest laufen lassen, Ergebnis in testing.md eintragen (Datum, Ergebnis, Auffälligkeiten) und im Chat in zwei Sätzen melden.
- Fehler: beheben, Checkpoint wiederholen, dann ein `fix:`-Commit samt docs.
- Alles, was nur mit echtem Pen prüfbar ist, kommt in testing.md unter "Offen mit echtem Pen".

## Entscheidungen

- docs/decisions.md und die Fortsetzung docs/decisions-2.md (neue Einträge dort), chronologisch, je Eintrag: Datum, Entscheidung, Begründung, verworfene Alternative.
- API-Erkenntnisse (Context7, Primärquellen): Pen/Ink nach pen-input.md, alles andere nach decisions.md.
- Neues NuGet-Paket: erst in decisions.md begründen, dann in architecture.md listen.

## Commits

- Conventional Commits, Englisch, nur `feat`, `fix`, `refactor`, `chore`, `build` (Details in CLAUDE.md).
- Ein Commit pro abgeschlossenem Feature oder Bugfix, docs und README im selben Commit. Keine Docs-Commits, keine Zwischenstände.
- Push nur auf ausdrückliche Anweisung: `ALLOW_PUSH=1 git push origin main`.
