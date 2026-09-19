# Saves the app window as PNG (rendered via PrintWindow, so other windows on top do not matter).
# Usage: powershell -ExecutionPolicy Bypass -File tools/screenshot.ps1 -Out tmp/shot.png
#   -Dialog  capture the app's foreground window instead (modal dialog)
#   -Screen  capture the whole screen
param(
    [string]$Out = "tmp/screenshot.png",
    [string]$ProcessName = "Mitschreibprogramm",
    [switch]$Dialog,
    [switch]$Screen
)

$ErrorActionPreference = "Stop"
Add-Type -Path (Join-Path $PSScriptRoot "MspNative.cs") -ReferencedAssemblies System.Drawing

$target = if ([System.IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path (Get-Location) $Out }
New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null

if ($Screen) {
    $size = [MspNative]::CaptureScreen($target)
} else {
    $hwnd = [MspNative]::FindAppWindow($ProcessName)
    if ($Dialog) {
        $hwnd = [MspNative]::ForegroundWindowOf($hwnd)
        if ($hwnd -eq [IntPtr]::Zero) { throw "Kein Vordergrundfenster der App gefunden." }
    }
    $size = [MspNative]::Capture($hwnd, $target)
}
Write-Output "screenshot $target ($size)"
