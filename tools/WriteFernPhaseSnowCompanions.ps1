# Emit standalone *-snow.json for each legacy fern phase block (no cover variant group on base code).
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

foreach ($file in Get-ChildItem -Path $outDir -Filter "fernphase-*.json") {
    if ($file.Name -match "-snow\.json$") { continue }
    if ($file.Name -notmatch "^fernphase-(.+)-(dormant|dieback)\.json$") { continue }

    $block = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json
    if ($block.variantgroups) { continue }

    $snowCode = $block.code + "-snow"
    $snowTex = "game:block/plant/fern/eaglefern/tall"
    if ($block.textures) {
        foreach ($prop in $block.textures.PSObject.Properties) {
            $p = if ($prop.Value.base) { $prop.Value.base } else { $prop.Value }
            if ($p -and $p -notmatch "transparent") { $snowTex = $p; break }
        }
    }

    $y2 = if ($block.selectionbox.y2) { $block.selectionbox.y2 } else { 0.6 }
    $dest = Join-Path $outDir "$snowCode.json"
    $json = @"
{
  "code": "$snowCode",
  "class": "$($block.class)",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Snow",
  "drawtype": "crossandsnowlayer",
  "randomizeRotations": true,
  "shape": { "base": "game:block/basic/cross" },
  "textures": {
    "north": { "base": "$snowTex" },
    "south": { "base": "$snowTex" }
  },
  "attributes": {
    "drawnHeight": 14,
    "allowOverlays": false,
    "allowStepWhenStuck": true
  },
  "vertexFlags": {
    "zOffset": 3,
    "windMode": "ExtraWeakWind",
    "windData": 2
  },
  "faceCullMode": "MergeSnowLayer",
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.15, "y1": 0, "z1": 0.15, "x2": 0.85, "y2": $y2, "z2": 0.85 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant" },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@
    [System.IO.File]::WriteAllText($dest, $json, $utf8NoBom)
    Write-Host "Wrote $snowCode"
}
