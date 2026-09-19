# Entscheidungen

Chronologisch. Je Eintrag: Entscheidung, Begründung, verworfene Alternative.

## 2026-09-19 Lokales Repo auf origin/main aufgesetzt

- Entscheidung: `git init`, `git fetch origin`, `git reset origin/main`. Lokale Historie baut auf dem GitHub-Commit `Initial commit` (README) auf.
- Begründung: Ein späterer Push läuft ohne `--force` und ohne unrelated histories.
- Verworfen: frische lokale Historie ohne Bezug zum Remote.
