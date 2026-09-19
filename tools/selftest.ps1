# Runs the self-test checkpoints (docs/testing.md) against an exe: synthetic pen, keys, mouse, debug log, screenshots.
# Usage: powershell -ExecutionPolicy Bypass -File tools/selftest.ps1 [-Exe dist/Mitschreibprogramm.exe] [-Checkpoint 1,2]
# Needs an unlocked desktop. Nobody may use mouse or keyboard while it runs. Screenshots land in tmp/selftest/.
param(
    [string]$Exe = "Mitschreibprogramm/bin/Debug/net8.0-windows/Mitschreibprogramm.exe",
    [string]$Checkpoint = "1,2,3,4,5"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot
$exePath = if ([System.IO.Path]::IsPathRooted($Exe)) { $Exe } else { Join-Path $root $Exe }
$outDir = Join-Path $root "tmp/selftest"
$logPath = Join-Path $outDir "debug.log"
$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$script:failures = 0
$script:logOffset = 0
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Check([bool]$condition, [string]$message) {
    if ($condition) { Write-Output "  PASS  $message" } else { Write-Output "  FAIL  $message"; $script:failures++ }
}
function Pen { & "$PSScriptRoot/pen-sim.ps1" @args | Out-Null; Start-Sleep -Milliseconds 350 }
function Ctl { & "$PSScriptRoot/app-control.ps1" @args; Start-Sleep -Milliseconds 250 }
function Shot([string]$name) { & "$PSScriptRoot/screenshot.ps1" -Out (Join-Path $outDir "$name.png") | Out-Null }

function Start-App([switch]$KeepSettings) {
    Stop-App
    Remove-Item $logPath -ErrorAction SilentlyContinue
    if (-not $KeepSettings) { Remove-Item (Join-Path $outDir "settings.json") -ErrorAction SilentlyContinue }
    $script:logOffset = 0
    $env:MSP_DEBUG_LOG = $logPath
    try { Start-Process -FilePath $exePath } finally { $env:MSP_DEBUG_LOG = $null }
    foreach ($attempt in 1..40) {
        Start-Sleep -Milliseconds 250
        if (Get-Process Mitschreibprogramm -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero }) { break }
    }
    Start-Sleep -Milliseconds 800
    if (-not $KeepSettings) { Ctl -Place "20,10,1500,1000" }
}
function Stop-App {
    Get-Process Mitschreibprogramm -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 400
}

# Returns the log lines written since the previous call, parsed into key/value tables.
function Read-Log {
    Start-Sleep -Milliseconds 300
    $lines = @(if (Test-Path $logPath) { Get-Content $logPath })
    $new = @($lines | Select-Object -Skip $script:logOffset)
    $script:logOffset = $lines.Count
    foreach ($line in $new) {
        $entry = @{ raw = $line; kind = ($line -split " ")[1] }
        foreach ($match in [regex]::Matches($line, "(\w+)=(\([^)]*\)|\S+)")) { $entry[$match.Groups[1].Value] = $match.Groups[2].Value }
        $entry
    }
}
function Number([string]$value) { [double]::Parse($value, $invariant) }
function PointOf([string]$value) { @($value.Trim("(", ")").Split(",") | ForEach-Object { Number $_ }) }

# Draws a short stroke at a client position and derives where page 0,0 sits in client coordinates.
function Get-PageOrigin([double]$clientX, [double]$clientY) {
    Pen -From "$clientX,$clientY" -To "$($clientX + 20),$clientY" -Steps 6
    $entry = @(Read-Log)[-1]
    $first = PointOf $entry.first
    $zoom = Number $entry.zoom
    @{ X = $clientX - $zoom * $first[0]; Y = $clientY - $zoom * $first[1]; Zoom = $zoom; Page = $entry.page }
}
function PagePen($origin, [double]$x1, [double]$y1, [double]$x2, [double]$y2) {
    $from = "{0},{1}" -f ($origin.X + $origin.Zoom * $x1).ToString($invariant), ($origin.Y + $origin.Zoom * $y1).ToString($invariant)
    $to = "{0},{1}" -f ($origin.X + $origin.Zoom * $x2).ToString($invariant), ($origin.Y + $origin.Zoom * $y2).ToString($invariant)
    Pen -From $from -To $to
}

