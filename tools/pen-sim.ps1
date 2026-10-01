# Injects one synthetic pen stroke (Windows 10 1809+, no admin) into the running app window.
# Coordinates are WPF units (DIPs) relative to the top-left corner of the window's client area.
# The stroke is refused if the target pixels do not belong to the app (covered or off-screen window).
# Usage: powershell -ExecutionPolicy Bypass -File tools/pen-sim.ps1 -From "300,300" -To "700,300" -PressureFrom 100 -PressureTo 1000
#   -Path "x,y;x,y;..."  a polyline instead of From/To, constant pressure (PressureFrom), about one frame per 6 screen pixels
#   -HoldMs 700          with -Path: keep the pen down and still on the last point before lifting
#   -Barrel    side button held (hover and contact)
#   -ReleaseBarrel  with -Path -Barrel: let go of the side button at the lift, or HoldMs before it
#   -Inverted  pen reported as inverted, contact frames carry the eraser flag
param(
    [string]$From,
    [string]$To,
    [string]$Path,
    [int]$HoldMs = 0,
    [int]$PressureFrom = 512,
    [int]$PressureTo = 512,
    [int]$Steps = 40,
    [int]$DelayMs = 8,
    [switch]$Barrel,
    [switch]$Inverted,
    [switch]$ReleaseBarrel,
    [string]$ProcessName = "Mitschreibprogramm"
)

$ErrorActionPreference = "Stop"
if (-not ("MspNative" -as [type])) { Add-Type -Path (Get-ChildItem $PSScriptRoot -Filter "MspNative.*.cs").FullName -ReferencedAssemblies System.Drawing }

$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$hwnd = [MspNative]::FindAppWindow($ProcessName)
if (-not [MspNative]::Activate($hwnd)) { throw "App-Fenster liess sich nicht in den Vordergrund holen." }
if ($Path) {
    $pairs = @($Path.Split(";") | ForEach-Object { ,@($_.Split(",") | ForEach-Object { [double]::Parse($_, $invariant) }) })
    $result = [MspNative]::PenPath($hwnd, [double[]]@($pairs | ForEach-Object { $_[0] }), [double[]]@($pairs | ForEach-Object { $_[1] }),
        $PressureFrom, 6, $DelayMs, $Barrel.IsPresent, $Inverted.IsPresent, $HoldMs, $ReleaseBarrel.IsPresent)
} else {
    $start = $From.Split(",") | ForEach-Object { [double]::Parse($_, $invariant) }
    $end = $To.Split(",") | ForEach-Object { [double]::Parse($_, $invariant) }
    $result = [MspNative]::PenStroke($hwnd, $start[0], $start[1], $end[0], $end[1],
        $PressureFrom, $PressureTo, $Steps, $DelayMs, $Barrel.IsPresent, $Inverted.IsPresent)
}
Write-Output "pen-sim $result pressure=$PressureFrom..$PressureTo barrel=$($Barrel.IsPresent) inverted=$($Inverted.IsPresent)"
