# Pen-Input

## Hardware-Fakten

- Tablet: One by Wacom (Medium), CTL-672, USB. Aktive Fläche ca. 216 x 135 mm, per Treiber auf den Bildschirm gemappt. Quelle: Wacom eStore, Produktseite CTL-672.
- Stift: der Stift der One by Wacom, 2048 Druckstufen, zwei Seitentasten, kein Tilt, kein Radierer am Stiftende.
- Keine ExpressKeys.
- Druckstufen spielen für den Code keine Rolle: WPF liefert den Druck normiert als `StylusPoint.PressureFactor` 0..1.
- Mögliche Pen-Inputs für die App: Position, Druck, Seitentaste 1, Seitentaste 2, Hover. Sonst nichts.
- Tilt und Rotation werden nie implementiert.

## Windows Ink

- Druck kommt in WPF nur an, wenn im Wacom-Treiber "Windows Ink verwenden" aktiv ist. Ohne Windows Ink sieht die App nur Maus-Events (konstante Breite, Seitentaste = Maustaste, kein Radieren per Taste).

## WPF-Verhalten (Quelle: dotnet/wpf, Branch release/8.0)

- Stack: Die App nutzt WPFs Standard-Stylus-Stack (WISP, penimc). Der Pointer-Stack (`Switch.System.Windows.Input.Stylus.EnablePointerSupport`) ist nicht aktiv und nicht nötig, siehe decisions.md.
- Druck zu Breite: `StrokeNodeIterator.GetNormalizedPressureFactor` (Datei `PresentationCore/MS/internal/Ink/StrokeNodeEnumerator.cs`): Faktor = 1,5 x Druck + 0,25. Druck 0..1 ergibt 0,25x bis 1,75x der eingestellten Breite. Der PDF-Export rechnet mit derselben Formel.
- Maus: Punkte bekommen `StylusPoint.DefaultPressure` = 0,5, also Faktor 1,0. Ein Mausstrich ist exakt so breit wie eingestellt und konstant.
- Checkbox "Druck" aus = `DrawingAttributes.IgnorePressure = true`. Gilt pro Strich (InkCanvas klont die Attribute beim Strichbeginn).
- Seitentaste (Barrel): InkCanvas wertet sie selbst nie aus (`EditingCoordinator.OnInkCanvasDeviceDown` prüft keine Buttons). Die App liest deshalb `StylusDevice.StylusButtons` (Guid `StylusPointProperties.BarrelButton`) in PreviewStylusInRange, -InAirMove, -ButtonDown/Up und -Down und schaltet auf Select (Lasso), solange die Taste unten ist (bis 2026-09-28: EraseByStroke). PreviewStylusDown läuft vor dem Handler von InkCanvas, der Zustand ist dort schon aktuell.
- StylusButtonDown/Up kommen auch im Hover (über InAirMove-Pakete). Wechselt die Taste im selben Paket wie das Aufsetzen, kommt das Button-Event erst nach StylusDown, deshalb zusätzlich der Check in PreviewStylusDown.
- Invertierter Stift: `InkCanvas.EditingModeInverted` steht auf EraseByStroke. InkCanvas schaltet bei InRange, InAirMove und Down selbst um. Meldet der Wacom-Treiber die Seitentaste als "Radieren", kommt sie als invertiert an und radiert ohne eigenen Code.
- Moduswechsel im Hover ist sicher. Moduswechsel mitten im Strich (Seitentaste während des Schreibens drücken) verwirft den laufenden Strich.
- Press-and-Hold: `Stylus.IsPressAndHoldEnabled = false` auf dem InkCanvas, sonst verzögert Windows jeden Strichbeginn für die Rechtsklick-Erkennung. Flicks und Tap-Feedback sind ebenfalls aus.
- Seitentaste + Aufsetzen wird von WPF zusätzlich als rechte Maustaste hochgestuft (`WispStylusDevice`, SystemGesture RightTap/RightDrag). InkCanvas ignoriert Nicht-Links-Klicks, das stört nicht. Das Lasso läuft über die Stylus-Ereignisse und funktioniert mit gehaltener Taste. Verschieben einer Auswahl macht InkCanvas immer über die Maus („We always use MouseDevice for the selection editing“), mit gehaltener Taste also nur als Rechtsklick: zum Ziehen die Taste loslassen.

