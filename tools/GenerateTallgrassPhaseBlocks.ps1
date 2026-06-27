# Tallgrass phenology phase blocks — vanilla cross + tallgrass textures.
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$plantSounds = '"place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant"'

$phases = @{
    dormant = @{
        Tint = "#5c6b52"
        SelectionY2 = 0.35
        Texture = "veryshort"
    }
    dieback = @{
        Tint = "#8b7355"
        SelectionY2 = 0.22
        Texture = "veryshort"
    }
}

foreach ($phase in $phases.Keys) {
    $cfg = $phases[$phase]
    $dest = Join-Path $outDir "tallgrassphase-$phase-free.json"
    @"
{
  "code": "tallgrassphase-$phase-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "cross",
  "randomDrawOffset": true,
  "randomizeRotations": true,
  "shape": { "base": "game:block/basic/cross" },
  "textures": {
    "north": { "base": "game:block/plant/tallgrass/free/$($cfg.Texture)-north", "tint": "$($cfg.Tint)" },
    "south": { "base": "game:block/plant/tallgrass/free/$($cfg.Texture)-south", "tint": "$($cfg.Tint)" }
  },
  "climateColorMap": "climatePlantTint",
  "seasonColorMap": "seasonalGrass",
  "vertexFlags": {
    "windMode": "NormalWind",
    "windData": 2
  },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 6000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.1, "y1": 0, "z1": 0.1, "x2": 0.9, "y2": $($cfg.SelectionY2), "z2": 0.9 },
  "sounds": { $plantSounds },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Encoding UTF8 $dest
    Write-Host "Wrote $dest"
}

$seasonalLang = Join-Path $PSScriptRoot "GenerateSeasonalBlockLang.ps1"
if (Test-Path $seasonalLang) {
    & $seasonalLang
}

Write-Host "Done."
