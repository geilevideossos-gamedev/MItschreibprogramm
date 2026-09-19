# Pen-Input

## Hardware-Fakten

- Tablet: Wacom Intuos Small, CTL-4100K-S, USB. Aktive Fläche ca. 152 x 95 mm, per Treiber auf den Bildschirm gemappt.
- Stift: Wacom Pen 4K, 4096 Druckstufen, zwei Seitentasten, kein Tilt, kein Radierer am Stiftende.
- 4 ExpressKeys, vom Treiber als Tastendrücke gesendet. Die App sieht sie als normale Tastatur.
- Mögliche Pen-Inputs für die App: Position, Druck, Seitentaste 1, Seitentaste 2, Hover. Sonst nichts.
- Tilt und Rotation werden nie implementiert.

## Windows Ink

- Druck kommt in WPF nur an, wenn im Wacom-Treiber "Windows Ink verwenden" aktiv ist. Ohne Windows Ink sieht die App nur Maus-Events (konstante Breite, Seitentaste = Maustaste, kein Radieren per Taste).

## WPF-Verhalten (Quelle: dotnet/wpf, Branch release/8.0)

- Stack: Die App nutzt WPFs Standard-Stylus-Stack (WISP, penimc). Der Pointer-Stack (`Switch.System.Windows.Input.Stylus.EnablePointerSupport`) ist nicht aktiv und nicht nötig, siehe decisions.md.
- Druck zu Breite: `StrokeNodeIterator.GetNormalizedPressureFactor` (Datei `PresentationCore/MS/internal/Ink/StrokeNodeEnumerator.cs`): Faktor = 1,5 x Druck + 0,25. Druck 0..1 ergibt 0,25x bis 1,75x der eingestellten Breite. Der PDF-Export rechnet mit derselben Formel.
- Maus: Punkte bekommen `StylusPoint.DefaultPressure` = 0,5, also Faktor 1,0. Ein Mausstrich ist exakt so breit wie eingestellt und konstant.
- Checkbox "Druck" aus = `DrawingAttributes.IgnorePressure = true`. Gilt pro Strich (InkCanvas klont die Attribute beim Strichbeginn).
- Seitentaste (Barrel): InkCanvas wertet sie selbst nie aus (`EditingCoordinator.OnInkCanvasDeviceDown` prüft keine Buttons). Die App liest deshalb `StylusDevice.StylusButtons` (Guid `StylusPointProperties.BarrelButton`) in PreviewStylusInRange, -InAirMove, -ButtonDown/Up und -Down und schaltet auf EraseByStroke, solange die Taste unten ist. PreviewStylusDown läuft vor dem Handler von InkCanvas, der Zustand ist dort schon aktuell.
- StylusButtonDown/Up kommen auch im Hover (über InAirMove-Pakete). Wechselt die Taste im selben Paket wie das Aufsetzen, kommt das Button-Event erst nach StylusDown, deshalb zusätzlich der Check in PreviewStylusDown.
- Invertierter Stift: `InkCanvas.EditingModeInverted` steht auf EraseByStroke. InkCanvas schaltet bei InRange, InAirMove und Down selbst um. Meldet der Wacom-Treiber die Seitentaste als "Radieren", kommt sie als invertiert an und radiert ohne eigenen Code.
- Moduswechsel im Hover ist sicher. Moduswechsel mitten im Strich (Seitentaste während des Schreibens drücken) verwirft den laufenden Strich.
- Press-and-Hold: `Stylus.IsPressAndHoldEnabled = false` auf dem InkCanvas, sonst verzögert Windows jeden Strichbeginn für die Rechtsklick-Erkennung. Flicks und Tap-Feedback sind ebenfalls aus.
- Seitentaste + Aufsetzen wird von WPF zusätzlich als rechte Maustaste hochgestuft. InkCanvas ignoriert Nicht-Links-Klicks, das stört nicht.

## Synthetischer Pen (Selbsttest)

- API: `CreateSyntheticPointerDevice(PT_PEN = 3, 1, POINTER_FEEDBACK_DEFAULT = 1)`, `InjectSyntheticPointerInput`, `DestroySyntheticPointerDevice` (user32, Windows 10 1809+, kein Admin).
- Layout x64: POINTER_INFO 96 Bytes, POINTER_PEN_INFO 120, POINTER_TOUCH_INFO 144, POINTER_TYPE_INFO 152 (type bei 0, Union bei Offset 8). In C# Explicit-Layout, Touch-Member nur als Größen-Padding.
- Sequenz: Hover `INRANGE|UPDATE`, Aufsetzen `INRANGE|INCONTACT|DOWN`, Bewegung `INRANGE|INCONTACT|UPDATE`, Abheben `INRANGE|UP`, Bereich verlassen `UPDATE` allein.
- penFlags: BARREL = 1 (Seitentaste, schon im Hover setzen), INVERTED = 2, ERASER = 4 (nur mit Kontakt). Der Stift muss schon invertiert in den Bereich kommen. penMask PRESSURE = 1, Druck 0..1024.
- Koordinaten sind physische Pixel. PowerShell 5.1 ist DPI-unaware, deshalb `SetThreadDpiAwarenessContext(-4)` im selben nativen Aufruf wie die Injektion.
- Befund 2026-09-19: WPFs Standard-Stack liefert die Injektion als echten Stylus mit Druck, Barrel und Inverted (Debug-Log `device=stylus pmin=0.098 pmax=0.977`).
- Sicherung: `tools/MspNative.*.cs` injiziert nur, wenn der Zielpixel zum App-Prozess gehört (`RequireAppAt` mit `WindowFromPoint`). pen-sim.ps1 und app-control.ps1 holen das Fenster vorher mit `Activate` in den Vordergrund. Ohne das landet der Strich im Fenster darüber und öffnet die Bildschirmtastatur.

## Treiber-Fallstricke

- Die App kann obere und untere Seitentaste nicht unterscheiden. Windows Ink kennt nur ein Barrel-Flag. Welche Taste radiert, legt der Wacom-Treiber fest (Belegung "Rechtsklick" = Barrel oder "Radieren" = invertiert, beides funktioniert).
- Windows-Einstellung "Gedrückt halten für Rechtsklick" (Stift-Einstellungen) wirkt systemweit. Die App schaltet sie für die Schreibfläche ab, in der README steht der Hinweis trotzdem.
