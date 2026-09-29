# Dot-sourced by tools/selftest.ps1, uses its helpers and those of checkpoint-8.ps1 and checkpoint-9.ps1.
# Puts a test picture on the clipboard: only text on the clipboard is restored afterwards.

# A pen tap at a page position that wanders by wobble units. It rests 70 ms on the page like a real tap: a down
# directly followed by an up, without contact packets, often never reaches the app (docs/status.md).
function PageTap($origin, [double]$x, [double]$y, [double]$wobble, [switch]$Barrel) {
    $points = @(@($x, $y), @(($x + $wobble), $y))
    $path = ($points | ForEach-Object { "{0},{1}" -f ($origin.X + $origin.Zoom * $_[0]).ToString($invariant), ($origin.Y + $origin.Zoom * $_[1]).ToString($invariant) }) -join ";"
    & "$toolsDir/pen-sim.ps1" -Path $path -HoldMs 70 -Barrel:$Barrel | Out-Null
    Start-Sleep -Milliseconds 700
}
function Get-Counts([string]$file) {
    $pages = (Get-Content $file -Raw | ConvertFrom-Json).pages
    @{ Images = @($pages | ForEach-Object { $_.images }).Count; Strokes = @($pages | ForEach-Object { $_.strokes }).Count }
}
function Saved { Ctl -Keys "ctrl+s"; Start-Sleep -Milliseconds 300; Get-Counts (Get-NewestNote) }
# The top of the window (title bar and both toolbar rows) of several screenshots, stacked into one picture.
function Join-Toolbars([string[]]$names, [string]$target) {
    Add-Type -AssemblyName System.Drawing
    $shots = @($names | ForEach-Object { [System.Drawing.Bitmap]::FromFile((Join-Path $outDir "$_.png")) })
    try {
        $height = 135
        $sheet = New-Object System.Drawing.Bitmap $shots[0].Width, ($height * $shots.Count)
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        for ($i = 0; $i -lt $shots.Count; $i++) {
            $graphics.DrawImage($shots[$i], (New-Object System.Drawing.Rectangle 0, ($i * $height), $shots[$i].Width, $height), (New-Object System.Drawing.Rectangle 0, 0, $shots[$i].Width, $height), [System.Drawing.GraphicsUnit]::Pixel)
        }
        $graphics.Dispose()
        $sheet.Save((Join-Path $outDir "$target.png"))
        $sheet.Dispose()
    } finally { $shots | ForEach-Object { $_.Dispose() } }
}

function Checkpoint10 {
    Write-Output "Checkpoint 10: Bilder loeschen (Tippen, Lasso, Radierer) und Toolbar"
    Add-Type -AssemblyName System.Windows.Forms
    $savedText = if ([System.Windows.Forms.Clipboard]::ContainsText()) { [System.Windows.Forms.Clipboard]::GetText() } else { $null }
    try { Checkpoint10Steps } finally {
        if ($savedText) { [System.Windows.Forms.Clipboard]::SetText($savedText) } else { [System.Windows.Forms.Clipboard]::Clear() }
    }
}

