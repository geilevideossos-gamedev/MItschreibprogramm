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
- Hilfslinien: Bildschirmfarbe (mit Alpha) wird auf Weiß vorgemischt (`Palette.RuleLineOnPaper`), keine Transparenz im PDF. Gestrichelt über `XPen.DashPattern`, eine Linie pro DrawLine. (Überholt seit dem Eintrag "Seitenstil Kariert ersetzt Strichliert": es gibt kein DashPattern mehr, das Gitter sind durchgezogene Linien.)
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

## 2026-09-19 Seitenstil Kariert ersetzt Strichliert

- Auftrag von Daniel: Statt gestrichelter Linien ein Karo-Gitter wie im Rechenheft. Durchgezogene dünne waagrechte und senkrechte Linien, Abstand 5 mm (`AppConstants.GridSpacing`). Liniert bleibt bei 8 mm. Linienfarbe Schwarz / Blau und die Dark-Mode-Farben gelten für das Gitter genauso, es nutzt dieselbe Farbe wie die Linien (`Palette.RuleLine`).
- Name: in UI, docs und README "Kariert". Im Code `PageStyle.Squared` und in den Dateien `"squared"`, weil Identifier und Formatwerte laut CLAUDE.md englisch sind.
- Alte Dateien: .msp und settings.json mit `"dashed"` laden als kariert (`PageStyleJsonConverter`), geschrieben wird nur noch `"squared"`. Die Formatversion bleibt 1, weil sich sonst nichts ändert. Verworfen: `dashed` als zweiten Enum-Namen behalten (welcher Name beim Schreiben gewinnt, wäre Zufall).
- Geometrie in `RuleLines`: Zeilen starten pro A4-Höhe neu wie bei Liniert (297 mm ist kein Vielfaches von 5 mm, die unterste Zelle jeder Seite ist 2 mm hoch). Spalten laufen auf der Endlos-Fläche durch, weil 210 mm ein Vielfaches von 5 mm ist. Eine Linie genau auf dem rechten Seitenrand wird nicht gezeichnet.
- `RuleLines` liegt jetzt in Models (reine Seitengeometrie ohne WPF), damit die xUnit-Tests sie prüfen dürfen. Die Strich-Konstanten `RuleDashLength` / `RuleDashGap` sind entfallen.
- Am Bildschirm ist der Linienstift halbtransparent. Zeilen und Spalten einzeln gezeichnet hätten jede Kreuzung doppelt überblendet (dunklere Punkte, im Review gefunden). `PageBackground` zeichnet deshalb alle Linien als eine `StreamGeometry` in einem Aufruf. Verworfen: `PushOpacity` um deckende Linien (braucht pro Seite eine Zwischenfläche in Zoomgröße).
- Der Konverter vergleicht Stilnamen ohne Rücksicht auf Groß- und Kleinschreibung, wie es der Standard-Enum-Konverter vorher tat.

## 2026-09-28 Hefte-Bibliothek: Metadaten in index.json, nicht im msp-Header

- Auftrag von Daniel: alle Hefte intern als einzelne .msp in %AppData%/Mitschreibprogramm/notes/, Dateiname = id, dazu Anzeigename, Zeitstempel und Scrollposition in einem Index, "index.json oder msp-header, einfachste robuste Variante".
- Entscheidung: `notes/index.json` (Format in file-format.md). Das .msp bleibt ein reines Dokument in Version 1: Export und Import laufen ohne Umbau, und Ansichtszustand wie die Scrollposition gehört nicht in die Notiz. Für die Liste braucht es alle Namen und Zeitstempel auf einmal, dafür soll beim Start nicht jede Heftdatei (bei vielen Strichen mehrere MB) geparst werden. Umbenennen und Scrollposition ändern eine kleine Datei statt das ganze Heft neu zu schreiben.
- Robustheit: Index und Hefte werden atomar geschrieben (`AtomicFile`, wie bisher die .msp). Beim Start wird der Index mit dem Ordner abgeglichen: Einträge ohne Datei fallen weg, Dateien ohne Eintrag werden mit dem Dateinamen als Name aufgenommen. Ein fehlender oder kaputter Index kostet höchstens Namen und Scrollpositionen, nie ein Heft. Der Abgleich schreibt nichts, ein schreibgeschützter Ordner lässt sich noch lesen.
- Verworfen: Name, Zeitstempel und Scrollposition im Kopf jeder .msp. Dann müsste jede Datei beim Start gelesen werden, und das Dokumentformat trüge Ansichtszustand.
- `lastOpen` steht im Index und nicht in settings.json, weil es sich auf ids des Index bezieht und der Ordner notes/ so als Ganzes kopierbar bleibt (Backup).
- Zeitstempel `modified` ändert sich nur bei Inhaltsänderungen (Strich, Radieren, Undo/Redo, Seite, Stil, Modus), nicht beim Umbenennen. Die Zeit kommt aus einem `TimeProvider` (.NET 8), die xUnit-Tests stellen ihn.
- Scrollposition in Seitenpixeln bei 100 % (Dokumentpunkt oben links im Fenster), nicht als Scroll-Offset: der Offset hängt vom Zoom ab, der beim Start immer 100 % ist. So landet man bei jedem Zoom an derselben Stelle.
- Das Änderungsflag und `Save` (Datei nur nach Änderung, Scrollposition immer) liegen in `NotebookLibrary` und nicht in der Ansicht, damit xUnit das Autosave-Verhalten ohne WPF prüft.

