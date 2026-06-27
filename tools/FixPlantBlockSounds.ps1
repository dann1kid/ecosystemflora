$ErrorActionPreference = "Stop"
$plantDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$old = '"place": "block/plant", "break": "block/plant", "hit": "block/plant"'
$new = '"place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant"'
$count = 0

Get-ChildItem -Path $plantDir -Filter *.json | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    if ($content.Contains($old)) {
        $updated = $content.Replace($old, $new)
        [System.IO.File]::WriteAllText($_.FullName, $updated, (New-Object System.Text.UTF8Encoding $false))
        $count++
        Write-Host "Fixed $($_.Name)"
    }
}

Write-Host "Updated $count block json files."