function Checkpoint10Steps {
    Start-App
    $origin = Get-PageOrigin 500 110
    Set-TestImageOnClipboard
    Ctl -Keys "ctrl+v"
    $pasted = @(Read-Log | Where-Object { $_.kind -eq "image" })[0]
    $x = Number $pasted.x; $y = Number $pasted.y
    $client = "{0},{1}" -f ($origin.X + $x + 100).ToString($invariant), ($origin.Y + $y + 150).ToString($invariant)

    Ctl -Keys "s"
    Read-Log | Out-Null
    PageTap $origin ($x + 100) ($y + 150) 0
    $tap = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Check ($tap.Count -ge 1 -and $tap[-1].images -eq "1" -and $tap[-1].strokes -eq "0") "Auswahl: Stift-Tippen waehlt das Bild ($($tap.raw))"
    Ctl -Keys "delete"
    $deleted = @(Selection "deleted")
    $gone = Saved
    Ctl -Keys "ctrl+z"
    $back = Saved
    Check ($deleted.Count -eq 1 -and $deleted[0].images -eq "1" -and $gone.Images -eq 0 -and $back.Images -eq 1) "Entf loescht das getippte Bild, Strg+Z holt es zurueck"

    Read-Log | Out-Null
    Ctl -Drag "left,$client,$client"
    $click = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Ctl -Keys "delete"
    $gone = Saved
    Ctl -Keys "ctrl+z"
    $back = Saved
    Check ($click.Count -ge 1 -and $click[-1].images -eq "1" -and $gone.Images -eq 0 -and $back.Images -eq 1) "Mausklick waehlt das Bild, Entf loescht, Strg+Z holt es zurueck"

    PageTap $origin ($x + 100) ($y + 150) 0
    PageTap $origin ($x - 60) ($y + 150) 0
    $beside = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Check ($beside.Count -ge 2 -and $beside[-1].images -eq "0" -and $beside[-1].strokes -eq "0") "Stift-Tippen daneben hebt die Auswahl auf"

    Ctl -Keys "p"
    Read-Log | Out-Null
    PageTap $origin ($x + 100) ($y + 150) 10 -Barrel
    $barrel = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Ctl -Keys "delete"
    $gone = Saved
    Ctl -Keys "ctrl+z"
    $back = Saved
    Check ($barrel.Count -ge 1 -and $barrel[-1].images -eq "1" -and $gone.Images -eq 0 -and $back.Images -eq 1) "Tippen mit oberer Taste und 10 Einheiten Bewegung waehlt das Bild, Entf loescht, Strg+Z holt es zurueck (der Fall aus dem Bug)"

    Ctl -Keys "p"
    PagePath $origin @(@(($x + 40), ($y + 80)), @(($x + 360), ($y + 80)))
    PagePath $origin @(@(($x + 40), ($y + 220)), @(($x + 360), ($y + 220)))
    Read-Log | Out-Null
    $before = Saved
    PagePath $origin @(@(($x - 20), ($y - 20)), @(($x + 420), ($y - 20)), @(($x + 420), ($y + 320)), @(($x - 20), ($y + 320)), @(($x - 20), ($y - 18))) -Barrel
    $lasso = @(Read-Log | Where-Object { $_.kind -eq "selection" })
    Ctl -Keys "delete"
    $deleted = @(Selection "deleted")
    $gone = Saved
    Ctl -Keys "ctrl+z"
    $back = Saved
    Check ($lasso.Count -ge 1 -and $lasso[-1].strokes -eq "2" -and $lasso[-1].images -eq "1") "Lasso mit Seitentaste waehlt Bild und beide Striche ($($lasso[-1].raw))"
    Check ($deleted.Count -eq 1 -and $deleted[0].strokes -eq "2" -and $deleted[0].images -eq "1" -and $gone.Images -eq 0 -and $gone.Strokes -eq $before.Strokes - 2 -and $back.Images -eq 1 -and $back.Strokes -eq $before.Strokes) "Entf loescht Bild und Striche zusammen, Strg+Z holt alles zurueck ($($before.Strokes) / $($gone.Strokes) / $($back.Strokes) Striche)"

    Ctl -Keys "e"
    PagePath $origin @(@(($x + 150), ($y + 280)), @(($x + 250), ($y + 290)))
    $erased = @(Read-Log | Where-Object { $_.kind -eq "image" -and $_.raw -match "image erased" })
    $gone = Saved
    Ctl -Keys "ctrl+z"
    $back = Saved
    Check ($erased.Count -eq 1 -and $gone.Images -eq 0 -and $back.Images -eq 1 -and $back.Strokes -eq $before.Strokes) "Radierer-Werkzeug loescht das Bild beim Beruehren, Strg+Z holt es zurueck"

    Ctl -Keys "p"
    Read-Log | Out-Null
    PagePath $origin @(@(($x + 150), ($y + 280)), @(($x + 250), ($y + 290))) -Inverted
    $erased = @(Read-Log | Where-Object { $_.kind -eq "image" -and $_.raw -match "image erased" })
    $gone = Saved
    Ctl -Keys "ctrl+z"
    $back = Saved
    Ctl -Keys "ctrl+y"
    $redo = Saved
    Check ($erased.Count -eq 1 -and $gone.Images -eq 0 -and $back.Images -eq 1 -and $redo.Images -eq 0) "Untere Taste (invertiert) loescht das Bild, Strg+Z holt es zurueck, Strg+Y loescht es wieder"
    Ctl -Keys "ctrl+z"

    Check ((Ctl -Read ShapesButton) -eq "Formen: aus") "Schalter zeigt 'Formen: aus'"
    $names = @()
    foreach ($shapes in @("aus", "an")) {
        foreach ($tool in @(@{ Key = "p"; Name = "stift" }, @{ Key = "e"; Name = "radierer" }, @{ Key = "s"; Name = "auswahl" })) {
            Ctl -Keys $tool.Key
            $name = "cp10-toolbar-$($tool.Name)-$shapes"
            Shot $name
            $names += $name
        }
        Ctl -Keys "f"
    }
    Check ((Ctl -Read ShapesButton) -eq "Formen: aus") "F schaltet 'Formen: an' und wieder 'Formen: aus'"
    Ctl -Click ShapesButton
    Check ((Ctl -Read ShapesButton) -eq "Formen: an") "Klick auf den Schalter zeigt 'Formen: an'"
    Join-Toolbars $names "cp10-toolbars"
    Stop-App
}
