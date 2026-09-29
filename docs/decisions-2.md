# Entscheidungen (Fortsetzung)

Fortsetzung von decisions.md, gleiche Regeln: chronologisch, je Eintrag Entscheidung, Begründung, verworfene Alternative.

## 2026-09-29 Hardware-Korrektur: One by Wacom CTL-672

- Das Tablet ist kein Intuos Small, sondern ein One by Wacom (Medium) CTL-672: aktive Fläche ca. 216 x 135 mm, 2048 Druckstufen, zwei Seitentasten, kein Tilt, keine ExpressKeys. Korrigiert in CLAUDE.md, pen-input.md, README, testing.md.
- Am Code ändert das nichts: WPF normiert den Druck auf 0..1, die Stufenzahl spielt keine Rolle. Die Shortcuts bleiben Einzeltasten.
- Der ExpressKeys-Abschnitt ist aus der README und aus "Offen mit echtem Pen" entfernt. mega-prompt-claude-code-start.md bleibt als ursprünglicher Auftrag unverändert, features.md F35 und F41 tragen einen Hinweis.

## 2026-09-29 Änderungszeit unter dem Heftnamen bleibt, relativ

- Daniel: anzeigen, kurz und relativ. Format `heute 10:04`, `gestern`, sonst `28.09.`, bei einem anderen Jahr `31.12.2025`. Rechnung in `Services/RelativeDate` (xUnit), Anzeige über `RelativeDateConverter`.
- Die Zeile wird beim Neuaufbau der Liste berechnet (jeder Wechsel, jedes Speichern mit Änderung). Bleibt die App über Mitternacht offen, steht bis dahin noch „heute“. Verworfen: eigener Timer nur dafür.
