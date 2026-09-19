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

## 2026-09-19 Selbsttest-Werkzeuge mit gemeinsamer MspNative-Klasse

- Entscheidung: P/Invoke-Code liegt einmal in `tools/MspNative.*.cs` (partielle Klasse, drei Dateien unter 300 Zeilen), die Skripte laden ihn per Add-Type. Zusätzlich zu pen-sim.ps1 und screenshot.ps1 gibt es app-control.ps1 (Tasten, Maus, UI Automation) und selftest.ps1 (fährt die Checkpoints, auch gegen dist/).
- Begründung: Checkpoint 6 verlangt, den Selbsttest komplett gegen die exe zu wiederholen. Das geht nur verlässlich, wenn er ein Skript ist.
- Screenshots laufen über PrintWindow (PW_RENDERFULLCONTENT), damit verdeckende Fenster das Bild nicht verfälschen.

## 2026-09-19 Hilfslinien mit einer Guideline pro Linie

- Entscheidung: `PageBackground` setzt pro Linie nur eine Guideline (Oberkante), nicht zwei.
- Begründung: Mit zwei Guidelines rasten bei Zoom unter 100 % beide Kanten auf denselben Gerätepixel ein, die Linie wird 0 Pixel hoch und verschwindet bandweise (im Checkpoint-3-Screenshot bei 50 % gefunden). Eine Guideline hält alle Linien gleich und immer sichtbar.
- Verworfen: gar keine Guidelines (Linien wirken bei 125 % Skalierung ungleich dick).

## 2026-09-19 Seitenmodell und Konvertierung

- Entscheidung: Jede Seite ist ein eigener InkCanvas (`PageView`), Endlos ist eine einzige, wachsende `PageView`. Die Konvertierung läuft auf den reinen Datenklassen (`NoteDocument`), nicht auf WPF-Objekten: `PageModeConverter.Convert`.
- Seiten zu Endlos: Y-Offset = Seitenhöhe mal Index. Endlos zu Seiten: Kachel = A4, ein Strich gehört zur Kachel seines ersten Punkts und behält alle Punkte (ragt er über den Rand, wird er dort nur abgeschnitten dargestellt).
- Rechts von der A4-Breite: Spalte 0 ergibt pro Zeile immer eine Seite (leere Zeilen bleiben als leere Seiten, damit Abstände stimmen). Kacheln weiter rechts werden nur zu Seiten, wenn dort etwas steht, und folgen direkt auf ihre Zeile. So geht nichts verloren, was auf der Endlos-Fläche rechts geschrieben wurde. Der PDF-Export nutzt dieselbe Funktion.
- Verworfen: nur in der Höhe schneiden. Inhalt rechts der A4-Breite wäre im Seitenmodus und im PDF unsichtbar.
- Undo: Der Moduswechsel ist ein eigener Undo-Schritt, der die alten `PageView`-Objekte behält. Ältere Schritte zeigen weiter auf gültige Objekte, der Verlauf reicht wie gefordert bis zum letzten Öffnen oder Neu. Verworfen: Verlauf beim Moduswechsel leeren.
- Neue Seiten (Button, Ctrl+Enter, Auto-Seite) sind keine Undo-Schritte, Undo bleibt strichbasiert.
- Endlos wächst nach einem Strich, der näher als 200 px an den rechten oder unteren Rand kommt, in Schritten von einer halben A4-Breite bzw. -Höhe.

## 2026-09-19 Datei-Befehle

- Entscheidung: Eigener kleiner Dialog `UnsavedChangesDialog` statt MessageBox. Begründung: Die Buttons sollen Speichern / Verwerfen / Abbrechen heißen, MessageBox kann nur Ja / Nein / Abbrechen.
- Undo bis zum gespeicherten Stand löscht den Stern nicht (kein Vergleich mit dem Stand auf der Platte). Einfachste Variante, im Zweifel wird einmal zu viel nachgefragt.
- Speichern schreibt in eine .tmp-Datei und verschiebt sie über das Ziel, damit ein Absturz keine halbe Notiz hinterlässt.

## 2026-09-19 PDF-Export mit PDFsharp 6.2.4