## Zwei Seitentasten unterscheiden (Recherche 2026-09-29, dotnet/wpf release/8.0)

- WPF kann die beiden Tasten physisch nicht unterscheiden. `StylusPointPropertyIds` kennt TipButton, BarrelButton und SecondaryTipButton (HID 0x43, ein zweiter Spitzen-Schalter, keine zweite Seitentaste). Die Windows-Pointer-API hat nur PEN_FLAG_BARREL (1), PEN_FLAG_INVERTED (2) und PEN_FLAG_ERASER (4), kein Flag für eine zweite Seitentaste.
- Unterscheidbar ist nur, was der Wacom-Treiber aus einer Taste macht: Belegung „Rechtsklick“ kommt als BarrelButton an (App: Lasso), Belegung „Radieren“ schaltet den Stift in den Radiermodus und kommt als `StylusDevice.Inverted` an (App: `EditingModeInverted` = EraseByStroke). Deshalb: obere Taste „Rechtsklick“, untere Taste „Radieren“ (README).
- Ein Wechsel des Inverted-Zustands schaltet InkCanvas intern um und hebt dabei eine offene Auswahl auf (`EditingCoordinator.UpdateInvertedState`). Wer mit der unteren Taste radiert, beendet also eine Auswahl.

## InkCanvas-Auswahl (Select-Modus, Recherche 2026-09-29)

- `EditingMode` nie aus `SelectionChanged` heraus ändern, solange InkCanvas ein Lasso abschließt: das Capture besteht dann noch (`IsInMidStroke`), das aktive Verhalten ist schon der Auswahl-Editor, und `UpdateEditingState` wirft beim Cast auf `StylusEditingBehavior` eine `InvalidCastException` (Absturz 2026-09-30). `EditingModes` wendet solche Wechsel deshalb per Dispatcher nach dem Abheben an.
- Wechselt der Modus mitten in einem Lasso weg von Select (Seitentaste kurz vor dem Abheben losgelassen), verwirft WPF das Lasso (`LassoSelectionBehavior.OnSwitchToMode`, `Commit(false)`). `SideButtonWatcher` zählt ein Loslassen der Taste deshalb erst nach dem Abheben.
- Jeder Wechsel von `EditingMode` hebt die Auswahl auf (`EditingCoordinator.ChangeEditingBehavior` ruft `ClearSelection(true)`). Deshalb bleiben die Seiten im Select-Modus, solange etwas ausgewählt ist (`EditingModes`). `InkCanvas.Select(...)` schaltet selbst in den Select-Modus.
- Lasso: ein Strich gilt als gewählt, wenn 80 % seiner Länge im Lasso liegen, ein Element bei 60 % seiner Fläche (Punktraster). Tippen (weniger als 7 Einheiten Bewegung) wählt den obersten Strich oder das Element unter dem Punkt, Tippen auf leere Fläche hebt die Auswahl auf, Ziehen außerhalb startet ein neues Lasso.
- SelectionMoving / SelectionResizing kommen einmal beim Loslassen, `NewRectangle` ist setzbar und wird übernommen, `Cancel` wirkt. Striche werden mit `Stroke.Transform(matrix, false)` bewegt: die Punkte ändern sich an Ort und Stelle, die Stiftspitze (Breite) nie. Elemente bekommen InkCanvas.Left/Top und Width/Height neu (nur die geänderte Seite bei Kantengriffen, Margin wird abgezogen, Bilder haben keinen).
- Ist genau ein Element und kein Strich gewählt, lässt der Auswahlrahmen über dem Element ein Loch (für Textfelder gedacht): Klicks gehen an das Element, verschieben geht nur am Rahmen. Die App zieht ein allein gewähltes Bild deshalb selbst (`ImageDrag`).
- Tippen im Select-Modus (Messung 2026-09-29, synthetischer Stift mit 60 bis 80 ms Kontakt): mit der Spitze wählt InkCanvas das Bild unter dem Stift auch bei 10 und 14 Einheiten Bewegung, bei 20 nicht mehr. Mit gedrückter Seitentaste wählt schon ein Tippen mit 10 Einheiten Bewegung nichts aus. `TapSelection` holt das nach: bleibt die Auswahl nach einem Stift-Tippen (bis `AppConstants.TapTolerance` = 12 Fenstereinheiten) unverändert, wählt es wie InkCanvas beim Mausklick den obersten Strich in 5 Einheiten Reichweite, sonst das oberste Bild, oder hebt die Auswahl auf, wenn daneben getippt wurde.
- Testwerkzeug: ein injiziertes Tippen, bei dem direkt auf das Aufsetzen das Abheben folgt (keine Kontakt-Pakete dazwischen), kommt oft gar nicht bei WPF an. Ein echter Stift liefert auch beim ruhigen Tippen mehrere Pakete; der Selbsttest tippt deshalb mit 70 ms Kontakt.
- Tastenbefehle (Entf, Esc) bindet InkCanvas nur mit Tastaturfokus. Die Seiten sind nicht fokussierbar, die App erledigt Entf und Esc in ihrer Shortcut-Tabelle.
- Tinte über Bildern: die Striche liegen über den Kind-Elementen (`InkPresenter.GetVisualChild`: 0 = Kinder, 1 = Striche), Schreiben auf einem Bild geht ohne Einstellung.

