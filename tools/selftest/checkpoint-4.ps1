# Dot-sourced by tools/selftest.ps1, uses its helper functions.
# Sum of the strokes on all pages of a notebook file.
function Get-StrokeCount([string]$path) {
    $json = Get-Content $path -Raw | ConvertFrom-Json
    return (@($json.pages | ForEach-Object { @($_.strokes).Count }) | Measure-Object -Sum).Sum
}
function Get-Index { Get-Content (Join-Path $outDir "notes/index.json") -Raw | ConvertFrom-Json }
function Get-NotePath([string]$id) { Join-Path $outDir "notes/$id.msp" }

function Checkpoint4 {
    Write-Output "Checkpoint 4: Hefte-Bibliothek, Autosave, Import/Export, Neustart"
    $export = Join-Path $outDir "cp4-export.msp"
    $pdf = Join-Path $outDir "cp4-physik.pdf"
    Remove-Item $export, $pdf -ErrorAction SilentlyContinue
    Start-App -Panel
    Check ((Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm" -and (Ctl -Read FileText) -eq "Unbenannt") "Erster Start legt das Heft Unbenannt an, Titel und Statusleiste zeigen den Namen"
    Check ((Ctl -Exists NotebookList) -eq "True" -and @(Ctl -Items NotebookList).Count -eq 1) "Seitenleiste zeigt genau ein Heft"
    $first = (Get-Index).lastOpen
    Check ($first -and (Test-Path (Get-NotePath $first))) "index.json nennt das offene Heft, seine Datei liegt in notes/"

    $origin = Get-PageOrigin 600 200
    PagePen $origin 100 300 500 300
    Ctl -Keys "ctrl+n"
    Start-Sleep -Milliseconds 600
    $second = (Get-Index).lastOpen
    Check ((Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm" -and @(Ctl -Items NotebookList).Count -eq 2 -and $second -ne $first) "Ctrl+N legt ein zweites Heft Unbenannt an und oeffnet es"
    Check ((Get-StrokeCount (Get-NotePath $first)) -eq 2) "Das vorherige Heft wurde beim Wechsel ohne Nachfrage mit beiden Strichen gespeichert"

    Ctl -DoubleClick $second
    Start-Sleep -Milliseconds 600
    & "$toolsDir/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp4-rename-dialog.png") | Out-Null
    Check ((Ctl -Read Window) -eq "Heft umbenennen") "Doppelklick auf ein Heft oeffnet den Umbenennen-Dialog"
    Ctl -Text "Mathe"
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 500
    Check ((Ctl -Read Window) -eq "Mathe - Mitschreibprogramm" -and (Ctl -Read FileText) -eq "Mathe") "Umbenennen in Mathe: Titel und Statusleiste zeigen den neuen Namen"
    Ctl -Keys "ctrl+enter"; Ctl -Keys "ctrl+enter"
    foreach ($i in 1..5) { Ctl -Wheel "600,400,-120" }
    $before = Get-PageOrigin 600 400
    $pageBefore = Ctl -Read PageText
    Check ($pageBefore -match "^Seite [23] von 3$") "Mathe hat 3 Seiten und ist nach unten gescrollt ($pageBefore)"

    Ctl -Click $first
    Start-Sleep -Milliseconds 600
    Check ((Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm") "Klick in der Seitenleiste oeffnet das erste Heft"
    $mathe = Get-Content (Get-NotePath $second) -Raw | ConvertFrom-Json
    Check ($mathe.pages.Count -eq 3 -and (Get-StrokeCount (Get-NotePath $second)) -eq 1) "Mathe wurde beim Wechsel mit 3 Seiten und seinem Strich gespeichert"
    $names = @(Ctl -Items NotebookList)
    Check ($names[0] -eq "Mathe" -and $names[1] -eq "Unbenannt") "Sortierung: zuletzt bearbeitetes Heft steht oben ($($names -join ', '))"
    Shot "cp4-panel"

    Ctl -Keys "ctrl+shift+s"
    Start-Sleep -Milliseconds 1200
    $proposed = Ctl -Value 1001
    Ctl -Text $export
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    Check ($proposed -eq "Unbenannt.msp" -and (Test-Path $export) -and (Get-StrokeCount $export) -eq 2) "Ctrl+Umschalt+S exportiert das offene Heft als .msp, Vorschlag '$proposed', 2 Striche"
    Ctl -Keys "ctrl+o"
    Start-Sleep -Milliseconds 1200
    Ctl -Text $export
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    $third = (Get-Index).lastOpen
    Check ((Ctl -Read Window) -eq "cp4-export - Mitschreibprogramm" -and @(Ctl -Items NotebookList).Count -eq 3 -and $third -ne $first -and (Get-StrokeCount (Get-NotePath $third)) -eq 2) "Ctrl+O kopiert die Datei als neues Heft cp4-export in die Bibliothek und oeffnet es"
    Ctl -Click NewNotebookButton
    Start-Sleep -Milliseconds 600
    $fourth = (Get-Index).lastOpen
    Check ((Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm" -and @(Ctl -Items NotebookList).Count -eq 4 -and $fourth -ne $third) "'+ Neues Heft' legt ein viertes Heft an"

    Ctl -RightClick $first
    Start-Sleep -Milliseconds 500
    Check ((Ctl -Exists DeleteMenuItem) -eq "True" -and (Get-Index).lastOpen -eq $fourth) "Rechtsklick oeffnet das Kontextmenue, ohne das Heft zu wechseln"
    Ctl -Click DeleteMenuItem
    Start-Sleep -Milliseconds 600
    & "$toolsDir/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp4-delete-dialog.png") | Out-Null
    Check ((Ctl -Read Window) -eq "Heft l$([char]0xF6)schen") "Loeschen fragt nach"
    Ctl -Click CancelButton
    Check ((Test-Path (Get-NotePath $first)) -and @(Ctl -Items NotebookList).Count -eq 4) "Abbrechen loescht nichts"
    Ctl -RightClick $first
    Start-Sleep -Milliseconds 500
    Ctl -Click DeleteMenuItem
    Start-Sleep -Milliseconds 600
    Ctl -Click ConfirmButton
    Start-Sleep -Milliseconds 500
    Check (-not (Test-Path (Get-NotePath $first)) -and @(Ctl -Items NotebookList).Count -eq 3 -and (Get-Index).lastOpen -eq $fourth) "Loeschen entfernt Datei und Eintrag, das offene Heft bleibt offen"

    Ctl -RightClick $second
    Start-Sleep -Milliseconds 500
    Ctl -Click RenameMenuItem
    Start-Sleep -Milliseconds 600
    Ctl -Text "Physik"
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 500
    Check ((@(Ctl -Items NotebookList) -contains "Physik") -and (Ctl -Read Window) -eq "Unbenannt - Mitschreibprogramm") "Kontextmenue Umbenennen benennt ein anderes Heft um, ohne es zu oeffnen"
    Ctl -RightClick $second
    Start-Sleep -Milliseconds 500
    Ctl -Click ExportPdfMenuItem
    Start-Sleep -Milliseconds 700
    Check ((Ctl -Read Window) -eq "Als PDF exportieren") "Kontextmenue PDF oeffnet den Export-Dialog"
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    $proposedPdf = Ctl -Value 1001
    Ctl -Text $pdf
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1500
    $pages = @(Convert-PdfToPng $pdf "cp4-physik-page")
    Check ($proposedPdf -eq "Physik.pdf" -and (Test-Path $pdf) -and ($pages.Count -eq 0 -or $pages.Count -eq 3)) "PDF-Export schlaegt den Heftnamen vor ('$proposedPdf') und schreibt das ganze Heft ($($pages.Count) Seiten gerendert)"

    Ctl -Keys "ctrl+b"
    Check ((Ctl -Exists NotebookList) -eq "False") "Ctrl+B blendet die Seitenleiste aus"
    Shot "cp4-panel-hidden"
    Ctl -Click NotebookPanelButton
    Check ((Ctl -Exists NotebookList) -eq "True") "Toolbar-Button Hefte blendet sie wieder ein"

    Ctl -Click $second
    Start-Sleep -Milliseconds 600
    $again = Get-PageOrigin 600 400
    Check ((Ctl -Read Window) -eq "Physik - Mitschreibprogramm" -and $again.Page -eq $before.Page -and [Math]::Abs($again.Y - $before.Y) -lt 1.5) "Zurueck in Physik: dieselbe Scrollposition wie beim Verlassen (Seite $($again.Page), Y $([Math]::Round($again.Y, 1)) zu $([Math]::Round($before.Y, 1)))"
    Ctl -Keys "alt+f4"
    Check (Wait-AppExit) "Alt+F4 beendet die App ohne Nachfrage"
    $saved = Get-Index
    $physik = Get-Content (Get-NotePath $second) -Raw | ConvertFrom-Json
    Check ($physik.pages.Count -eq 3 -and (Get-StrokeCount (Get-NotePath $second)) -eq 2) "Beim Schliessen wurde Physik mit 3 Seiten und 2 Strichen gespeichert"
    Check ($saved.lastOpen -eq $second -and @($saved.notebooks).Count -eq 3 -and ($saved.notebooks | Where-Object { $_.id -eq $second }).scrollY -gt 100) "index.json: zuletzt offen = Physik, 3 Hefte, Scrollposition gemerkt"

    Start-App -KeepSettings
    Check ((Ctl -Read Window) -eq "Physik - Mitschreibprogramm") "Neustart oeffnet das zuletzt offene Heft"
    $after = Get-PageOrigin 600 400
    Check ($after.Page -eq $before.Page -and [Math]::Abs($after.Y - $before.Y) -lt 1.5 -and (Ctl -Read PageText) -eq $pageBefore) "Neustart landet an derselben Stelle (Seite $($after.Page), Y $([Math]::Round($after.Y, 1)) zu $([Math]::Round($before.Y, 1)), '$(Ctl -Read PageText)')"
    $names = @(Ctl -Items NotebookList)
    Check ($names.Count -eq 3 -and $names[0] -eq "Physik" -and ($names -contains "cp4-export") -and ($names -contains "Unbenannt")) "Alle Hefte sind nach dem Neustart da, Physik oben ($($names -join ', '))"
    Shot "cp4-panel-restart"

    Ctl -Click $fourth
    Start-Sleep -Milliseconds 600
    Ctl -RightClick $fourth
    Start-Sleep -Milliseconds 500
    Ctl -Click DeleteMenuItem
    Start-Sleep -Milliseconds 600
    Ctl -Click ConfirmButton
    Start-Sleep -Milliseconds 800
    $names = @(Ctl -Items NotebookList)
    Check ($names.Count -eq 2 -and (Get-Index).lastOpen -ne $fourth -and (Ctl -Read Window) -match "^(Physik|cp4-export) - Mitschreibprogramm$") "Loeschen des offenen Hefts oeffnet das naechste ($(Ctl -Read Window))"
    Stop-App
}
