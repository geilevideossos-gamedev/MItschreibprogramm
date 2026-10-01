# Drives the running app for self-tests: window placement, keyboard, mouse, toolbar controls (UI Automation).
# Coordinates are WPF units (DIPs) relative to the window's client area. Examples:
#   tools/app-control.ps1 -Place "20,10,1500,1000"         window rectangle in physical pixels
#   tools/app-control.ps1 -Keys "ctrl+z"                    chord: ctrl, shift, alt, enter, space, esc, tab, plus, minus, f4, letters, digits
#   tools/app-control.ps1 -Text "C:\tmp\test.msp"           types text (file dialogs)
#   tools/app-control.ps1 -Click PressureCheck              invokes/toggles/selects a control by AutomationId (x:Name)
#   tools/app-control.ps1 -Wheel "500,400,120" -Hold ctrl   mouse wheel at a point, optional held key
#   tools/app-control.ps1 -Drag "middle,500,400,500,250"    mouse drag (left or middle), optional -Hold space
#   tools/app-control.ps1 -DragPath "300,300;500,300;400,450;300,300"  left-button drag along a polyline (mouse lasso)
#   tools/app-control.ps1 -Bounds                           prints the window rectangle in physical pixels (does not activate)
#   tools/app-control.ps1 -Pixel "500,300"                  prints the screen colour at a client position as r,g,b
#   tools/app-control.ps1 -Read ZoomText                    prints the Name (text) of a control, "Window" = title of the foreground app window
#   tools/app-control.ps1 -RightClick <id>                  right mouse click on the centre of a control (context menu)
#   tools/app-control.ps1 -DoubleClick <id>                 left double click on the centre of a control
#   tools/app-control.ps1 -Value 1001                       prints the text of a control (Value pattern, or a Win32 Edit by control id in a file dialog), "" if absent
#   tools/app-control.ps1 -Checked ToolSelect              prints True or False: radio button selected or toggle button on
#   tools/app-control.ps1 -Exists NotebookList              prints True or False (collapsed controls are not in the UI Automation tree)
#   tools/app-control.ps1 -Items NotebookList               prints the names of a list's items, one per line, in display order
param(
    [string]$Place,
    [string]$Keys,
    [string]$Text,
    [string]$Click,
    [string]$RightClick,
    [string]$DoubleClick,
    [string]$Value,
    [string]$Wheel,
    [string]$Drag,
    [string]$DragPath,
    [string]$Hold,
    [string]$Read,
    [string]$Exists,
    [string]$Checked,
    [string]$Items,
    [string]$Pixel,
    [switch]$Bounds,
    [string]$ProcessName = "Mitschreibprogramm"
)

$ErrorActionPreference = "Stop"
if (-not ("MspNative" -as [type])) { Add-Type -Path (Get-ChildItem $PSScriptRoot -Filter "MspNative.*.cs").FullName -ReferencedAssemblies System.Drawing }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$virtualKeys = @{ ctrl = 0x11; shift = 0x10; alt = 0x12; enter = 0x0D; space = 0x20; esc = 0x1B; tab = 0x09; plus = 0xBB; minus = 0xBD; f4 = 0x73; delete = 0x2E }
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
# Looks in the given window first, then in every other top-level window of the app (dialogs, context menu popups).
function Find-Control([IntPtr]$window, [string]$automationId, [switch]$Optional) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    $element = $null
    if ($window -ne [IntPtr]::Zero) {
        $element = [System.Windows.Automation.AutomationElement]::FromHandle($window).FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    }
    if (-not $element) {
        $sameProcess = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, [MspNative]::ProcessIdOf($hwnd))
        foreach ($top in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $sameProcess)) {
            $element = $top.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
            if ($element) { break }
        }
    }
    if (-not $element -and -not $Optional) { throw "Control '$automationId' nicht gefunden." }
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
if ($RightClick) {
    $rect = (Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $RightClick).Current.BoundingRectangle
    [MspNative]::MouseClickAt($hwnd, "right", [int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
    Start-Sleep -Milliseconds 400
}
if ($DoubleClick) {
    $rect = (Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $DoubleClick).Current.BoundingRectangle
    $x = [int]($rect.X + $rect.Width / 2); $y = [int]($rect.Y + $rect.Height / 2)
    [MspNative]::MouseClickAt($hwnd, "left", $x, $y)
    [MspNative]::MouseClickAt($hwnd, "left", $x, $y)
    Start-Sleep -Milliseconds 400
}
if ($Value) {
    # The common file dialogs hide their file name box from the managed UI Automation client; a numeric id is then
    # looked up as Win32 control id among the dialog's Edit windows.
    $element = Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $Value -Optional
    $pattern = $null
    $text = ""
    if ($element -and $element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) { $text = $pattern.Current.Value }
    elseif ($Value -match "^\d+$") {
        $line = @([MspNative]::EditTexts([MspNative]::ForegroundWindowOf($hwnd))) | Where-Object { $_ -like "$Value|*" } | Select-Object -First 1
        if ($line) { $text = ($line -split "\|", 3)[2] }
    }
    Write-Output $text
}
if ($Pixel) {
    $at = Get-Numbers $Pixel
    Write-Output ([MspNative]::ClientPixel($hwnd, $at[0], $at[1]))
}
if ($Exists) {
    $element = Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $Exists -Optional
    Write-Output ([string]($null -ne $element -and -not $element.Current.IsOffscreen))
}
if ($Checked) {
    $element = Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $Checked
    $pattern = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) { Write-Output ([string]$pattern.Current.IsSelected) }
    elseif ($element.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) { Write-Output ([string]($pattern.Current.ToggleState -eq "On")) }
    else { throw "Control '$Checked' hat keinen Auswahl- oder Schaltzustand." }
}
if ($Items) {
    $list = Find-Control ([MspNative]::ForegroundWindowOf($hwnd)) $Items
    $itemCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
    foreach ($item in $list.FindAll([System.Windows.Automation.TreeScope]::Children, $itemCondition)) { Write-Output $item.Current.Name }
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
    if ($DragPath) {
        $pairs = @($DragPath.Split(";") | ForEach-Object { ,(Get-Numbers $_) })
        [MspNative]::MouseDragPath($hwnd, [double[]]@($pairs | ForEach-Object { $_[0] }), [double[]]@($pairs | ForEach-Object { $_[1] }), 6)
    }
    if ($Drag) {
        $parts = $Drag.Split(",")
        $d = Get-Numbers (($parts | Select-Object -Skip 1) -join ",")
        [MspNative]::MouseDrag($hwnd, $parts[0].Trim(), $d[0], $d[1], $d[2], $d[3], 20)
    }
} finally {
    if ($held) { [MspNative]::KeyUp($held) }
}