## Synthetischer Pen (Selbsttest)

- API: `CreateSyntheticPointerDevice(PT_PEN = 3, 1, POINTER_FEEDBACK_DEFAULT = 1)`, `InjectSyntheticPointerInput`, `DestroySyntheticPointerDevice` (user32, Windows 10 1809+, kein Admin).
- Layout x64: POINTER_INFO 96 Bytes, POINTER_PEN_INFO 120, POINTER_TOUCH_INFO 144, POINTER_TYPE_INFO 152 (type bei 0, Union bei Offset 8). In C# Explicit-Layout, Touch-Member nur als Größen-Padding.
- Sequenz: Hover `INRANGE|UPDATE`, Aufsetzen `INRANGE|INCONTACT|DOWN`, Bewegung `INRANGE|INCONTACT|UPDATE`, Abheben `INRANGE|UP`, Bereich verlassen `UPDATE` allein.
- penFlags: BARREL = 1 (Seitentaste, schon im Hover setzen), INVERTED = 2, ERASER = 4 (nur mit Kontakt). Der Stift muss schon invertiert in den Bereich kommen. penMask PRESSURE = 1, Druck 0..1024.
- Koordinaten sind physische Pixel. PowerShell 5.1 ist DPI-unaware, deshalb `SetThreadDpiAwarenessContext(-4)` im selben nativen Aufruf wie die Injektion.
- Artefakte der Injektion (2026-09-29), kein Verhalten echter Geräte: ein Stift-Tippen ohne Kontakt-Pakete zwischen Aufsetzen und Abheben kommt oft nicht an, deshalb liegen Test-Tipps 60 bis 80 ms auf. Ein Mausklick ohne jede Mausbewegung genau an der Stelle, an der der Stift den Cursor gelassen hat, kommt ebenfalls nicht an (20 Einheiten daneben immer). Test-Klicks deshalb nie auf den letzten Stiftpunkt.
- Befund 2026-09-19: WPFs Standard-Stack liefert die Injektion als echten Stylus mit Druck, Barrel und Inverted (Debug-Log `device=stylus pmin=0.098 pmax=0.977`).
- Sicherung: `tools/MspNative.*.cs` injiziert nur, wenn der Zielpixel zum App-Prozess gehört (`RequireAppAt` mit `WindowFromPoint`). pen-sim.ps1 und app-control.ps1 holen das Fenster vorher mit `Activate` in den Vordergrund. Ohne das landet der Strich im Fenster darüber und öffnet die Bildschirmtastatur.

## Treiber-Fallstricke

- Die App kann obere und untere Seitentaste nicht selbst unterscheiden, nur über die Treiber-Belegung: obere Taste „Rechtsklick“ = Lasso, untere Taste „Radieren“ = Radierer. Liegt auf beiden „Rechtsklick“, sind beide Lasso; liegt auf beiden „Radieren“, radieren beide.
- Windows-Einstellung "Gedrückt halten für Rechtsklick" (Stift-Einstellungen) wirkt systemweit. Die App schaltet sie für die Schreibfläche ab, in der README steht der Hinweis trotzdem.