- Paket: `PDFsharp` 6.2.4 (Core-Build, MIT, net8.0, keine WPF-/GDI-Abhängigkeit). Einziges externes Paket der App. Quelle: nuget.org und docs.pdfsharp.net, Build und Restore laufen ohne Warnung. Verworfen: `PDFsharp-WPF` (erzeugt pro Seite WPF-Objekte, für reine Linien unnötig).
- Seiten: exakt 210 x 297 mm, Zeichnen in Seitenpixeln über `ScaleTransform(72/96)`.
- Striche: Punkte kommen aus `Stroke.GetBezierStylusPoints()`, also dieselbe geglättete Kurve wie auf dem Bildschirm. Ohne Druck ein Linienzug mit konstanter Breite. Mit Druck ein Linienstück pro Abschnitt mit eigener Breite (runde Enden), Breite = Basis x (1,5 x Druck + 0,25) wie in WPF.
- Fallstrick: Die Bezier-Glättung macht aus einer geraden Linie zwei Punkte. Ein Mittelwert pro Abschnitt würde den Druckverlauf verlieren (im Checkpoint 5 gefunden). Deshalb teilt `PdfExporter.PressureSegments` jeden Abschnitt so, dass die Breite pro Teilstück höchstens 0,25 px springt.
- Verworfen: Umriss aus `Stroke.GetGeometry()` füllen (exakt, aber sehr große PDFs bei Druckstrichen).
- Hilfslinien: Bildschirmfarbe (mit Alpha) wird auf Weiß vorgemischt (`Palette.RuleLineOnPaper`), keine Transparenz im PDF. Gestrichelt über `XPen.DashPattern`, eine Linie pro DrawLine.
- Endlos: `PageModeConverter` schneidet in A4-Kacheln, derselbe Code wie beim Moduswechsel.
- PDFsharp-Fallen: `PdfDocumentOpenMode.ReadOnly` ist obsolet (Tests nutzen `Import`), `XGraphicsPath.StartFigure` wirkt im Core-Build nicht (deshalb DrawLine / DrawLines statt Pfaden).

## 2026-09-19 Dark Mode

- .NET 8 hat weder Fluent-Theme noch `ThemeMode` (kommt erst mit .NET 9), also von Hand: Alle UI-Farben sind DynamicResource-Pinsel, `Rendering/Theme.Apply` ersetzt sie im laufenden Betrieb (ersetzen, nicht ändern, weil Pinsel in den Application-Resources eingefroren werden).
- Farben an zwei Stellen nach Zweck: UI-Farben beider Themen in `Theme`, Stift-, Seiten- und Linienfarben in `Palette`. Jede Farbe steht genau einmal.
- Schwarz auf dunkler Seite: `Palette.PenOnPage` liefert Weiß, `Palette.LogicalPen` rechnet jede angezeigte Farbe zurück. Beim Umschalten färbt `DocumentView.ApplyTheme` alle Striche um. Dasselbe passiert nach jedem Undo/Redo und Moduswechsel, weil Striche und Seiten aus dem Verlauf noch die Farben des anderen Themas tragen können. Datei und PDF sehen nur die logische Farbe.
- Verworfen: logische Farbe als Zusatzdaten am Strich speichern (`AddPropertyData`). Bei genau vier Farben reicht das Zurückrechnen.
- Eigene Vorlagen nur dort, wo Aero2 feste helle Farben malt: ComboBox und ScrollBar (App.xaml). Slider und CheckBox bleiben nativ.
- Titelleiste: `DwmSetWindowAttribute` mit Attribut 20 (DWMWA_USE_IMMERSIVE_DARK_MODE) in OnSourceInitialized jedes Fensters, sonst bliebe sie weiß. Schlägt der Aufruf auf altem Windows fehl, passiert nichts.
- Toggle hängt an Checked / Unchecked statt Click, damit er auch über UI Automation (Bedienhilfen, Selbsttest) schaltet.

## 2026-09-19 Unabhängiger Code-Review vor der Auslieferung

Ein zweiter, nur lesender Durchgang über den ganzen Code hat diese Fehler gefunden, alle behoben und einzeln committet:

