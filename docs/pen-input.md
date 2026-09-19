# Pen-Input

## Hardware-Fakten

- Tablet: Wacom Intuos Small, CTL-4100K-S, USB. Aktive Fläche ca. 152 x 95 mm, per Treiber auf den Bildschirm gemappt.
- Stift: Wacom Pen 4K, 4096 Druckstufen, zwei Seitentasten, kein Tilt, kein Radierer am Stiftende.
- 4 ExpressKeys, vom Treiber als Tastendrücke gesendet. Die App sieht sie als normale Tastatur.
- Mögliche Pen-Inputs für die App: Position, Druck, Seitentaste 1, Seitentaste 2, Hover. Sonst nichts.
- Tilt und Rotation werden nie implementiert.

## Windows Ink

- Druck kommt in WPF nur an, wenn im Wacom-Treiber "Windows Ink verwenden" aktiv ist. Ohne Windows Ink sieht die App nur Maus-Events (konstante Breite).

## WPF-Verhalten

(wird in Schritt 2 mit Quellen gefüllt: Seitentaste, Inverted, Druckformel, Press-and-Hold)

## Synthetischer Pen (Selbsttest)

(wird in Schritt 2 gefüllt: Structs, Flags, Fallstricke von InjectSyntheticPointerInput)

## Treiber-Fallstricke

(wird laufend ergänzt)
