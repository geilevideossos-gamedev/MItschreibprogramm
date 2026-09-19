# Dot-sourced by tools/selftest.ps1, uses its helper functions.
function Checkpoint4 {
    Write-Output "Checkpoint 4: Datei, Titel, Schliessen-Dialog"
    $file = Join-Path $outDir "cp4.msp"
    Remove-Item $file -ErrorAction SilentlyContinue
    Start-App
    Check ((Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm") "Neues Dokument heisst Unbenannt, ohne Stern"
    Ctl -Keys "2"
    Pen -From "300,300" -To "700,340" -PressureFrom 200 -PressureTo 900
    Pen -From "300,420" -To "700,380"
    $drawn = @(Read-Log)
    Check ((Ctl -Read Window) -eq "Unbenannt* - Mitschreibprogramm") "Nach dem Zeichnen steht der Stern im Titel"
    Ctl -Keys "ctrl+s"
    Start-Sleep -Milliseconds 1200
    Ctl -Text $file
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    Check ((Ctl -Read Window) -eq "cp4.msp - Mitschreibprogramm" -and (Ctl -Read FileText) -eq $file) "Ctrl+S speichert ueber den Dialog, Titel und Statusleiste zeigen die Datei"
    $json = Get-Content $file -Raw | ConvertFrom-Json
    $firstPoint = $json.pages[0].strokes[0].points[0]
    $logged = PointOf $drawn[0].first
    Check ($json.version -eq 1 -and $json.pages[0].strokes.Count -eq 2 -and $json.pages[0].strokes[0].color -eq "blue") "Datei enthaelt Version 1 und beide blauen Striche"
    Check ([Math]::Abs($firstPoint[0] - $logged[0]) -lt 0.06 -and [Math]::Abs($firstPoint[1] - $logged[1]) -lt 0.06 -and $firstPoint[2] -lt 0.3) "Koordinaten und Druck in der Datei passen zum Debug-Log ($($firstPoint -join ' '))"
    Shot "cp4-saved"
    Ctl -Keys "ctrl+n"
    Shot "cp4-new"
    Check ((Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm" -and (Compare-Shots "cp4-saved" "cp4-new") -gt 0.0005) "Ctrl+N leert das Dokument ohne Nachfrage"
    Ctl -Keys "ctrl+o"
    Start-Sleep -Milliseconds 1200
    Ctl -Text $file
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    Shot "cp4-reloaded"
    $difference = Compare-Shots "cp4-saved" "cp4-reloaded"
    Check ((Ctl -Read Window) -eq "cp4.msp - Mitschreibprogramm" -and $difference -lt 0.0005) "Ctrl+O laedt die Datei, Screenshot gleicht dem vor dem Speichern (Abweichung $([Math]::Round($difference * 100, 3)) %)"

    Pen -From "300,500" -To "700,500"
    Ctl -Keys "alt+f4"
    Start-Sleep -Milliseconds 800
    & "$toolsDir/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp4-close-dialog.png") | Out-Null
    Check ((Ctl -Read Window) -like "Ungespeicherte*") "Schliessen mit Aenderungen zeigt den Dialog"
    Ctl -Click CancelButton
    Check ((Ctl -Read Window) -eq "cp4.msp* - Mitschreibprogramm") "Abbrechen laesst die App offen"
    Ctl -Keys "alt+f4"
    Start-Sleep -Milliseconds 800
    Ctl -Click SaveButton
    Check (Wait-AppExit) "Speichern im Dialog speichert und beendet die App"
    Check ((Get-Content $file -Raw | ConvertFrom-Json).pages[0].strokes.Count -eq 3) "Die Datei enthaelt danach 3 Striche"
    $lastFolder = (Get-Content (Join-Path $outDir "settings.json") -Raw | ConvertFrom-Json).lastFolder
    Check ($lastFolder -eq [System.IO.Path]::GetFullPath($outDir)) "settings.json merkt sich den letzten Ordner ($lastFolder)"

    Start-App
    Ctl -Keys "3"; Ctl -Keys "plus"; Ctl -Keys "plus"
    Ctl -Click PressureCheck
    Ctl -Keys "ctrl+l"
    Ctl -Click LineColorButton
    Ctl -Click PageModeButton
    Ctl -Keys "alt+f4"
    Start-Sleep -Milliseconds 800
    Ctl -Click DiscardButton
    Check (Wait-AppExit) "Verwerfen im Dialog beendet die App ohne zu speichern"
    $saved = Get-Content (Join-Path $outDir "settings.json") -Raw | ConvertFrom-Json
    Check ($saved.penColor -eq "red" -and $saved.strokeWidth -eq 4 -and $saved.pressureEnabled -eq $false) "settings.json: Farbe rot, Breite 4, Druck aus"
    Check ($saved.pageStyle -eq "dashed" -and $saved.lineColor -eq "black" -and $saved.pageMode -eq "endless") "settings.json: Strichliert, Linien schwarz, Endlos"
    Start-App -KeepSettings
    Check ((Ctl -Bounds) -eq "20,10,1500,1000") "Fensterlage wird wiederhergestellt ($(Ctl -Bounds))"
    Check ((Ctl -Read WidthLabel) -match "^4[.,]0 px$" -and (Ctl -Read PageText) -eq "Endlos" -and (Ctl -Read LineColorButton) -eq "Linien: Schwarz") "Breite, Modus und Linienfarbe sind nach dem Neustart wieder da"
    Pen -From "300,300" -To "700,300" -PressureFrom 100 -PressureTo 1000
    $s = @(Read-Log)[-1]
    Check ($s.pressure -eq "False" -and $s.width -eq "4") "Neuer Strich nutzt die gespeicherten Werte (Breite 4, Druck aus)"
    Shot "cp4-settings-restored"
}
