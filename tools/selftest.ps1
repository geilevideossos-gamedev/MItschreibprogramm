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

function Start-App {
    Stop-App
    Remove-Item $logPath -ErrorAction SilentlyContinue
    $script:logOffset = 0
    $env:MSP_DEBUG_LOG = $logPath
    try { Start-Process -FilePath $exePath } finally { $env:MSP_DEBUG_LOG = $null }
    foreach ($attempt in 1..40) {
        Start-Sleep -Milliseconds 250
        if (Get-Process Mitschreibprogramm -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero }) { break }
    }
    Start-Sleep -Milliseconds 800
    Ctl -Place "20,10,1500,1000"
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

foreach ($number in $Checkpoint.Split(",")) { & "Checkpoint$($number.Trim())" }
Write-Output $(if ($script:failures -eq 0) { "SELFTEST OK" } else { "SELFTEST FEHLER: $script:failures" })
exit $script:failures
