# Dot-sourced by tools/selftest.ps1, uses the helpers of checkpoint-8.ps1, checkpoint-9.ps1 and checkpoint-10.ps1.
# Puts a test picture on the clipboard: only text on the clipboard is restored afterwards.

function Get-ActiveTool { foreach ($name in @("ToolPen", "ToolEraser", "ToolSelect")) { if ((Ctl -Checked $name) -eq "True") { return $name } } }
# A lasso with the upper side button that lets go of the button at the lift, or holdMs before it.
function ButtonLasso($origin, [double]$left, [double]$top, [double]$right, [double]$bottom, [int]$holdMs = 0) {
    PagePath $origin @(@($left, $top), @($right, $top), @($right, $bottom), @($left, $bottom), @($left, ($top + 2))) -Barrel -ReleaseBarrel -HoldMs $holdMs
}
function Get-LastStrokeSpan([string]$file) {
    $stroke = Get-LastStroke $file
    $xs = @($stroke.points | ForEach-Object { $_[0] }) | Measure-Object -Minimum -Maximum
    @{ X = $xs.Minimum; W = $xs.Maximum - $xs.Minimum }
}
function Alive { $null -ne (Get-Process Mitschreibprogramm -ErrorAction SilentlyContinue | Where-Object { $_.Responding }) }

function Checkpoint11 {
    Write-Output "Checkpoint 11: Lasso mit oberer Taste aus Stift und Radierer"
    Add-Type -AssemblyName System.Windows.Forms
    $savedText = if ([System.Windows.Forms.Clipboard]::ContainsText()) { [System.Windows.Forms.Clipboard]::GetText() } else { $null }
    try {
        Checkpoint11Strokes
        Checkpoint11Picture
    } finally {
        if ($savedText) { [System.Windows.Forms.Clipboard]::SetText($savedText) } else { [System.Windows.Forms.Clipboard]::Clear() }
    }
}

function Checkpoint11Strokes {
    Start-App
    $origin = Get-PageOrigin 500 110
    foreach ($tool in @(@{ Key = "p"; Name = "ToolPen"; Label = "Stift"; Top = 360; Hold = 0 }, @{ Key = "e"; Name = "ToolEraser"; Label = "Radierer"; Top = 160; Hold = 150 })) {
        $top = $tool.Top
        Ctl -Keys "p"
        PagePath $origin @(@(100, ($top + 20)), @(200, ($top + 80)), @(300, ($top + 20)))
        Ctl -Keys $tool.Key
        Read-Log | Out-Null
        ButtonLasso $origin 80 $top 320 ($top + 100) $tool.Hold
        $lasso = @(Read-Log | Where-Object { $_.kind -eq "selection" })
        $switched = Get-ActiveTool
        Check ($lasso.Count -ge 1 -and $lasso[-1].strokes -eq "1" -and $switched -eq "ToolSelect" -and (Alive)) "$($tool.Label): Lasso mit oberer Taste (losgelassen $($tool.Hold) ms vor dem Abheben) waehlt den Strich, Toolbar zeigt Auswahl ($switched)"
        if ($tool.Key -eq "p") { Shot "cp11-auto-select" }
        $start = Get-Rect $lasso[-1]

        PagePath $origin @(@(200, ($top + 50)), @(260, ($top + 100)))
        $moved = @(Selection "moved")
        $after = if ($moved.Count) { Get-Rect $moved[-1] } else { @{ X = [double]::NaN; Y = 0; W = 0; H = 0 } }
        Check ([Math]::Abs($after.X - $start.X - 60) -lt 3 -and [Math]::Abs($after.Y - $start.Y - 50) -lt 3) "$($tool.Label): Stift ohne Taste verschiebt die Auswahl um 60,50 ($($moved.raw))"
        $cornerX = $after.X + $after.W + 8; $cornerY = $after.Y + $after.H + 8
        PagePath $origin @(@($cornerX, $cornerY), @(($cornerX + 80), ($cornerY + 20)))
        $resized = @(Selection "resized")
        $grown = if ($resized.Count) { Get-Rect $resized[-1] } else { @{ W = 0 } }
        Check ($grown.W -gt $after.W + 40) "$($tool.Label): Eckgriff mit dem Stift skaliert ($([Math]::Round($after.W)) zu $([Math]::Round($grown.W)))"

        Ctl -Keys "esc"
        $cleared = @(Read-Log | Where-Object { $_.kind -eq "selection" })
        $back = Get-ActiveTool
        Check ($cleared.Count -ge 1 -and $cleared[-1].strokes -eq "0" -and $back -eq $tool.Name) "$($tool.Label): Esc hebt die Auswahl auf, Toolbar zeigt wieder $($tool.Label) ($back)"
        Ctl -Keys "ctrl+s"
        $file = Get-NewestNote
        $big = Get-LastStrokeSpan $file
        Ctl -Keys "ctrl+z"; Ctl -Keys "ctrl+z"; Ctl -Keys "ctrl+s"
        $undone = Get-LastStrokeSpan $file
        Ctl -Keys "ctrl+y"; Ctl -Keys "ctrl+y"; Ctl -Keys "ctrl+s"
        $redone = Get-LastStrokeSpan $file
        Check ($big.W -gt 240 -and [Math]::Abs($undone.X - 100) -lt 2 -and [Math]::Abs($undone.W - 200) -lt 2 -and [Math]::Abs($redone.W - $big.W) -lt 1 -and (Get-ActiveTool) -eq $tool.Name) "$($tool.Label): Strg+Z nimmt Skalieren und Verschieben zurueck, Strg+Y stellt beides her (x $([Math]::Round($big.X)) / $([Math]::Round($undone.X)), Breite $([Math]::Round($big.W)) / $([Math]::Round($undone.W)) / $([Math]::Round($redone.W)))"

        Ctl -Keys "ctrl+z"; Ctl -Keys "ctrl+z"
        Read-Log | Out-Null
        ButtonLasso $origin 80 $top 320 ($top + 100)
        $before = Saved
        Ctl -Keys "delete"
        $deleted = @(Selection "deleted")
        $back = Get-ActiveTool
        $gone = Saved
        Undo ($deleted.Count -eq 1)
        $restored = Saved
        Check ($deleted.Count -eq 1 -and $deleted[0].strokes -eq "1" -and $gone.Strokes -eq $before.Strokes - 1 -and $restored.Strokes -eq $before.Strokes -and $back -eq $tool.Name) "$($tool.Label): Entf loescht die Taste-Auswahl, Toolbar zeigt wieder $($tool.Label), Strg+Z holt den Strich zurueck"

        Read-Log | Out-Null
        ButtonLasso $origin 80 $top 320 ($top + 100)
        PageTap $origin 600 ($top + 50) 0
        Start-Sleep -Milliseconds 500
        $tapped = @(Read-Log | Where-Object { $_.kind -eq "selection" })
        $back = Get-ActiveTool
        Check ((Alive) -and $tapped.Count -ge 2 -and $tapped[-1].strokes -eq "0" -and $back -eq $tool.Name) "$($tool.Label): Stift-Tippen daneben (der Absturz-Fall) hebt die Auswahl auf, App laeuft, Toolbar zeigt wieder $($tool.Label)"
    }

    Ctl -Keys "s"
    ButtonLasso $origin 80 360 320 460
    $kept = Get-ActiveTool
    Ctl -Keys "esc"
    Check ($kept -eq "ToolSelect" -and (Get-ActiveTool) -eq "ToolSelect") "Mit Werkzeug Auswahl bleibt Auswahl auch nach Lasso mit Taste und Esc aktiv"
    Stop-App
}

