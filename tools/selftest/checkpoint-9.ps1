# Dot-sourced by tools/selftest.ps1, uses its helper functions and PagePath / Get-NewestNote from checkpoint-8.ps1.
# Puts a test picture on the clipboard: only text on the clipboard is restored afterwards.

# 400 x 300: left half orange, right half blue. WinForms offers it as a plain bitmap, the app's fallback path.
function Set-TestImageOnClipboard {
    Add-Type -AssemblyName System.Windows.Forms, System.Drawing
    $bitmap = New-Object System.Drawing.Bitmap 400, 300
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 240, 120, 30))
    $graphics.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 90, 220))), 200, 0, 200, 300)
    $graphics.Dispose()
    [System.Windows.Forms.Clipboard]::SetImage($bitmap)
    $bitmap.Dispose()
}
function Get-PageColor($origin, [double]$x, [double]$y) {
    $client = "{0},{1}" -f ($origin.X + $origin.Zoom * $x).ToString($invariant), ($origin.Y + $origin.Zoom * $y).ToString($invariant)
    Start-Sleep -Milliseconds 300
    @((Ctl -Pixel $client).Split(",") | ForEach-Object { [int]$_ })
}
function Test-Orange($rgb) { $rgb[0] -gt 200 -and $rgb[1] -gt 90 -and $rgb[1] -lt 150 -and $rgb[2] -lt 80 }
function Test-Blue($rgb) { $rgb[2] -gt 180 -and $rgb[0] -lt 80 }
function Get-Rect($entry) { $values = PointOf $entry.rect; @{ X = $values[0]; Y = $values[1]; W = $values[2]; H = $values[3] } }
function Get-Picture([string]$file) { @((Get-Content $file -Raw | ConvertFrom-Json).pages | ForEach-Object { $_.images }) }
function Selection([string]$change) { @(Read-Log | Where-Object { $_.kind -eq "selection" -and $_.change -eq $change }) }

function Checkpoint9 {
    Write-Output "Checkpoint 9: Bilder und Lasso"
    Add-Type -AssemblyName System.Windows.Forms
    $savedText = if ([System.Windows.Forms.Clipboard]::ContainsText()) { [System.Windows.Forms.Clipboard]::GetText() } else { $null }
    try { Checkpoint9Steps } finally {
        if ($savedText) { [System.Windows.Forms.Clipboard]::SetText($savedText) } else { [System.Windows.Forms.Clipboard]::Clear() }
    }
}