function Checkpoint1 {
    Write-Output "Checkpoint 1: Pen, Druck, Seitentaste, invertiert, E/P, Maus"
    Start-App
    Pen -From "300,300" -To "700,300" -PressureFrom 100 -PressureTo 1000
    $s = @(Read-Log)
    Check ($s.Count -eq 1 -and $s[0].device -eq "stylus" -and $s[0].mode -eq "ink") "Synthetischer Pen kommt als Stylus an"
    Check ((Number $s[0].pmin) -lt 0.2 -and (Number $s[0].pmax) -gt 0.9) "Druckverlauf 100..1000 kommt an (pmin=$($s[0].pmin) pmax=$($s[0].pmax))"
    Shot "cp1-pressure"
    Pen -From "500,250" -To "500,350" -Barrel
    $s = @(Read-Log)
    Check ($s.Count -eq 1 -and $s[0].mode -eq "erase" -and $s[0].barrel -eq "True") "Gehaltene Seitentaste radiert den gekreuzten Strich"
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

function Checkpoint2 {
    Write-Output "Checkpoint 2: Hintergrund, Zoom, Pan, Undo/Redo"
    Start-App
    Shot "cp2-lined-blue"
    Ctl -Click LineColorButton
    Shot "cp2-lined-black"
    Ctl -Keys "ctrl+l"
    Shot "cp2-dashed"
    Ctl -Keys "ctrl+l"
    Shot "cp2-blank"
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

# Share of pixels that differ between two screenshots (0 = identical).
function Compare-Shots([string]$first, [string]$second) {
    Add-Type -AssemblyName System.Drawing
    $a = [System.Drawing.Bitmap]::FromFile((Join-Path $outDir "$first.png"))
    $b = [System.Drawing.Bitmap]::FromFile((Join-Path $outDir "$second.png"))
    try {
        if ($a.Width -ne $b.Width -or $a.Height -ne $b.Height) { return 1.0 }
        $different = 0; $total = 0
        for ($y = 0; $y -lt $a.Height; $y += 2) {
            for ($x = 0; $x -lt $a.Width; $x += 2) {
                $total++
                if ($a.GetPixel($x, $y).ToArgb() -ne $b.GetPixel($x, $y).ToArgb()) { $different++ }
            }
        }
        return $different / $total
    } finally { $a.Dispose(); $b.Dispose() }
}
function Wait-AppExit {
    foreach ($attempt in 1..20) {
        if (-not (Get-Process Mitschreibprogramm -ErrorAction SilentlyContinue)) { return $true }
        Start-Sleep -Milliseconds 250
    }
    return $false
}

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
    & "$PSScriptRoot/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp4-close-dialog.png") | Out-Null
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

# Exports through the real dialogs: Ctrl+E, optional checkbox toggle, Enter, path, Enter.
function Export-Pdf([string]$path, [switch]$WithoutRuleLines) {
    Remove-Item $path -ErrorAction SilentlyContinue
    Ctl -Keys "ctrl+e"
    Start-Sleep -Milliseconds 700
    if ($WithoutRuleLines) { Ctl -Click RuleLinesCheck }
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1200
    Ctl -Text $path
    Ctl -Keys "enter"
    Start-Sleep -Milliseconds 1500
}
# Renders a PDF to PNG files with pdftoppm and returns them (empty if pdftoppm is missing).
function Convert-PdfToPng([string]$pdf, [string]$prefix) {
    $tool = Get-Command pdftoppm -ErrorAction SilentlyContinue
    if (-not $tool) { return @() }
    Get-ChildItem $outDir -Filter "$prefix-*.png" | Remove-Item
    $ErrorActionPreference = "Continue"
    & $tool.Source -png -r 100 $pdf (Join-Path $outDir $prefix) 2>$null | Out-Null
    $ErrorActionPreference = "Stop"
    @(Get-ChildItem $outDir -Filter "$prefix-*.png" | Sort-Object Name)
}
# Share of clearly non-white pixels in an image.
function Get-InkShare([string]$png) {
    Add-Type -AssemblyName System.Drawing
    $image = [System.Drawing.Bitmap]::FromFile($png)
    try {
        $dark = 0; $total = 0
        for ($y = 0; $y -lt $image.Height; $y += 2) {
            for ($x = 0; $x -lt $image.Width; $x += 2) {
                $total++
                $pixel = $image.GetPixel($x, $y)
                if (($pixel.R + $pixel.G + $pixel.B) -lt 720) { $dark++ }
            }
        }
        return $dark / $total
    } finally { $image.Dispose() }
}

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
    & "$PSScriptRoot/screenshot.ps1" -Dialog -Out (Join-Path $outDir "cp5-export-dialog.png") | Out-Null
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

try {
    foreach ($number in $Checkpoint.Split(",")) { & "Checkpoint$($number.Trim())" }
} finally {
    Stop-App
}
Write-Output $(if ($script:failures -eq 0) { "SELFTEST OK" } else { "SELFTEST FEHLER: $script:failures" })
exit $script:failures
