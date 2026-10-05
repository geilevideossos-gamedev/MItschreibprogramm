# Dot-sourced by tools/selftest.ps1, uses its helper functions.
# The mouse rests on the page while colour, width, zoom, theme and tool change: the cursor has to follow without a
# mouse move. Sizes in the log are DIPs, the measured screen cursor is in physical pixels (log dpi). Tool changes build
# no new cursor, so the newest cursor line in the log ($script:cursorLog) stays valid for them.

function CursorShot([string]$name) { & "$toolsDir/screenshot.ps1" -Out (Join-Path $outDir "$name.png") -Cursor | Out-Null }
function Read-CursorState([string]$text) {
    $state = @{ raw = $text }
    foreach ($match in [regex]::Matches($text, "(\w+)=(\S+)")) { $state[$match.Groups[1].Value] = $match.Groups[2].Value }
    $state
}
function Check-Dot([string]$name, [string]$label, [double]$size, [string]$rgb, [string]$rim) {
    Read-Log | Out-Null
    $log = $script:cursorLog
    $cursor = Read-CursorState (Ctl -Cursor)
    CursorShot "cp12-$name"
    $fill = $size * (Number $log.dpi)
    Check ((Number $log.pen) -eq $size -and $log.rim -eq $rim -and $cursor.kind -eq "custom" -and [Math]::Abs((Number $cursor.fill) - $fill) -le 1.5 -and $cursor.center -eq $rgb) "${label}: Punkt $size px in $rgb, Rand $rim (Log pen=$($log.pen) rim=$($log.rim), Cursor $($cursor.raw))"
}
# The ring is drawn on the circle of the eraser size: half the ring (1.5) and the rim (1) lie outside on each side.
function Check-Ring([string]$name, [string]$label, [double]$size) {
    Read-Log | Out-Null
    $log = $script:cursorLog
    $cursor = Read-CursorState (Ctl -Cursor)
    CursorShot "cp12-$name"
    $outer = ($size + 3.5) * (Number $log.dpi)
    Check ((Number $log.eraser) -eq $size -and $cursor.kind -eq "custom" -and $cursor.center -eq "none" -and [Math]::Abs((Number $cursor.outer) - $outer) -le 2) "${label}: Ring $size px, innen leer (Log eraser=$($log.eraser), Cursor $($cursor.raw))"
}
# Average width in physical pixels of a horizontal stroke: the ink that the second screenshot has more than the first,
# summed over the rows where they differ, divided by the stroke length.
function Get-StrokePixelWidth([string]$before, [string]$after, [double]$lengthPixels) {
    Add-Type -AssemblyName System.Drawing
    $a = [System.Drawing.Bitmap]::FromFile((Join-Path $outDir "$before.png"))
    $b = [System.Drawing.Bitmap]::FromFile((Join-Path $outDir "$after.png"))
    try {
        $column = [int]($a.Width / 2)
        $rows = @(for ($y = 0; $y -lt $a.Height; $y++) { if ($a.GetPixel($column, $y).ToArgb() -ne $b.GetPixel($column, $y).ToArgb()) { $y } })
        if (-not $rows.Count) { return [double]::NaN }
        $ink = 0.0
        for ($y = $rows[0] - 3; $y -le $rows[-1] + 3; $y++) {
            for ($x = 0; $x -lt $a.Width; $x++) {
                $p = $a.GetPixel($x, $y); $q = $b.GetPixel($x, $y)
                $ink += (($p.R + $p.G + $p.B) - ($q.R + $q.G + $q.B)) / 765.0
            }
        }
        return $ink / $lengthPixels
    } finally { $a.Dispose(); $b.Dispose() }
}