function Checkpoint11Picture {
    Start-App
    $origin = Get-PageOrigin 500 110
    Set-TestImageOnClipboard
    Ctl -Keys "ctrl+v"
    $pasted = @(Read-Log | Where-Object { $_.kind -eq "image" })[0]
    $x = Number $pasted.x; $y = Number $pasted.y
    PagePath $origin @(@(($x + 40), ($y + 80)), @(($x + 360), ($y + 80)))
    Ctl -Keys "e"
    Read-Log | Out-Null
    ButtonLasso $origin ($x - 20) ($y - 20) ($x + 420) ($y + 320)
    $lasso = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    $switched = Get-ActiveTool
    Check ($lasso.Count -ge 1 -and $lasso[-1].strokes -eq "1" -and $lasso[-1].images -eq "1" -and $switched -eq "ToolSelect") "Radierer: Lasso mit Taste waehlt Bild und Strich, Toolbar zeigt Auswahl ($($lasso[-1].raw))"
    $start = Get-Rect $lasso[-1]
    PagePath $origin @(@(($x + 200), ($y + 200)), @(($x + 260), ($y + 250)))
    $moved = @(Selection "moved")
    $after = if ($moved.Count) { Get-Rect $moved[-1] } else { @{ X = [double]::NaN; Y = 0; W = 1; H = 1 } }
    Check ([Math]::Abs($after.X - $start.X - 60) -lt 3 -and [Math]::Abs($after.Y - $start.Y - 50) -lt 3 -and @(Read-Log | Where-Object { $_.raw -match "image erased" }).Count -eq 0) "Bild-Auswahl mit dem Stift verschoben, nichts radiert"
    $cornerX = $after.X + $after.W + 8; $cornerY = $after.Y + $after.H + 8
    PagePath $origin @(@($cornerX, $cornerY), @(($cornerX + 80), ($cornerY + 20)))
    $resized = @(Selection "resized")
    $grown = if ($resized.Count) { Get-Rect $resized[-1] } else { @{ W = 0; H = 1 } }
    Check ($grown.W -gt $after.W + 40 -and [Math]::Abs(($grown.W / $grown.H) - ($after.W / $after.H)) -lt 0.02) "Bild-Auswahl am Eckgriff skaliert, Seitenverhaeltnis bleibt"
    Ctl -Keys "esc"
    $back = Get-ActiveTool
    Ctl -Keys "ctrl+s"
    $file = Get-NewestNote
    $big = @(Get-Picture $file)[0]
    Ctl -Keys "ctrl+z"; Ctl -Keys "ctrl+z"; Ctl -Keys "ctrl+s"
    $small = @(Get-Picture $file)[0]
    Check ($back -eq "ToolEraser" -and $big.width -gt 440 -and [Math]::Abs($small.width - 400) -lt 1 -and [Math]::Abs($small.x - $x) -lt 1) "Esc: Toolbar zeigt wieder Radierer; Strg+Z nimmt Skalieren und Verschieben des Bilds zurueck ($($big.width) zu $($small.width))"

    Read-Log | Out-Null
    ButtonLasso $origin ($x - 20) ($y - 20) ($x + 420) ($y + 320)
    Ctl -Keys "delete"
    $deleted = @(Selection "deleted")
    $back = Get-ActiveTool
    $gone = Saved
    Undo ($deleted.Count -eq 1)
    $restored = Saved
    Check ($deleted.Count -eq 1 -and $deleted[0].images -eq "1" -and $gone.Images -eq 0 -and $restored.Images -eq 1 -and $back -eq "ToolEraser") "Entf loescht Bild und Strich der Taste-Auswahl, Toolbar zeigt wieder Radierer, Strg+Z holt beides zurueck"
    Stop-App
}