## 2026-09-28 Seitenleiste, Autosave und die Datei-Befehle

- Titelleiste zeigt nur den Heftnamen, kein `*` mehr: mit Autosave verlangt der Stern keine Handlung, und beim Schließen fragt nichts nach. Schlägt ein Speichern fehl, steht „(nicht gespeichert)“ im Titel, bis es wieder klappt; die Fehlerbox kommt pro Störung einmal, nicht bei jedem Tick.
- Ctrl+S bleibt als „jetzt speichern“ ohne Dialog (Gewohnheit, und der Selbsttest braucht einen festen Schreibzeitpunkt). Neu (Ctrl+N) legt ein Heft an, Öffnen wurde zu Importieren (Ctrl+O, Kopie in die Bibliothek), Speichern unter zu Exportieren (Ctrl+Umschalt+S). `UnsavedChangesDialog` ist damit tot und entfernt. `ConfirmDialog` fragt beim Löschen und beim Beenden trotz Schreibfehler; eine MessageBox hätte auf einem englischen Windows Yes/No-Knöpfe.
- Der Autosave-Tick setzt aus, solange Maus oder Stift unten sind, der nächste Tick holt es nach. Ein Speicherfehler blockiert Heftwechsel, Neu und Import, damit nichts verloren geht; Export geht weiter (das offene Heft kommt aus der Ansicht), Beenden nach Rückfrage.
- Löschen des offenen Hefts öffnet das neueste verbleibende, sonst entsteht ein neues „Unbenannt“. Gelöscht wird endgültig, deshalb die Rückfrage mit dem Heftnamen. Verworfen: Papierkorb (bräuchte Microsoft.VisualBasic oder SHFileOperation-Interop).
- Rechtsklick in der Liste wählt nicht aus (`PreviewMouseRightButtonDown` wird als erledigt markiert): WPF würde sonst das Heft öffnen, bevor das Menü aufgeht. Das Menü wirkt über den DataContext des angeklickten Eintrags.
- ListBoxItem, ContextMenu und MenuItem haben eigene kleine Vorlagen (wie ComboBox und ScrollBar), weil Aero2 feste helle Farben malt. Unter dem Namen steht die Änderungszeit, damit die Sortierung nachvollziehbar ist. `IsTextSearchEnabled` ist aus, sonst würde ein getippter Buchstabe ein Heft öffnen. Die Sichtbarkeit der Seitenleiste steht in settings.json (`notebookPanelVisible`).
- Scrollposition wiederherstellen läuft über `Dispatcher.BeginInvoke(DispatcherPriority.Loaded)`: direkt nach `Load` ist das Layout der neuen Seiten noch nicht gerechnet, und beim Start hat das Fenster noch keine Größe. Die Rechnung ist dieselbe wie beim Zoom um den Zeiger (`TranslatePoint` in beide Richtungen).
- `AtomicFile` versucht das Verschieben bis zu dreimal (50, 150, 400 ms): ein Virenscanner hält die frische Datei kurz offen, das darf kein Autosave-Fehler werden.
- Selbsttest: die alten Checkpoints starten mit ausgeblendeter Seitenleiste (settings.json), weil ihre festen Client-Koordinaten die Seitenlage ohne Panel annehmen; Checkpoint 4 startet mit Panel. selftest.ps1 startet nicht, solange eine Instanz der App läuft, weil er jede Instanz beenden würde.

## 2026-09-28 Nach dem Design-Review

