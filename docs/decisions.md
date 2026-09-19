# Entscheidungen

Chronologisch. Je Eintrag: Entscheidung, Begründung, verworfene Alternative.

## 2026-09-19 Lokales Repo auf origin/main aufgesetzt

- Entscheidung: `git init`, `git fetch origin`, `git reset origin/main`. Lokale Historie baut auf dem GitHub-Commit `Initial commit` (README) auf.
- Begründung: Ein späterer Push läuft ohne `--force` und ohne unrelated histories.
- Verworfen: frische lokale Historie ohne Bezug zum Remote.

## 2026-09-19 Standard-Stylus-Stack bleibt, kein Pointer-Stack

- Entscheidung: Kein `Switch.System.Windows.Input.Stylus.EnablePointerSupport`, auch nicht im Debug-Modus.
- Begründung: Der synthetische Pen (InjectSyntheticPointerInput) kommt im Standard-Stack als Stylus mit Druck, Barrel und Inverted an (Checkpoint 1). Der Selbsttest prüft damit denselben Codepfad wie der echte Pen.
- Verworfen: Pointer-Stack nur für Tests. Hätte einen anderen Pfad getestet als den produktiven.

## 2026-09-19 Seitentaste per StylusButtons, invertiert per InkCanvas

- Entscheidung: Barrel-Zustand aus `StylusDevice.StylusButtons` lesen und EditingMode umschalten. Invertierten Stift überlässt die App `InkCanvas.EditingModeInverted`.
- Begründung: InkCanvas wertet die Seitentaste laut Quelltext nie aus, Inverted dagegen vollständig selbst. Details und Quellen in pen-input.md.
- Verworfen: eigenes Hit-Testing zum Radieren.

## 2026-09-19 Ein DrawingAttributes-Objekt für alle Seiten

- Entscheidung: Alle InkCanvas teilen sich dieselbe `DrawingAttributes`-Instanz.
- Begründung: Farbe, Breite und Druck werden einmal gesetzt und gelten überall. InkCanvas klont die Attribute pro Strich, alte Striche ändern sich nicht.

## 2026-09-19 Undo über StrokeCollected / StrokeErasing

- Entscheidung: Undo-Schritte entstehen in den InkCanvas-Events StrokeCollected und StrokeErasing. Ein Schritt ist ein Paar aus Undo- und Redo-Aktion (`UndoStep`).
- Begründung: Beide Events kommen nur bei Benutzeraktionen, nicht beim Laden oder bei Undo selbst, es braucht also kein Sperr-Flag. StrokeErasing kommt vor dem Entfernen, der Strich kehrt beim Undo an seine alte Z-Position zurück.
- Verworfen: `StrokeCollection.StrokesChanged` (feuert auch bei programmatischen Änderungen, Index des entfernten Strichs unbekannt).

## 2026-09-19 Zoom über LayoutTransform auf der Dokumentfläche

- Entscheidung: `DocumentView.LayoutTransform = ScaleTransform`. Zoom um den Zeiger: Inhaltspunkt unter dem Zeiger merken, skalieren, `UpdateLayout()`, Scroll-Offsets um die Abweichung korrigieren.
- Begründung: LayoutTransform ändert den Scrollbereich mit, alles bleibt Vektor, Pen-Eingabe bleibt korrekt gemappt. RenderTransform würde weder Scrollbereich noch Pen-Mapping anpassen.
- Grenze: Ist die Seite schmaler als das Fenster, bleibt sie zentriert. Dann gibt es horizontal keinen Scrollbereich und nur die vertikale Achse bleibt unter dem Zeiger stehen.
- Ctrl+Plus/Minus springen über feste Stufen (25, 50, 75, 100, 125, 150, 200, 300, 400 %), Ctrl+Mausrad multipliziert pro Raste mit 1,1.

## 2026-09-19 Hilfslinien starten pro A4-Höhe neu

- Entscheidung: Linien liegen bei Vielfachen von 8 mm innerhalb jeder A4-Höhe (`RuleLines.Positions`), auch auf der Endlos-Fläche.
- Begründung: 297 mm ist kein Vielfaches von 8 mm. Mit durchlaufendem Raster würde Geschriebenes nach Seiten-zu-Endlos-Umwandlung und im PDF-Export pro Seite 1 mm gegen die Linien wandern.

## 2026-09-19 Eigene flache Button-Styles statt ToolBar-Control

- Entscheidung: Toolbar ist ein Border mit WrapPanel, Buttons nutzen den Style `FlatButton` mit DynamicResource-Pinseln.
- Begründung: Die Aero2-Vorlagen von ToolBar (Overflow-Pfeil, Hover-Farben) sind fest verdrahtet und im Dark Mode nicht umfärbbar. WrapPanel bricht bei schmalem Fenster um, statt Buttons zu verstecken.

## 2026-09-19 Selbsttest-Werkzeuge mit gemeinsamer MspNative.cs

- Entscheidung: P/Invoke-Code liegt einmal in `tools/MspNative.cs`, die Skripte laden ihn per Add-Type. Zusätzlich zu pen-sim.ps1 und screenshot.ps1 gibt es app-control.ps1 (Tasten, Maus, UI Automation) und selftest.ps1 (fährt die Checkpoints, auch gegen dist/).
- Begründung: Checkpoint 6 verlangt, den Selbsttest komplett gegen die exe zu wiederholen. Das geht nur verlässlich, wenn er ein Skript ist.
- Screenshots laufen über PrintWindow (PW_RENDERFULLCONTENT), damit verdeckende Fenster das Bild nicht verfälschen.