function Checkpoint12 {
    Write-Output "Checkpoint 12: Stift- und Radierer-Cursor"
    $at = "585,300"
    $light = "#FFFFFFFF"
    $dark = "#FF000000"
    Start-App
    $scale = [MspNative]::DpiScale([MspNative]::FindAppWindow("Mitschreibprogramm"))

    Ctl -Keys "ctrl+l"; Ctl -Keys "ctrl+l"
    Ctl -Click PresetThin
    Shot "cp12-blank"
    Pen -From "350,420" -To "820,420" -Steps 40
    $stroke = @(Read-Log | Where-Object { $_.kind -eq "stroke" })[-1]
    Shot "cp12-thin-stroke"
    $pixels = Get-StrokePixelWidth "cp12-blank" "cp12-thin-stroke" (470 * $scale)
    Write-Output ("  INFO  duenner Strich: Log width={0}, im Bild {1:0.000} px bei Skalierung {2}" -f $stroke.width, $pixels, $scale)
    Check ($stroke.width -eq "1.5" -and [Math]::Abs($pixels - 1.5 * $scale) -lt 0.35) "Duenner Strich unveraendert: width=$($stroke.width), $([Math]::Round($pixels, 3)) px breit"

    Ctl -Keys "ctrl+l"; Ctl -Keys "ctrl+l"
    Ctl -Move "585,420"
    Check-Dot "on-stroke" "ueber dem schwarzen Strich, kariert" 8 "0,0,0" $light
    Ctl -Move $at
    Check-Dot "thin" "duenn 100 %" 8 "0,0,0" $light
    Ctl -Click PresetMedium
    Check-Dot "medium" "mittel 100 %" 8 "0,0,0" $light
    Ctl -Click PresetThick
    Check-Dot "thick" "dick 100 %" 8 "0,0,0" $light
    Ctl -Keys "ctrl+plus"; Ctl -Keys "ctrl+plus"; Ctl -Keys "ctrl+plus"
    Check-Dot "thick-200" "dick 200 %" 12 "0,0,0" $light
    Ctl -Click PresetThin
    Check-Dot "thin-200" "duenn 200 %" 8 "0,0,0" $light
    Ctl -Keys "e"
    Check-Ring "eraser-200" "Radierer 200 %" 16
    Ctl -Keys "p"
    Ctl -Keys "ctrl+0"; Ctl -Keys "ctrl+minus"; Ctl -Keys "ctrl+minus"
    Ctl -Click PresetThick
    Check-Dot "thick-50" "dick 50 %" 8 "0,0,0" $light
    Ctl -Keys "e"
    Check-Ring "eraser-50" "Radierer 50 %" 8
    Ctl -Keys "p"
    Ctl -Keys "ctrl+0"
    Ctl -Click PresetMedium

    foreach ($color in @(@{ Key = "2"; Name = "blue"; Rgb = "30,80,200" }, @{ Key = "3"; Name = "red"; Rgb = "208,37,43" }, @{ Key = "4"; Name = "green"; Rgb = "30,138,60" }, @{ Key = "1"; Name = "black"; Rgb = "0,0,0" })) {
        Ctl -Keys $color.Key
        Check-Dot $color.Name "Farbe $($color.Name)" 8 $color.Rgb $light
    }
    Ctl -Keys "e"
    Check-Ring "eraser" "Radierer 100 %" 8

    Ctl -Keys "p"
    Ctl -Keys "ctrl+d"
    Check-Dot "dark" "Dark Mode schwarz" 8 "255,255,255" $dark
    Ctl -Keys "2"
    Check-Dot "dark-blue" "Dark Mode blau" 8 "30,80,200" $dark
    Ctl -Keys "1"
    Ctl -Keys "e"
    Check-Ring "dark-eraser" "Dark Mode Radierer" 8
    Ctl -Keys "p"
    Ctl -Keys "ctrl+d"

    Ctl -Keys "s"
    $select = Read-CursorState (Ctl -Cursor)
    CursorShot "cp12-select"
    Check ($select.kind -ne "custom" -and $select.kind -ne "none") "Auswahl: Cursor von InkCanvas wie bisher ($($select.raw))"
    Ctl -Keys "p"
    Ctl -Move "100,300"
    $gray = Read-CursorState (Ctl -Cursor)
    CursorShot "cp12-gray"
    Check ($gray.kind -eq "arrow") "Graue Flaeche neben der Seite: Pfeil ($($gray.raw))"

    $hover = Read-CursorState (& "$toolsDir/pen-sim.ps1" -Hover $at -Shot (Join-Path $outDir "cp12-hover-pen.png"))
    Check (($hover.flags -band 1) -and $hover.kind -eq "custom" -and $hover.center -eq "0,0,0" -and [Math]::Abs((Number $hover.fill) - 8 * $scale) -le 1.5) "Stift schwebt: Punkt sichtbar ($($hover.raw))"
    $inverted = Read-CursorState (& "$toolsDir/pen-sim.ps1" -Hover $at -Inverted -Shot (Join-Path $outDir "cp12-hover-eraser.png"))
    Check (($inverted.flags -band 1) -and $inverted.kind -eq "custom" -and $inverted.center -eq "none") "Stift schwebt mit unterer Taste (invertiert): Ring ($($inverted.raw))"
    $barrel = Read-CursorState (& "$toolsDir/pen-sim.ps1" -Hover $at -Barrel -Shot (Join-Path $outDir "cp12-hover-lasso.png"))
    Check ($barrel.kind -ne "custom" -and $barrel.kind -eq $select.kind) "Stift schwebt mit oberer Taste (Lasso): Cursor wie im Auswahl-Werkzeug ($($barrel.raw))"
    $back = Read-CursorState (& "$toolsDir/pen-sim.ps1" -Hover $at)
    Check ($back.kind -eq "custom" -and $back.center -eq "0,0,0") "Nach dem Lasso-Hover wieder der Punkt ($($back.raw))"
}