- Sortierschlüssel `modified` wandert beim ersten Strich nach dem Öffnen (`MarkChanged`), nicht erst beim Speichern: so steht das Heft schon oben, bevor ein Autosave-Tick oder ein Wechsel die Liste neu aufbaut, und keine Zeile springt unter dem Zeiger weg.
- Die Bibliothek unterscheidet `LastOpen` (aus index.json, nur für den Start) von `OpenId` (das Heft, das diese Instanz wirklich geladen hat). `Save` schreibt nur nach `OpenId`; ein Heft, das sich nicht laden ließ, kann nie zum Autosave-Ziel werden. Start probiert lastOpen, dann die übrigen Hefte, zuletzt ein neues.
- Zweite Instanz: `LibraryLock` hält `notes/.lock` exklusiv, die zweite Instanz meldet sich und beendet sich in `App.OnStartup`, bevor ein Fenster entsteht (deshalb kein StartupUri mehr). Zwei Instanzen hätten sich Index und das zuletzt offene Heft gegenseitig überschrieben. Der Selbsttest hat seine eigene Bibliothek neben dem Log und ist nicht betroffen.
- Löschen schickt die Datei in den Papierkorb (`Microsoft.VisualBasic.FileIO.FileSystem`, Teil von .NET, kein Paket). Die Tests löschen über einen Konstruktor-Parameter endgültig, damit Testläufe den Papierkorb nicht füllen.
- Speichern schlägt fehl: Wechsel, Neu und Import gehen weiter, wenn nur der Index betroffen war (das Heft liegt auf der Platte), und fragen sonst „Trotzdem wechseln?“; Beenden fragt „Trotzdem beenden?“. Vorher blockierte jeder Fehler den Wechsel.
- Die Heftliste nimmt keinen Tastaturfokus (`Focusable=False`, Klicks im Preview-Handler): das Fenster sieht weiter jede Taste, Pfeiltasten oder Buchstaben können kein Heft wechseln. UI Automation öffnet weiter über die Auswahl.
- index.json wird beim Laden repariert: unendliche oder absurde Scrollwerte werden 0 (Infinity ließe sich nicht zurückschreiben, die App käme nicht mehr hoch), eigene Dateien ohne Eintrag heißen „Wiederhergestellt <Datum>“ statt GUID.
- `AtomicFile` wiederholt nur bei kurz gesperrter Datei (Sharing- oder Lock-Violation, UnauthorizedAccess), nicht bei voller Platte oder fehlendem Ordner.
- Verworfen: Speichern im Hintergrund-Thread. Bei sehr großen Heften dauert Serialisieren plus Schreiben 100 bis 300 ms auf dem UI-Thread; der Tick setzt während eines Strichs aus, die Nassschrift läuft ohnehin auf dem Stift-Thread. Der Mehraufwand (Reihenfolge mit Wechsel und Beenden, Fehlerpfade) lohnt sich erst, wenn es spürbar wird.
- Die Änderungszeit unter dem Heftnamen steht nicht in der Spezifikation. Sie bleibt, weil sie die verlangte Sortierung nachvollziehbar macht: eine Zeile im ItemTemplate, jederzeit streichbar.

## 2026-09-28 Nach dem Code-Review

- Striche, die entstehen, während kein Heft offen ist (Ordner beim ersten Start nicht beschreibbar, oder nach dem Löschen ließ sich kein Heft öffnen), gehen nicht verloren: die Session merkt sie sich (`_orphanChanges`), und der nächste erfolgreiche Speichervorgang legt daraus ein Heft „Unbenannt“ an. Bis dahin gelten sie als ungespeichert (Titel, Rückfrage beim Beenden). Verworfen: die Fläche in dem Zustand sperren, das hätte den Benutzer mitten im Schreiben ausgebremst.
- MSP-Export in den Bibliotheksordner selbst wird abgelehnt (`NotebookLibrary.Contains`): dort hätte er ein anderes Heft oder index.json überschrieben.
- Speicher und Platte bleiben gleich: `Rename` nimmt den Namen zurück, wenn der Index nicht geschrieben werden konnte; nach einem Löschen richtet sich die Ansicht danach, ob der Eintrag noch existiert, nicht nach dem Rückgabewert. Ids aus der Seitenleiste, die die Bibliothek nicht mehr kennt, bauen die Liste neu auf statt zu werfen (`Find` statt `Entry`).
- Ein Heft ohne gemerkte Position (Scrollwerte 0/0, nur bei nie verlassenen Heften) startet ganz oben mit sichtbarem Rand; sonst läge die Seitenkante exakt an der Fensterkante.
- Selbsttest-Skripte bleiben reines ASCII: Windows PowerShell 5.1 liest Dateien ohne BOM in der ANSI-Codepage, ein „ö“ im Skript käme als „Ã¶“ an. Umlaute werden mit `[char]0xF6` gebaut. `-Exists` gibt wie die anderen Schalter Text aus („True“/„False“), weil `$false -eq "False"` in PowerShell falsch ist.
- Die Zwischencommits caa826f bis 1e4c9b7 hatten die Löschung von FileSession zu früh enthalten (bauten nicht); die lokale, nie gepushte Historie wurde dafür einmal neu geschrieben, damit jeder Commit baut.
- Der Selbsttest liest das Namensfeld der Windows-Dialoge per WM_GETTEXT (GetWindowText kommt an Edit-Felder fremder Prozesse nicht heran) und erneuert die Auswahl mit Strg+A, weil das Auslesen sie aufhebt. Ein verlorener injizierter Strich wird einmal wiederholt und als RETRY gemeldet; das ist eine Eigenheit der Injektion, kein App-Verhalten.