function Checkpoint9Steps {
    Start-App
    $origin = Get-PageOrigin 500 110
    [System.Windows.Forms.Clipboard]::SetText("kein Bild")
    Ctl -Keys "ctrl+v"
    Check (@(Read-Log | Where-Object { $_.kind -eq "image" }).Count -eq 0) "Strg+V ohne Bild in der Zwischenablage tut nichts"
    Set-TestImageOnClipboard
    Ctl -Keys "ctrl+v"
    $pasted = @(Read-Log | Where-Object { $_.kind -eq "image" })
    Check ($pasted.Count -eq 1 -and (Number $pasted[0].width) -eq 400 -and (Number $pasted[0].height) -eq 300) "Strg+V fuegt das Bild ein ($($pasted.raw))"
    $x = Number $pasted[0].x; $y = Number $pasted[0].y
    Check ((Test-Orange (Get-PageColor $origin ($x + 100) ($y + 150))) -and (Test-Blue (Get-PageColor $origin ($x + 300) ($y + 150)))) "Bild ist an der gemeldeten Stelle sichtbar (links orange, rechts blau)"
    PagePath $origin @(@(($x + 40), ($y + 80)), @(($x + 360), ($y + 80)))
    Read-Log | Out-Null
    $ink = Get-PageColor $origin ($x + 100) ($y + 80)
    Check (($ink[0] + $ink[1] + $ink[2]) -lt 200) "Strich auf dem Bild liegt ueber dem Bild ($($ink -join ','))"
    Shot "cp9-pasted"

    PagePath $origin @(@(($x - 20), ($y - 20)), @(($x + 420), ($y - 20)), @(($x + 420), ($y + 320)), @(($x - 20), ($y + 320)), @(($x - 20), ($y - 18))) -Barrel
    $selected = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Check ($selected.Count -ge 1 -and $selected[-1].strokes -eq "1" -and $selected[-1].images -eq "1") "Lasso mit gehaltener oberer Seitentaste waehlt Strich und Bild ($($selected[-1].raw))"
    $before = Get-Rect $selected[-1]
    PagePath $origin @(@(($x + 200), ($y + 200)), @(($x + 260), ($y + 250)))
    $moved = Selection "moved"
    $after = if ($moved.Count) { Get-Rect $moved[-1] } else { @{ X = [double]::NaN } }
    Check ([Math]::Abs($after.X - $before.X - 60) -lt 3 -and [Math]::Abs($after.Y - $before.Y - 50) -lt 3) "Auswahl mit dem Stift gezogen: um 60,50 verschoben ($($moved.raw))"
    $cornerX = $after.X + $after.W + 8; $cornerY = $after.Y + $after.H + 8
    $from = "{0},{1}" -f ($origin.X + $cornerX).ToString($invariant), ($origin.Y + $cornerY).ToString($invariant)
    $to = "{0},{1}" -f ($origin.X + $cornerX + 80).ToString($invariant), ($origin.Y + $cornerY + 20).ToString($invariant)
    Ctl -Drag "left,$from,$to"
    $resized = Selection "resized"
    $grown = if ($resized.Count) { Get-Rect $resized[-1] } else { @{ W = [double]::NaN; H = 1 } }
    Check ($grown.W -gt $after.W + 40 -and [Math]::Abs(($grown.W / $grown.H) - ($after.W / $after.H)) -lt 0.02) "Eckgriff skaliert, Seitenverhaeltnis bleibt ($([Math]::Round($after.W)) x $([Math]::Round($after.H)) zu $([Math]::Round($grown.W)) x $([Math]::Round($grown.H)))"
    Shot "cp9-moved-resized"

    Ctl -Keys "ctrl+s"
    $file = Get-NewestNote
    $big = @(Get-Picture $file)[0]
    Ctl -Keys "ctrl+z"
    Ctl -Keys "ctrl+s"
    $small = @(Get-Picture $file)[0]
    Check ($big.width -gt 440 -and $small.width -eq 400 -and [Math]::Abs($small.x - ($x + 60)) -lt 1) "Strg+Z nimmt das Skalieren zurueck, das Verschieben bleibt ($($big.width) zu $($small.width), x $($small.x))"
    Ctl -Keys "ctrl+z"
    Ctl -Keys "ctrl+s"
    Check ([Math]::Abs(@(Get-Picture $file)[0].x - $x) -lt 1) "Zweites Strg+Z nimmt das Verschieben zurueck"
    Ctl -Keys "ctrl+y"; Ctl -Keys "ctrl+y"
    Ctl -Keys "ctrl+s"
    Check (@(Get-Picture $file)[0].width -gt 440) "Strg+Y stellt beides wieder her"
    Read-Log | Out-Null

    $picture = @(Get-Picture $file)[0]
    PagePath $origin @(@(($picture.x - 20), ($picture.y - 20)), @(($picture.x + $picture.width + 20), ($picture.y - 20)), @(($picture.x + $picture.width + 20), ($picture.y + $picture.height + 20)), @(($picture.x - 20), ($picture.y + $picture.height + 20)), @(($picture.x - 20), ($picture.y - 18))) -Barrel
    Read-Log | Out-Null
    PagePath $origin @(@(($picture.x + 100), ($picture.y + 100)), @(($picture.x + 560), ($picture.y + 140)))
    $edge = Selection "moved"
    $right = if ($edge.Count) { $r = Get-Rect $edge[-1]; $r.X + $r.W } else { [double]::NaN }
    Check ($right -le 794 -and $right -gt 780) "Im Seitenmodus bleibt die verschobene Auswahl auf dem Blatt (rechter Rand $right)"
    Ctl -Keys "esc"
    Check (@(Read-Log | Where-Object { $_.kind -eq "selection" })[-1].strokes -eq "0") "Esc hebt die Auswahl auf"

    PagePath $origin @(@(100, 600), @(260, 600))
    Ctl -Keys "s"
    $lasso = @(@(80, 580), @(280, 580), @(280, 625), @(80, 625), @(80, 582)) | ForEach-Object { "{0},{1}" -f ($origin.X + $_[0]).ToString($invariant), ($origin.Y + $_[1]).ToString($invariant) }
    Read-Log | Out-Null
    Ctl -DragPath ($lasso -join ";")
    $mouse = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Check ($mouse.Count -ge 1 -and $mouse[-1].strokes -eq "1" -and $mouse[-1].images -eq "0") "Auswahl-Werkzeug (S): Maus-Lasso waehlt den Strich ($($mouse.raw))"
    Ctl -Keys "delete"
    $deleted = Selection "deleted"
    Ctl -Keys "ctrl+s"
    $count = @((Get-Content $file -Raw | ConvertFrom-Json).pages | ForEach-Object { $_.strokes }).Count
    Ctl -Keys "ctrl+z"
    Ctl -Keys "ctrl+s"
    $restored = @((Get-Content $file -Raw | ConvertFrom-Json).pages | ForEach-Object { $_.strokes }).Count
    Check ($deleted.Count -eq 1 -and $deleted[0].strokes -eq "1" -and $restored -eq $count + 1) "Entf loescht die Auswahl, Strg+Z holt sie zurueck ($count zu $restored Strichen)"

    $picture = @(Get-Picture $file)[0]
    $tap = "{0},{1}" -f ($origin.X + $picture.x + ($picture.width * 0.75)).ToString($invariant), ($origin.Y + $picture.y + ($picture.height * 0.75)).ToString($invariant)
    Ctl -Drag "left,$tap,$tap"
    $alone = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Check ($alone.Count -ge 1 -and $alone[-1].strokes -eq "0" -and $alone[-1].images -eq "1") "Tippen auf das Bild waehlt es allein ($($alone.raw))"
    $target = "{0},{1}" -f ($origin.X + $picture.x + ($picture.width * 0.75) - 40).ToString($invariant), ($origin.Y + $picture.y + ($picture.height * 0.75) + 30).ToString($invariant)
    Ctl -Drag "left,$tap,$target"
    $dragged = Selection "moved"
    Ctl -Keys "ctrl+s"
    $shifted = @(Get-Picture $file)[0]
    Check ($dragged.Count -eq 1 -and [Math]::Abs($shifted.x - ($picture.x - 40)) -lt 1.5 -and [Math]::Abs($shifted.y - ($picture.y + 30)) -lt 1.5) "Allein gewaehltes Bild laesst sich in der Mitte ziehen (x $($picture.x) zu $($shifted.x))"
    Ctl -Keys "delete"
    Ctl -Keys "ctrl+s"
    $gone = @(Get-Picture $file).Count
    Ctl -Keys "ctrl+z"
    Ctl -Keys "p"
    Ctl -Keys "ctrl+s"
    Check ($gone -eq 0 -and @(Get-Picture $file).Count -eq 1) "Entf loescht das Bild, Strg+Z holt es zurueck"
    $final = @(Get-Picture $file)[0]
    Ctl -Keys "alt+f4"
    Check (Wait-AppExit) "App schliesst"

    Start-App -KeepSettings
    $again = Get-PageOrigin 500 110
    Check ((Test-Orange (Get-PageColor $again ($final.x + ($final.width / 4)) ($final.y + ($final.height / 2)))) -and (Test-Blue (Get-PageColor $again ($final.x + ($final.width * 0.8)) ($final.y + ($final.height / 2))))) "Nach dem Neustart ist das Bild an seiner Stelle"
    Shot "cp9-restart"
    $pdf = Join-Path $outDir "cp9.pdf"
    Export-Pdf $pdf -WithoutRuleLines
    $pages = @(Convert-PdfToPng $pdf "cp9-page")
    if ($pages.Count -eq 0) {
        Write-Output "  SKIP  pdftoppm nicht gefunden, PDF wird nicht gerendert"
    } else {
        Add-Type -AssemblyName System.Drawing
        $scale = 100 / 96
        $image = [System.Drawing.Bitmap]::FromFile($pages[0].FullName)
        try {
            $left = $image.GetPixel([int](($final.x + $final.width / 4) * $scale), [int](($final.y + $final.height / 2) * $scale))
            $rightSide = $image.GetPixel([int](($final.x + $final.width * 0.8) * $scale), [int](($final.y + $final.height / 2) * $scale))
            $outside = $image.GetPixel([int](($final.x - 15) * $scale), [int](($final.y + $final.height / 2) * $scale))
        } finally { $image.Dispose() }
        Check ((Test-Orange @($left.R, $left.G, $left.B)) -and (Test-Blue @($rightSide.R, $rightSide.G, $rightSide.B)) -and ($outside.R + $outside.G + $outside.B) -gt 720) "PDF enthaelt das Bild an Position und Groesse (Rand weiss daneben)"
    }
    Stop-App
}
