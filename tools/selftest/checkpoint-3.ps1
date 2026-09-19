# Dot-sourced by tools/selftest.ps1, uses its helper functions.
function Checkpoint3 {
    Write-Output "Checkpoint 3: Seitenmodell Seiten / Endlos"
    Start-App
    Ctl -Keys "ctrl+minus"; Ctl -Keys "ctrl+minus"
    Check ((Ctl -Read PageText) -eq "Seite 1 von 1") "Start im Seitenmodus mit einer Seite"
    $origin = Get-PageOrigin 500 200
    PagePen $origin 100 1040 400 1040
    $s = @(Read-Log)[-1]
    Check ($s.page -eq "1" -and (PointOf $s.first)[1] -gt 954) "Strich in den unteren 15 % der letzten Seite ($($s.first))"
    Check ((Ctl -Read PageText) -eq "Seite 1 von 2") "Auto-Seite: danach gibt es 2 Seiten"
    Shot "cp3-pages-auto"
    Ctl -Keys "ctrl+enter"
    Check ((Ctl -Read PageText) -match "von 3$") "Ctrl+Enter haengt eine Seite an ($(Ctl -Read PageText))"
    Ctl -Click AddPageButton
    Check ((Ctl -Read PageText) -match "von 4$") "Button '+ Seite' haengt eine Seite an"
    $second = Get-PageOrigin 500 400
    Check ([int]$second.Page -ge 2) "Strich auf einer hinteren Seite wird dieser Seite zugeordnet (page=$($second.Page))"
    Shot "cp3-pages"

    Ctl -Click PageModeButton
    Check ((Ctl -Read PageText) -eq "Endlos") "Umschalten auf Endlos"
    Shot "cp3-endless"
    Ctl -Keys "ctrl+z"
    $undo = @(Read-Log)[-1]
    Check ((Ctl -Read PageText) -match "von 4$" -and $undo.strokes -eq "3") "Ctrl+Z nimmt den Moduswechsel zurueck, alle 3 Striche bleiben"
    Ctl -Keys "ctrl+y"
    Check ((Ctl -Read PageText) -eq "Endlos") "Ctrl+Y stellt Endlos wieder her"

    Ctl -Keys "ctrl+0"; Ctl -Keys "ctrl+minus"; Ctl -Keys "ctrl+minus"
    $surface = Get-PageOrigin 500 300
    $visibleY = (340 - $surface.Y) / $surface.Zoom
    PagePen $surface 600 $visibleY 760 $visibleY
    Read-Log | Out-Null
    $grown = Get-PageOrigin 500 300
    $visibleY = (380 - $grown.Y) / $grown.Zoom
    PagePen $grown 900 $visibleY 1000 $visibleY
    $wide = @(Read-Log)[-1]
    Check ((PointOf $wide.first)[0] -gt 890) "Endlos waechst nach rechts: Strich jenseits der A4-Breite moeglich ($($wide.first))"
    Shot "cp3-endless-grown"
    Ctl -Click PageModeButton
    Check ((Ctl -Read PageText) -match "von \d+$") "Zurueck in den Seitenmodus ($(Ctl -Read PageText))"
    Shot "cp3-back-to-pages"
    Stop-App
}
