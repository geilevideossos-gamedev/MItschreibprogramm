# Dot-sourced by tools/selftest.ps1, uses its helper functions.
function Checkpoint2 {
    Write-Output "Checkpoint 2: Hintergrund, Zoom, Pan, Undo/Redo"
    Start-App
    Shot "cp2-lined-blue"
    Ctl -Click LineColorButton
    Shot "cp2-lined-black"
    Ctl -Keys "ctrl+l"
    Shot "cp2-squared-black"
    Ctl -Click LineColorButton
    Shot "cp2-squared-blue"
    $ruled = Get-VerticalLineCount "cp2-lined-black"
    $gridBlack = Get-VerticalLineCount "cp2-squared-black"
    $gridBlue = Get-VerticalLineCount "cp2-squared-blue"
    Check ($ruled -le 2 -and $gridBlack -ge 30 -and $gridBlack -le 45 -and $gridBlue -ge 30 -and $gridBlue -le 45) "Kariert zeigt senkrechte Gitterlinien in beiden Linienfarben, Liniert keine (liniert $ruled, kariert schwarz $gridBlack, blau $gridBlue)"
    Ctl -Click LineColorButton
    Ctl -Keys "ctrl+l"
    Shot "cp2-blank"
    Check ((Get-VerticalLineCount "cp2-blank") -le 2) "Blanko hat keine Linien"
    Ctl -Keys "ctrl+l"

    Pen -From "400,300" -To "700,300"
    $a = @(Read-Log)[0]
    Ctl -Keys "ctrl+z"
    Shot "cp2-undo"
    $undo = @(Read-Log)
    Check ($undo.Count -eq 1 -and $undo[0].kind -eq "undo" -and $undo[0].strokes -eq "0") "Ctrl+Z entfernt den Strich"
    Ctl -Keys "ctrl+y"
    $redo = @(Read-Log)
    Check ($redo.Count -eq 1 -and $redo[0].kind -eq "redo" -and $redo[0].strokes -eq "1") "Ctrl+Y stellt ihn wieder her"

    foreach ($i in 1..7) { Ctl -Wheel "400,300,120" -Hold ctrl }
    $zoomText = Ctl -Read ZoomText
    Pen -From "400,300" -To "700,300"
    $b = @(Read-Log)[0]
    $a0 = PointOf $a.first; $b0 = PointOf $b.first; $b1 = PointOf $b.last
    $zoom = Number $b.zoom
    Check ($zoom -gt 1.9 -and $zoom -lt 2.0) "Ctrl+Mausrad zoomt (7 Stufen = $($b.zoom), Anzeige '$zoomText')"
    # At 100 % the page is narrower than the viewport and stays centred, so only Y can be anchored here.
    Check ([Math]::Abs($a0[1] - $b0[1]) -lt 1.5) "Zoom um die Mausposition, vertikal: Punkt unter dem Zeiger bleibt stehen ($($a.first) -> $($b.first))"
    Check ([Math]::Abs(($b1[0] - $b0[0]) * $zoom - 300) -lt 2) "Strichlaenge skaliert mit dem Zoom (300 px Bildschirm = $([Math]::Round($b1[0] - $b0[0], 1)) Seitenpixel)"
    Shot "cp2-zoom-wheel"

    Ctl -Keys "ctrl+0"
    Ctl -Keys "ctrl+plus"; Ctl -Keys "ctrl+plus"; Ctl -Keys "ctrl+plus"
    Check ((Ctl -Read ZoomText) -match "200") "Ctrl+0 und 3x Ctrl+Plus = 200 %"
    Pen -From "400,400" -To "700,400"
    $c = @(Read-Log)[0]
    $c0 = PointOf $c.first; $c1 = PointOf $c.last
    Check ($c.zoom -eq "2" -and [Math]::Abs(($c1[0] - $c0[0]) - 150) -lt 1.5) "Strich bei 200 %: 300 px Bildschirm = 150 Seitenpixel ($($c.first) -> $($c.last))"
    Shot "cp2-zoom-200"
    foreach ($i in 1..3) { Ctl -Wheel "400,400,120" -Hold ctrl }
    Pen -From "400,400" -To "700,400"
    $w0 = PointOf (@(Read-Log)[0]).first
    Check ([Math]::Abs($w0[0] - $c0[0]) -lt 1.5 -and [Math]::Abs($w0[1] - $c0[1]) -lt 1.5) "Zoom um die Mausposition, beide Achsen ab 200 %: ($($c.first) -> ($($w0[0]),$($w0[1])))"
    foreach ($i in 1..3) { Ctl -Wheel "400,400,-120" -Hold ctrl }
    Pen -From "400,400" -To "700,400"
    $c0 = PointOf (@(Read-Log)[0]).first

    Ctl -Drag "middle,600,500,600,350"
    Pen -From "400,400" -To "700,400"
    $d0 = PointOf (@(Read-Log)[0]).first
    Check ([Math]::Abs(($d0[1] - $c0[1]) - 75) -lt 1.5) "Mittlere Maustaste zieht die Ansicht (150 px = 75 Seitenpixel)"
    Ctl -Drag "left,600,500,500,500" -Hold space
    Pen -From "400,400" -To "700,400"
    $e0 = PointOf (@(Read-Log)[0]).first
    Check ([Math]::Abs(($e0[0] - $d0[0]) - 50) -lt 1.5 -and [Math]::Abs($e0[1] - $d0[1]) -lt 1.5) "Leertaste + Ziehen verschiebt horizontal, ohne zu zeichnen"
    Ctl -Wheel "600,500,-120" -Hold shift
    Pen -From "400,400" -To "700,400"
    $f0 = PointOf (@(Read-Log)[0]).first
    Check ($f0[0] -gt $e0[0] + 10 -and [Math]::Abs($f0[1] - $e0[1]) -lt 1.5) "Shift+Mausrad scrollt horizontal"
    Ctl -Wheel "600,500,-120"
    Pen -From "400,400" -To "700,400"
    $g0 = PointOf (@(Read-Log)[0]).first
    Check ($g0[1] -gt $f0[1] + 10 -and [Math]::Abs($g0[0] - $f0[0]) -lt 1.5) "Mausrad scrollt vertikal"
    Ctl -Keys "ctrl+minus"
    Check ((Ctl -Read ZoomText) -match "150") "Ctrl+Minus = 150 %"
    Stop-App
}
