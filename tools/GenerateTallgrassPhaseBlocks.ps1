# Tallgrass phenology phase blocks — vanilla cross shape + tallgrass textures.
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$plantSounds = '"place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant"'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

function Write-BlockJson($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
}

$phases = @{
    dormant = @{
        Tint = "#5c6b52"
        SelectionY2 = 0.35
    }
    dieback = @{
        Tint = "#8b7355"
        SelectionY2 = 0.22
    }
}

$covers = @{
    free = @{
        Suffix = "-free"
        Drawtype = "JSON"
        TextureCover = "free"
        BlockMaterial = "Plant"
        FaceCullMode = "Default"
        ClimateMaps = @"
  "climateColorMap": "climatePlantTint",
  "seasonColorMap": "seasonalGrass",
"@
        VertexFlags = @"
  "vertexFlags": {
    "windMode": "NormalWind",
    "windData": 2
  },
"@
    }
    snow = @{
        Suffix = "-snow"
        Drawtype = "crossandsnowlayer"
        TextureCover = "snow"
        BlockMaterial = "Snow"
        FaceCullMode = "MergeSnowLayer"
        ClimateMaps = ""
        VertexFlags = @"
  "vertexFlags": {
    "zOffset": 3,
    "windMode": "ExtraWeakWind",
    "windData": 2
  },
"@
    }
}

foreach ($phase in $phases.Keys) {
    $cfg = $phases[$phase]
    foreach ($coverKey in $covers.Keys) {
        $cover = $covers[$coverKey]
        $dest = Join-Path $outDir "tallgrassphase-$phase$($cover.Suffix).json"
        @"
{
  "code": "tallgrassphase-$phase$($cover.Suffix)",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "$($cover.BlockMaterial)",
  "drawtype": "$($cover.Drawtype)",
  "randomDrawOffset": true,
  "randomizeRotations": true,
  "shape": { "base": "game:block/basic/cross" },
  "attributes": {
    "drawnHeight": 8,
    "overrideRandomDrawOffset": 2,
    "allowOverlays": false,
    "allowStepWhenStuck": true
  },
  "textures": {
    "north": { "base": "game:block/plant/tallgrass/$($cover.TextureCover)/veryshort-north", "tint": "$($cfg.Tint)" },
    "south": { "base": "game:block/plant/tallgrass/$($cover.TextureCover)/veryshort-south", "tint": "$($cfg.Tint)" }
  },
$($cover.ClimateMaps)$($cover.VertexFlags)
  "faceCullMode": "$($cover.FaceCullMode)",
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
"@ | ForEach-Object { Write-BlockJson $dest $_ }
        Write-Host "Wrote $dest"
    }
}

$seasonalLang = Join-Path $PSScriptRoot "GenerateSeasonalBlockLang.ps1"
if (Test-Path $seasonalLang) {
    & $seasonalLang
}

Write-Host "Done."