- `InvalidDataException` erbt von SystemException, nicht von IOException. Eine .msp mit neuerer Version hätte die App beim Öffnen beendet statt eine Meldung zu zeigen.
- Redo eines Moduswechsels zeigte die Seitenliste vom Zeitpunkt des Wechsels. Danach angehängte Seiten samt Strichen verschwanden still. Der Undo-Schritt merkt sich jetzt beide Seitenlisten jeweils beim Verlassen neu.
- settings.json: Speichern beim Schließen ist abgesichert, negative Fenstergrößen und nicht definierte Enum-Werte führen zu Standardwerten statt zu einem Absturz bei jedem Start. In .NET 8 lässt der Enum-Konverter Zahl-Strings wie "7" durch, deshalb zusätzlich `Enum.IsDefined` (auch in .msp, dort als Fehler).
- Letzter Ordner wurde bei jeder Änderung auf den Dokumentordner zurückgesetzt. Jetzt nur noch bei Öffnen, Speichern und Export.
- Fehlgeschlagenes Speichern ließ `<name>.msp.tmp` liegen.
- InkCanvas hängt sich an das Ereignis seiner DefaultDrawingAttributes. Weil alle Seiten denselben Stift teilen, hielt der Stift jede je erzeugte Seite am Leben. `Load` hängt alte Seiten jetzt ab.
- Seitentasten-Zustand blieb für die Maus hängen, wenn der Stift über der Toolbar abgehoben wurde. `StylusLeave` setzt ihn zurück.

## 2026-09-19 Selbsttest nimmt dem Benutzer keine Eingabe weg

- Anlass: Beim ersten Versuch von Checkpoint 6 war der Bildschirm erst gesperrt und danach wurde am Rechner gechattet. Ein Lauf hätte Dateipfade und Enter in ein fremdes Fenster tippen können.
- `selftest.ps1` startet nur, wenn seit `-IdleSeconds` (15 s) niemand Maus oder Tastatur benutzt hat (GetLastInputInfo), und bricht sonst nach `-IdleWaitSeconds` mit Exit-Code 99 ab.
- `MspNative.KeyChord` und `TypeText` prüfen vor jedem einzelnen Tastendruck, ob ein Fenster der App im Vordergrund ist, und brechen sonst ab. Bereits gedrückte Tasten werden losgelassen. Pen und Maus prüfen wie bisher, dass der Zielpixel zur App gehört.
- Grenze: Injizierte Eingaben setzen den Leerlauf-Zähler selbst zurück. Ob mitten im Lauf jemand dazukommt, lässt sich darüber nicht erkennen, nur über den Vordergrund-Check.

## 2026-09-19 pen-sim zeichnet einen Strich, die definierten Striche stehen in den Checkpoints

- pen-sim.ps1 injiziert genau einen parametrierten Strich (Start, Ende, Druck von / bis, Seitentaste, invertiert). Die in der Spezifikation genannten Striche (steigender Druck, Seitentaste über bestehendem Strich, invertiert, Strich bei 200 % Zoom) sind Aufrufe davon in `tools/selftest/checkpoint-1.ps1` und `checkpoint-2.ps1`.
- Begründung: Ein Strich pro Aufruf lässt sich mit Tasten, Maus und Log-Prüfungen beliebig kombinieren. Feste Szenarien im Skript hätten jede neue Prüfung zu einer Skriptänderung gemacht.
- Die P/Invoke-Structs bleiben in der partiellen Klasse `MspNative` verschachtelt. Sie haben außerhalb keine Bedeutung und werden per Add-Type gemeinsam übersetzt.

## 2026-09-19 Fenster-Aktivierung im Debug-Log

- Das Debug-Log schreibt zusätzlich `window activated` und `window deactivated`. Der Selbsttest blendet diese Zeilen bei den Zählprüfungen aus und zeigt sie bei jedem FAIL an.
- Anlass: Beim ersten Anlauf von Checkpoint 6 blieben sporadisch injizierte Gesten wirkungslos (zweimal ein Pen-Strich, einmal ein Ziehen mit der mittleren Maustaste), jedes Mal an anderer Stelle, nur während parallel Hintergrundprozesse liefen. Ein fremdes Fenster im Vordergrund deaktiviert das App-Fenster, WPF bricht dann laufende Gesten ab. Mit den Zeilen ist das im Fehlerfall belegbar statt vermutet.
- Regel für den Selbsttest: währenddessen nichts anderes auf dem Rechner starten, auch keine Hintergrund-Agenten oder Builds.
