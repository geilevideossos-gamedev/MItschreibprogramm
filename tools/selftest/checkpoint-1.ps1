# Dot-sourced by tools/selftest.ps1, uses its helper functions.
function Checkpoint1 {
    Write-Output "Checkpoint 1: Pen, Druck, Seitentaste, invertiert, E/P, Maus"
    Start-App
    Pen -From "300,300" -To "700,300" -PressureFrom 100 -PressureTo 1000
    $s = @(Read-Log)
    Check ($s.Count -eq 1 -and $s[0].device -eq "stylus" -and $s[0].mode -eq "ink") "Synthetischer Pen kommt als Stylus an"
    Check ((Number $s[0].pmin) -lt 0.2 -and (Number $s[0].pmax) -gt 0.9) "Druckverlauf 100..1000 kommt an (pmin=$($s[0].pmin) pmax=$($s[0].pmax))"
    Shot "cp1-pressure"
    & "$toolsDir/pen-sim.ps1" -Path "280,270;720,270;720,330;280,330;280,272" -Barrel | Out-Null
    Start-Sleep -Milliseconds 400
    $s = @(Read-Log)
    Check ($s.Count -ge 1 -and $s[-1].kind -eq "selection" -and $s[-1].strokes -eq "1" -and @($s | Where-Object { $_.kind -eq "stroke" }).Count -eq 0) "Gehaltene obere Seitentaste zieht ein Lasso und waehlt den Strich, ohne zu zeichnen"
    Ctl -Keys "esc"
    Read-Log | Out-Null
    Pen -From "300,450" -To "700,450"
    Pen -From "500,400" -To "500,500" -Inverted
    $s = @(Read-Log)
    Check ($s.Count -eq 2 -and $s[1].mode -eq "erase" -and $s[1].device -eq "stylus-inverted") "Invertierter Stift radiert"
    Pen -From "300,600" -To "700,600"
    Ctl -Keys "e"
    Pen -From "500,550" -To "500,650"
    $s = @(Read-Log)
    Check ($s.Count -eq 2 -and $s[1].mode -eq "erase" -and $s[1].barrel -eq "False") "Taste E schaltet den Radierer ein"
    Ctl -Keys "p"
    Ctl -Drag "left,300,200,700,200"
    $s = @(Read-Log)
    Check ($s.Count -eq 1 -and $s[0].device -eq "mouse" -and $s[0].pmin -eq $s[0].pmax) "Taste P = Stift, Maus zeichnet mit konstanter Breite"
    Ctl -Click PressureCheck
    Pen -From "300,260" -To "700,260" -PressureFrom 100 -PressureTo 1000
    $s = @(Read-Log)
    Check ($s.Count -eq 1 -and $s[0].pressure -eq "False") "Checkbox Druck aus: Strich ignoriert den Druck"
    Shot "cp1-final"
    Stop-App
}
