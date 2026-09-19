# Dot-sourced by tools/selftest.ps1, uses its helper functions.
function Checkpoint5 {
    Write-Output "Checkpoint 5: PDF-Export"
    $pdf = Join-Path $outDir "cp5.pdf"
    $plain = Join-Path $outDir "cp5-plain.pdf"
    Start-App
    Ctl -Keys "plus"; Ctl -Keys "plus"; Ctl -Keys "plus"; Ctl -Keys "plus"
    Pen -From "300,200" -To "800,200" -PressureFrom 60 -PressureTo 1000
    Ctl -Keys "2"
    Pen -From "300,260" -To "800,300" -PressureFrom 1000 -PressureTo 60
    Ctl -Keys "3"
    Pen -From "300,360" -To "800,340"
    Ctl -Keys "4"
    Ctl -Click PressureCheck
    Pen -From "300,420" -To "800,460" -PressureFrom 60 -PressureTo 1000
    Shot "cp5-screen"
    Ctl -Keys "ctrl+enter"
    Get-PageOrigin 500 400 | Out-Null
    Ctl -Keys "ctrl+e"
    Start-Sleep -Milliseconds 700
    & "$toolsDir/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp5-export-dialog.png") | Out-Null
    Check ((Ctl -Read Window) -eq "Als PDF exportieren") "Ctrl+E oeffnet den Export-Dialog"
    Ctl -Keys "esc"
    Export-Pdf $pdf
    Check (Test-Path $pdf) "Export ueber die Dialoge schreibt die PDF-Datei"
    Export-Pdf $plain -WithoutRuleLines
    Check ((Test-Path $plain) -and (Get-Item $pdf).Length -gt (Get-Item $plain).Length) "Ohne Haken fehlen die Hintergrundlinien (Datei kleiner)"
    $pages = @(Convert-PdfToPng $pdf "cp5-page")
    if ($pages.Count -eq 0) {
        Write-Output "  SKIP  pdftoppm nicht gefunden, PDF wird nicht gerendert"
    } else {
        Check ($pages.Count -eq 2) "PDF hat 2 Seiten wie das Dokument"
        $plainPages = @(Convert-PdfToPng $plain "cp5-plain-page")
        $withLines = Get-InkShare $pages[0].FullName
        $withoutLines = Get-InkShare $plainPages[0].FullName
        Check ($withLines -gt $withoutLines -and $withoutLines -gt 0.0005) "Gerendert: Striche sichtbar, Linien nur in der Variante mit Haken ($([Math]::Round($withLines * 100, 2)) % zu $([Math]::Round($withoutLines * 100, 2)) %)"
    }
    Stop-App
}
