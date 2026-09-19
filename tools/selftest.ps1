# Runs the self-test checkpoints (docs/testing.md) against an exe: synthetic pen, keys, mouse, debug log, screenshots.
# Usage: powershell -ExecutionPolicy Bypass -File tools/selftest.ps1 [-Exe dist/Mitschreibprogramm.exe] [-Checkpoint 1,2]
# Needs an unlocked desktop. Nobody may use mouse or keyboard while it runs: it waits for an idle machine before it
# starts and stops sending keys the moment another program comes to the foreground. Screenshots land in tmp/selftest/.
param(
    [string]$Exe = "Mitschreibprogramm/bin/Debug/net8.0-windows/Mitschreibprogramm.exe",
    [string]$Checkpoint = "1,2,3,4,5,7",
    [int]$IdleSeconds = 15,
    [int]$IdleWaitSeconds = 120
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot
$exePath = if ([System.IO.Path]::IsPathRooted($Exe)) { $Exe } else { Join-Path $root $Exe }
$outDir = Join-Path $root "tmp/selftest"
$logPath = Join-Path $outDir "debug.log"
$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$script:failures = 0
$script:logOffset = 0
$script:windowEvents = @()
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# On a failure the window events since the last check are printed: a foreign window taking the foreground
# aborts a running stroke or pan, and that is the first thing to rule out.
function Check([bool]$condition, [string]$message) {
    if ($condition) { Write-Output "  PASS  $message" } else {
        Write-Output "  FAIL  $message"
        $script:failures++
        foreach ($event in $script:windowEvents) { Write-Output "        $event" }
    }
    $script:windowEvents = @()
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
    foreach ($attempt in 1..120) {
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
    $fresh = @($lines | Select-Object -Skip $script:logOffset)
    $script:windowEvents += @($fresh | Where-Object { $_ -match "^\S+ window " })
    $new = @($fresh | Where-Object { $_ -notmatch "^\S+ window " })
    $script:logOffset = $lines.Count
    foreach ($line in $new) {
        $entry = @{ raw = $line; kind = ($line -split " ")[1] }
        foreach ($match in [regex]::Matches($line, "(\w+)=(\([^)]*\)|\S+)")) { $entry[$match.Groups[1].Value] = $match.Groups[2].Value }
        $entry
    }
}
# A missing log entry yields NaN, so the affected checks fail instead of stopping the whole run.
function Number([string]$value) { if ($value) { [double]::Parse($value, $invariant) } else { [double]::NaN } }
function PointOf([string]$value) { if ($value) { @($value.Trim("(", ")").Split(",") | ForEach-Object { Number $_ }) } else { @([double]::NaN, [double]::NaN) } }

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

# Sum of r, g and b (0 = black, 765 = white) of the screen pixel at a page position.
function Get-PageBrightness($origin, [double]$x, [double]$y) {
    $client = "{0},{1}" -f ($origin.X + $origin.Zoom * $x).ToString($invariant), ($origin.Y + $origin.Zoom * $y).ToString($invariant)
    Start-Sleep -Milliseconds 300
    $rgb = @((Ctl -Pixel $client).Split(",") | ForEach-Object { [int]$_ })
    $rgb[0] + $rgb[1] + $rgb[2]
}

foreach ($part in Get-ChildItem (Join-Path $PSScriptRoot "selftest") -Filter "checkpoint-*.ps1") { . $part.FullName }

# The run takes over mouse and keyboard. It only starts on a machine nobody is using right now, and gives up otherwise.
if (-not ("MspNative" -as [type])) { Add-Type -Path (Get-ChildItem $PSScriptRoot -Filter "MspNative.*.cs").FullName -ReferencedAssemblies System.Drawing }
[MspNative]::KeepAwake()
$waited = 0
while ([MspNative]::IdleSeconds() -lt $IdleSeconds) {
    if ($waited -ge $IdleWaitSeconds) {
        Write-Output "ABBRUCH: Maus oder Tastatur werden gerade benutzt. Der Selbsttest startet erst nach $IdleSeconds s ohne Eingabe."
        exit 99
    }
    Start-Sleep -Seconds 2
    $waited += 2
}

try {
    foreach ($number in $Checkpoint.Split(",")) { & "Checkpoint$($number.Trim())" }
} finally {
    Stop-App
}
Write-Output $(if ($script:failures -eq 0) { "SELFTEST OK" } else { "SELFTEST FEHLER: $script:failures" })
exit $script:failures
