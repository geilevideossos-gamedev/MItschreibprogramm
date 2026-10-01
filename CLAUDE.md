# Mitschreibprogramm

Handschrift-Notizprogramm für Windows, bedient mit einem One by Wacom (CTL-672) über Windows Ink.
Stack: C# / WPF / .NET 8. Ergebnis: eine einzelne exe in dist/.
Scope steht in docs/features.md. Was dort nicht steht, wird nicht gebaut.

## Sprache

- UI-Texte: Deutsch
- Code, Identifier, Commit-Messages: Englisch
- README und docs/: Deutsch
- Antworten an Daniel: kurz, Deutsch, keine langen Erklärungen

## Git

- Remote: origin = git@github.com:geilevideossos-gamedev/MItschreibprogramm.git (HTTPS-Fallback: https://github.com/geilevideossos-gamedev/MItschreibprogramm.git)
- Committen: ja, nach jedem abgeschlossenen Feature oder Bugfix (siehe unten)
- Pushen: nur wenn Daniel es in seiner aktuellen Nachricht ausdrücklich sagt ("push"). Dann `ALLOW_PUSH=1 git push origin main`. Nie von selbst, nie am Ende eines Schritts, nie in einem Skript.
- Pre-push Hook in .githooks/pre-push blockt jeden Push ohne `ALLOW_PUSH=1`. Hook nie ändern, nie `--no-verify`. Die Variable nur im Push-Befehl setzen, nie exportieren.
- Ein Commit pro abgeschlossenem Feature oder Bugfix, nicht pro Zwischenschritt. Kein Commit für reine Zwischenstände, kein "WIP".
- Docs-, README- und status.md-Änderungen kommen immer in denselben Commit wie die feat/fix-Änderung, zu der sie gehören. Keine eigenen Docs-Commits, kein Prefix `docs:`.
- Conventional Commits, Englisch, Imperativ, klein nach dem Prefix. Erlaubt sind nur:
  - `feat:` neue Funktion
  - `fix:` Bugfix
  - `refactor:` keine Verhaltensänderung
  - `chore:` Tooling, gitignore, Hooks, Dependencies
  - `build:` csproj, Publish, Build-Skripte
- Beispiel: `feat: add eraser mode via pen side button`
- Vor jedem feat/fix-Commit: docs/status.md und betroffene docs aktualisieren und mit in den Commit nehmen.

## Code

- Kein Bulk: keine Features, Abstraktionen, Interfaces oder Helper "für später". Nur was docs/features.md verlangt.
- Kommentare nur, wenn wirklich relevant: Workaround, Treiber-Fallstrick, nicht offensichtliche Rechnung. Nie beschreiben, was der Code ohnehin zeigt. Im Zweifel weglassen.
- Keine NuGet-Pakete außer den in docs/architecture.md gelisteten (Testprojekt: xUnit erlaubt). Neues Paket = erst begründen, in docs/decisions.md eintragen.
- Tests: xUnit-Projekt Mitschreibprogramm.Tests nur für Models/Services (Dateiformat, Seitenkonvertierung, PDF-Seitenzahl, Settings). Keine UI-Tests dort.
- Debug-Code in der App nur hinter der Umgebungsvariable MSP_DEBUG_LOG. Ohne Variable darf nichts davon laufen.
- WPF-Bordmittel bevorzugen (InkCanvas, StrokeCollection, StylusPoint, DrawingAttributes, ScaleTransform) statt Eigenbau.
- Eine Klasse pro Datei, Dateien unter 300 Zeilen. Wird es größer: aufteilen.
- Kein toter Code, keine auskommentierten Blöcke, keine ungenutzten usings.
- `dotnet build` muss nach jedem Schritt mit 0 Errors und 0 Warnings durchlaufen.
- Namespace `Mitschreibprogramm.*`, Ordner Models/, Views/, Services/, Rendering/ (Details in docs/architecture.md).
- Farben, Linienabstand, A4-Maße, Zoomgrenzen: je genau eine Konstante, keine Magic Numbers im Code verteilt.

## Context7

- Bei jeder Unsicherheit zu einer API (WPF Stylus/Ink, PDFsharp, dotnet publish): Context7 abfragen, nicht raten.
- Erst Context7, dann implementieren. Erkenntnisse in docs/pen-input.md oder docs/decisions.md festhalten.

## Docs-System

- Diese Datei: max 200 Zeilen. Jede docs/*.md: max 200 Zeilen. Wird eine länger: aufteilen, nicht kürzen und Wissen verlieren.
- docs/ ist dein Gedächtnis. Alles, was du beim nächsten Start wieder brauchst, steht dort, nicht im Chat.
- Dateien:
  - docs/track.md: wie Fortschritt getrackt wird, Regeln für die docs. Zuerst lesen.
  - docs/status.md: aktueller Stand, nächster Schritt, offene Punkte, bekannte Bugs
  - docs/features.md: vollständige Feature-Liste mit Status (todo / wip / done / getestet)
  - docs/architecture.md: Projektstruktur, Klassen, Datenfluss, NuGet-Pakete
  - docs/pen-input.md: One by Wacom, Windows Ink, Druck, Seitentasten, Treiber-Fallstricke
  - docs/file-format.md: Dateiformat .msp (JSON), settings.json und notes/index.json
  - docs/build.md: Build- und Publish-Befehle, exe-Ausgabe
  - docs/testing.md: manuelle Test-Checkliste, Checkpoints
  - docs/decisions.md, docs/decisions-2.md: Entscheidungen mit Begründung, chronologisch (neue Einträge in decisions-2.md)
- Sessionstart: CLAUDE.md, dann docs/track.md, dann docs/status.md, dann nur die docs, die der aktuelle Schritt braucht.
- Sessionende und vor jedem /compact: status.md und betroffene docs aktualisieren. Sie gehen mit dem nächsten abgeschlossenen feat/fix in den Commit.

## Workflow

1. Lesen (siehe Sessionstart), Schrittplan zeigen, auf "ok passt" warten.
2. Schritte einzeln: implementieren, `dotnet build`, docs aktualisieren. Committet wird, wenn das Feature oder der Bugfix fertig und geprüft ist: ein Commit samt docs.
3. Checkpoints (docs/testing.md): zuerst Selbsttest ohne Pen (tools/pen-sim.ps1, Debug-Log, Screenshots, xUnit). Ist Daniel mit Tablet da, testet er zusätzlich und du wartest auf "ok passt". Sonst weiter, und alles, was nur mit echtem Pen prüfbar ist, in docs/testing.md unter "Offen mit echtem Pen" sammeln. features.md: "done" nach Selbsttest, "getestet" erst nach echtem Pen.
4. Kontext über 60 %: docs aktualisieren, dann /compact.
5. Blocker: kurz melden, nicht drumherum bauen.
6. Nicht bei jedem Schritt nachfragen. Nur bei Plan, Checkpoints, Blockern und echten Produktentscheidungen, die weder hier noch in docs/ stehen.

## Hardware

- Tablet: One by Wacom (Medium), CTL-672, USB. Aktive Fläche ca. 216 x 135 mm, wird per Treiber auf den Bildschirm gemappt.
- Stift: der Stift der One by Wacom. 2048 Druckstufen, zwei Seitentasten, kein Tilt, kein Radierer am Stiftende. Tilt also nie implementieren.
- Keine ExpressKeys am Tablet. Die Shortcuts bleiben trotzdem Einzeltasten.
- Druck kommt in WPF nur an, wenn im Wacom-Treiber "Windows Ink verwenden" aktiv ist. Ohne Windows Ink sieht die App nur Maus-Events.
- Daniel hat das Tablet nicht immer dabei. Ohne Tablet testest du selbst nach docs/testing.md (synthetische Pen-Eingabe über Win32, Debug-Log, Screenshots, xUnit). Maus-Eingabe muss immer auch funktionieren.

## Umgebung

- Windows, Git Bash. Skripte mit Forward-Slashes.
- .NET 8 SDK muss vorhanden sein (`dotnet --version`). Fehlt es: melden, nicht selbst installieren.
- Build-Ausgabe: dist/Mitschreibprogramm.exe. dist/, bin/, obj/, tmp/ stehen in .gitignore.
- Google-MCPs (Drive, Gmail, Calendar) sind verbunden, werden für dieses Projekt nicht gebraucht. Nicht verwenden.
