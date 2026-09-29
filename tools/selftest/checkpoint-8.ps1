# Dot-sourced by tools/selftest.ps1, uses its helper functions. PagePath and Circle are also used by checkpoint 9.

# Injects a pen stroke along page coordinates. Unlike Pen it also counts selection and shape lines as arrival, so a
# lasso (no stroke line) is not taken for a lost stroke; a stroke that left no log line at all is repeated once.
function PagePath($origin, $points, [int]$HoldMs = 0, [switch]$Barrel) {
    $path = ($points | ForEach-Object { "{0},{1}" -f ($origin.X + $origin.Zoom * $_[0]).ToString($invariant), ($origin.Y + $origin.Zoom * $_[1]).ToString($invariant) }) -join ";"
    $before = Get-InkLineCount
    & "$toolsDir/pen-sim.ps1" -Path $path -HoldMs $HoldMs -Barrel:$Barrel | Out-Null
    Start-Sleep -Milliseconds 400
    if ((Get-InkLineCount) -eq $before) {
        Start-Sleep -Milliseconds 400
        if ((Get-InkLineCount) -eq $before) {
            Write-Output "  RETRY path (keine Logzeile)"
            $script:retries++
            & "$toolsDir/pen-sim.ps1" -Path $path -HoldMs $HoldMs -Barrel:$Barrel | Out-Null
            Start-Sleep -Milliseconds 400
        }
    }
}
function Get-InkLineCount { @(if (Test-Path $logPath) { Get-Content $logPath | Where-Object { $_ -match "^\S+ (stroke|shape|selection) " } }).Count }
function Circle([double]$cx, [double]$cy, [double]$r, [int]$count) {
    for ($i = 0; $i -le $count; $i++) { $a = 2 * [Math]::PI * $i / $count; , @(($cx + $r * [Math]::Cos($a)), ($cy + $r * [Math]::Sin($a))) }
}
function Get-LastStroke([string]$file) {
    $strokes = @((Get-Content $file -Raw | ConvertFrom-Json).pages | ForEach-Object { $_.strokes })
    $strokes[-1]
}
function Get-NewestNote { (Get-ChildItem (Join-Path $outDir "notes") -Filter "*.msp" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName }

function Checkpoint8 {
    Write-Output "Checkpoint 8: Formerkennung"
    Start-App
    $origin = Get-PageOrigin 500 110
    $shapes = @(
        @{ Name = "line"; Points = @(@(100, 230), @(560, 262)) },
        @{ Name = "rectangle"; Points = @(@(120, 300), @(400, 300), @(400, 420), @(120, 420), @(120, 300), @(140, 301)) },
        @{ Name = "triangle"; Points = @(@(600, 300), @(700, 440), @(500, 440), @(600, 300)) },
        @{ Name = "circle"; Points = @(Circle 250 520 70 36) }
    )
    foreach ($shape in $shapes) {
        PagePath $origin $shape.Points -HoldMs 700
        $entries = @(Read-Log)
        $stroke = @($entries | Where-Object { $_.kind -eq "stroke" })
        $recognized = @($entries | Where-Object { $_.kind -eq "shape" })
        Check ($stroke.Count -eq 1 -and $recognized.Count -eq 1 -and $recognized[0].trigger -eq "hold" -and $recognized[0].result -eq $shape.Name) "Stillhalten am Strichende: $($shape.Name) erkannt ($($recognized.raw))"
    }
    Shot "cp8-shapes-hold"

    PagePath $origin @(@(350, 470), @(550, 470), @(550, 570), @(350, 570), @(350, 470))
    $entries = @(Read-Log)
    Check (@($entries | Where-Object { $_.kind -eq "stroke" }).Count -eq 1 -and @($entries | Where-Object { $_.kind -eq "shape" }).Count -eq 0) "Rechteck ohne Stillhalten bleibt Freihand"

    Ctl -Keys "f"
    PagePath $origin @(Circle 680 520 65 36)
    $toggle = @(Read-Log | Where-Object { $_.kind -eq "shape" })
    Check ($toggle.Count -eq 1 -and $toggle[0].trigger -eq "toggle" -and $toggle[0].result -eq "circle") "Schalter Formen (F): Kreis wird beim Loslassen erkannt ($($toggle.raw))"
    Ctl -Keys "ctrl+s"
    $file = Get-NewestNote
    $shaped = Get-LastStroke $file
    Ctl -Keys "ctrl+z"
    Ctl -Keys "ctrl+s"
    $freehand = Get-LastStroke $file
    Check ($shaped.fitToCurve -eq $false -and $shaped.points.Count -eq 73 -and $freehand.fitToCurve -eq $true -and $freehand.points.Count -gt 20 -and $freehand.points.Count -ne 73) "Strg+Z nach der Erkennung holt den Freihand-Strich zurueck ($($shaped.points.Count) zu $($freehand.points.Count) Punkten)"
    Ctl -Keys "ctrl+y"
    Ctl -Keys "ctrl+s"
    Check ((Get-LastStroke $file).fitToCurve -eq $false) "Strg+Y stellt die Form wieder her"
    Read-Log | Out-Null

    PagePath $origin @(Circle 480 615 10 16)
    PagePath $origin @(@(560, 596), @(548, 614), @(566, 614))
    PagePath $origin @(@(600, 600), @(612, 625), @(624, 600), @(636, 625), @(648, 600))
    $writing = @(Read-Log | Where-Object { $_.kind -eq "shape" })
    Check ($writing.Count -eq 3 -and @($writing | Where-Object { $_.result -ne "none" }).Count -eq 0) "Mit Formen an bleiben kleine Handschrift-Striche (o, 4, w) Freihand"
    Ctl -Keys "f"
    Shot "cp8-shapes"

    Ctl -Keys "ctrl+s"
    $saved = @((Get-Content $file -Raw | ConvertFrom-Json).pages | ForEach-Object { $_.strokes })
    $shapeStrokes = @($saved | Where-Object { $_.fitToCurve -eq $false })
    Check ($shapeStrokes.Count -eq 5 -and @($shapeStrokes | Where-Object { $_.width -ne 3 -or $_.color -ne "black" }).Count -eq 0) "Datei: 5 Form-Striche mit fitToCurve false, Farbe und Breite vom Stift ($($shapeStrokes.Count))"
    $pdf = Join-Path $outDir "cp8.pdf"
    Export-Pdf $pdf -WithoutRuleLines
    $pages = @(Convert-PdfToPng $pdf "cp8-page")
    if ($pages.Count -eq 0) {
        Write-Output "  SKIP  pdftoppm nicht gefunden, PDF wird nicht gerendert"
    } else {
        Check ($pages.Count -eq 1 -and (Get-InkShare $pages[0].FullName) -gt 0.002) "PDF mit den Formen gerendert ($($pages[0].Name))"
    }
    Stop-App
}
