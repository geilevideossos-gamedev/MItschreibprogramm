# Dot-sourced by tools/selftest.ps1, uses its helper functions.
# Section 7 of the step plan has no checkpoint of its own; it runs with the others and against the exe.
function Checkpoint7 {
    Write-Output "Abschnitt 7: Dark Mode"
    $file = Join-Path $outDir "cp7.msp"
    $pdf = Join-Path $outDir "cp7.pdf"
    Remove-Item $file -ErrorAction SilentlyContinue
    Start-App
    Ctl -Keys "plus"; Ctl -Keys "plus"; Ctl -Keys "plus"; Ctl -Keys "plus"
    $origin = Get-PageOrigin 500 160
    PagePen $origin 100 226 500 226
    Ctl -Keys "2"
    PagePen $origin 100 287 500 287
    Ctl -Keys "1"
    $inkLight = Get-PageBrightness $origin 300 226
    $blueLight = Get-PageBrightness $origin 300 287
    $paperLight = Get-PageBrightness $origin 300 256
    Check ($inkLight -lt 120 -and $paperLight -gt 700) "Hell: schwarzer Strich dunkel ($inkLight), Seite weiss ($paperLight)"
    Ctl -Keys "ctrl+d"
    Shot "cp7-dark"
    $inkDark = Get-PageBrightness $origin 300 226
    $blueDark = Get-PageBrightness $origin 300 287
    $paperDark = Get-PageBrightness $origin 300 256
    Check ($inkDark -gt 700 -and $paperDark -lt 200) "Ctrl+D: Schwarz wird weiss dargestellt ($inkDark), Seite dunkelgrau ($paperDark)"
    Check ([Math]::Abs($blueDark - $blueLight) -lt 30) "Blau bleibt unveraendert ($blueLight zu $blueDark)"
    PagePen $origin 100 347 500 347
    Check ((Get-PageBrightness $origin 300 347) -gt 700) "Neuer schwarzer Strich erscheint im Dark Mode weiss"
    Ctl -Keys "ctrl+z"
    Ctl -Keys "ctrl+d"
    Ctl -Keys "ctrl+y"
    Check ((Get-PageBrightness $origin 300 347) -lt 120) "Redo nach Themenwechsel faerbt den Strich passend zum hellen Modus"
    Ctl -Click DarkModeButton
    Check ((Get-PageBrightness $origin 300 256) -lt 200) "Toolbar-Button schaltet den Dark Mode ein"
    Ctl -Keys "ctrl+l"
    Shot "cp7-dark-squared"
    $darkGrid = Get-VerticalLineCount "cp7-dark-squared"
    Check ($darkGrid -ge 30 -and $darkGrid -le 45) "Kariert im Dark Mode: helleres Gitter auf dunkler Seite ($darkGrid senkrechte Linien)"

    Ctl -Keys "ctrl+s"
    Start-Sleep -Milliseconds 1200
    & "$toolsDir/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp7-dark-savedialog.png") | Out-Null
    Ctl -Text $file
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    $colors = @((Get-Content $file -Raw | ConvertFrom-Json).pages[0].strokes | ForEach-Object { $_.color })
    Check (($colors -join ",") -eq "black,black,blue,black") "Gespeichert bleibt die logische Farbe ($($colors -join ','))"
    Ctl -Keys "ctrl+e"
    Start-Sleep -Milliseconds 700
    & "$toolsDir/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp7-dark-dialog.png") | Out-Null
    Ctl -Keys "esc"
    Export-Pdf $pdf
    $pages = @(Convert-PdfToPng $pdf "cp7-page")
    if ($pages.Count -eq 0) {
        Write-Output "  SKIP  pdftoppm nicht gefunden, PDF wird nicht gerendert"
    } else {
        $ink = Get-InkShare $pages[0].FullName
        Check ($ink -gt 0.0005 -and $ink -lt 0.2) "PDF aus dem Dark Mode ist Schwarz auf Weiss (Farbanteil $([Math]::Round($ink * 100, 2)) %)"
    }
    Ctl -Keys "alt+f4"
    Check (Wait-AppExit) "App schliesst ohne Nachfrage (gespeichert)"
    Check ((Get-Content (Join-Path $outDir "settings.json") -Raw | ConvertFrom-Json).darkMode -eq $true) "settings.json: darkMode = true"
    Start-App -KeepSettings
    $restored = Get-PageOrigin 500 160
    Check ((Get-PageBrightness $restored 300 256) -lt 200) "Neustart kommt im Dark Mode hoch"
    Shot "cp7-dark-restart"
}
