# Drives the running app for self-tests: window placement, keyboard, mouse, toolbar controls (UI Automation).
# Coordinates are WPF units (DIPs) relative to the window's client area. Examples:
#   tools/app-control.ps1 -Place "20,10,1500,1000"         window rectangle in physical pixels
#   tools/app-control.ps1 -Keys "ctrl+z"                    chord: ctrl, shift, alt, enter, space, esc, tab, plus, minus, f4, letters, digits
#   tools/app-control.ps1 -Text "C:\tmp\test.msp"           types text (file dialogs)
#   tools/app-control.ps1 -Click PressureCheck              invokes/toggles/selects a control by AutomationId (x:Name)
#   tools/app-control.ps1 -Wheel "500,400,120" -Hold ctrl   mouse wheel at a point, optional held key
#   tools/app-control.ps1 -Drag "middle,500,400,500,250"    mouse drag (left or middle), optional -Hold space
#   tools/app-control.ps1 -Bounds                           prints the window rectangle in physical pixels (does not activate)
#   tools/app-control.ps1 -Read ZoomText                    prints the Name (text) of a control, "Window" = title of the foreground app window
param(
    [string]$Place,
    [string]$Keys,
    [string]$Text,
    [string]$Click,
    [string]$Wheel,
    [string]$Drag,
    [string]$Hold,
    [string]$Read,
    [switch]$Bounds,
    [string]$ProcessName = "Mitschreibprogramm"
)

$ErrorActionPreference = "Stop"
if (-not ("MspNative" -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot "MspNative.cs") -ReferencedAssemblies System.Drawing }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$virtualKeys = @{ ctrl = 0x11; shift = 0x10; alt = 0x12; enter = 0x0D; space = 0x20; esc = 0x1B; tab = 0x09; plus = 0xBB; minus = 0xBD; f4 = 0x73 }
function Get-VirtualKey([string]$name) {
    $key = $name.Trim().ToLowerInvariant()
    if ($virtualKeys.ContainsKey($key)) { return [uint16]$virtualKeys[$key] }
    if ($key.Length -eq 1) { return [uint16][char]$key.ToUpperInvariant() }
    throw "Unbekannte Taste '$name'."
}
function Get-Numbers([string]$list) {
    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    return @($list.Split(",") | ForEach-Object { [double]::Parse($_.Trim(), $invariant) })
}
function Find-Control([IntPtr]$window, [string]$automationId) {
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($window)
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $element = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if (-not $element) { throw "Control '$automationId' nicht gefunden." }
    return $element
}

$hwnd = [MspNative]::FindAppWindow($ProcessName)
if ($Bounds) {
    Write-Output ([MspNative]::Bounds($hwnd))
    return
}
if ($Place) {
    $r = Get-Numbers $Place
    [MspNative]::Place($hwnd, [int]$r[0], [int]$r[1], [int]$r[2], [int]$r[3])
}
if (-not [MspNative]::Activate($hwnd)) { throw "App-Fenster liess sich nicht in den Vordergrund holen." }

if ($Click) {
    $element = Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $Click
    $pattern = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) { $pattern.Select() }
    elseif ($element.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) { $pattern.Toggle() }
    elseif ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) { $pattern.Invoke() }
    else { throw "Control '$Click' unterstuetzt weder Select, Toggle noch Invoke." }
    Start-Sleep -Milliseconds 200
}
if ($Read -eq "Window") {
    Write-Output ([System.Windows.Automation.AutomationElement]::FromHandle([MspNative]::ForegroundWindowOf($hwnd))).Current.Name
} elseif ($Read) {
    Write-Output (Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $Read).Current.Name
}

$held = if ($Hold) { Get-VirtualKey $Hold } else { $null }
try {
    if ($held) { [MspNative]::KeyDown($hwnd, $held); Start-Sleep -Milliseconds 80 }
    if ($Keys) {
        [MspNative]::KeyChord($hwnd, [uint16[]]@($Keys.Split("+") | ForEach-Object { Get-VirtualKey $_ }))
    }
    if ($Text) { [MspNative]::TypeText($hwnd, $Text) }
    if ($Wheel) {
        $w = Get-Numbers $Wheel
        [MspNative]::MouseWheel($hwnd, $w[0], $w[1], [int]$w[2])
    }
    if ($Drag) {
        $parts = $Drag.Split(",")
        $d = Get-Numbers (($parts | Select-Object -Skip 1) -join ",")
        [MspNative]::MouseDrag($hwnd, $parts[0].Trim(), $d[0], $d[1], $d[2], $d[3], 20)
    }
} finally {
    if ($held) { [MspNative]::KeyUp($held) }
}
