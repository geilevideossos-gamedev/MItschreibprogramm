# Injects one synthetic pen stroke (Windows 10 1809+, no admin) into the running app window.
# Coordinates are WPF units (DIPs) relative to the top-left corner of the window's client area.
# The stroke is refused if the target pixels do not belong to the app (covered or off-screen window).
# Usage: powershell -ExecutionPolicy Bypass -File tools/pen-sim.ps1 -From "300,300" -To "700,300" -PressureFrom 100 -PressureTo 1000
#   -Barrel    side button held (hover and contact)
#   -Inverted  pen reported as inverted, contact frames carry the eraser flag
param(
    [Parameter(Mandatory = $true)][string]$From,
    [Parameter(Mandatory = $true)][string]$To,
    [int]$PressureFrom = 512,
    [int]$PressureTo = 512,
    [int]$Steps = 40,
    [int]$DelayMs = 8,
    [switch]$Barrel,
    [switch]$Inverted,
    [string]$ProcessName = "Mitschreibprogramm"
)

$ErrorActionPreference = "Stop"
Add-Type -Path (Join-Path $PSScriptRoot "MspNative.cs") -ReferencedAssemblies System.Drawing

$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$start = $From.Split(",") | ForEach-Object { [double]::Parse($_, $invariant) }
$end = $To.Split(",") | ForEach-Object { [double]::Parse($_, $invariant) }

$hwnd = [MspNative]::FindAppWindow($ProcessName)
if (-not [MspNative]::Activate($hwnd)) { throw "App-Fenster liess sich nicht in den Vordergrund holen." }
$result = [MspNative]::PenStroke($hwnd, $start[0], $start[1], $end[0], $end[1],
    $PressureFrom, $PressureTo, $Steps, $DelayMs, $Barrel.IsPresent, $Inverted.IsPresent)
Write-Output "pen-sim $result pressure=$PressureFrom..$PressureTo barrel=$($Barrel.IsPresent) inverted=$($Inverted.IsPresent)"
