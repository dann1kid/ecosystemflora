# Fern phenology phase blocks (dormant/dieback) from juvenile-fern templates.
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$species = @("eaglefern", "cinnamonfern", "deerfern", "hartstongue", "tallfern")
$phases = @{
    dormant = "#5c6b52"
    dieback = "#8b7355"
}

foreach ($sp in $species) {
    $srcPath = Join-Path $outDir "juvenile-fern-$sp-free.json"
    if (-not (Test-Path $srcPath)) { throw "Missing template $srcPath" }
    $template = Get-Content $srcPath -Raw | ConvertFrom-Json

    foreach ($phase in $phases.Keys) {
        $obj = $template | ConvertTo-Json -Depth 20 | ConvertFrom-Json
        $obj.code = "fernphase-$sp-$phase"
        if ($obj.shape) { $obj.shape.scale = 1.0 }
        if ($obj.selectionbox) {
            $obj.selectionbox.y2 = [Math]::Min(1.0, [double]$obj.selectionbox.y2 * 2.4)
        }

        $tint = $phases[$phase]
        foreach ($prop in $obj.textures.PSObject.Properties) {
            if ($prop.Value.base) {
                $prop.Value | Add-Member -NotePropertyName "alpha" -NotePropertyValue 255 -Force
                $prop.Value | Add-Member -NotePropertyName "overlays" -NotePropertyValue @() -Force
                $prop.Value | Add-Member -NotePropertyName "tiles" -NotePropertyValue @() -Force
                $prop.Value | Add-Member -NotePropertyName "tilesWidth" -NotePropertyValue 0 -Force
                $prop.Value | Add-Member -NotePropertyName "tilesHeight" -NotePropertyValue 0 -Force
                $prop.Value | Add-Member -NotePropertyName "windMode" -NotePropertyValue 0 -Force
                $prop.Value | Add-Member -NotePropertyName "windData" -NotePropertyValue 0 -Force
                $prop.Value | Add-Member -NotePropertyName "rotation" -NotePropertyValue 0 -Force
                $prop.Value | Add-Member -NotePropertyName "autoTile" -NotePropertyValue $false -Force
                $prop.Value | Add-Member -NotePropertyName "alternates" -NotePropertyValue @() -Force
                $prop.Value | Add-Member -NotePropertyName "tint" -NotePropertyValue $tint -Force
            }
        }

        $dest = Join-Path $outDir "fernphase-$sp-$phase.json"
        ($obj | ConvertTo-Json -Depth 20) | Set-Content -Encoding UTF8 $dest
        Write-Host "Wrote $dest"
    }
}

Write-Host "Done."
