# Tallgrass phenology phase blocks — vanilla cross shape + tallgrass textures.
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "WritePlantPhaseSnowBlock.ps1")

$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

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

foreach ($phase in $phases.Keys) {
    $cfg = $phases[$phase]
    $baseCode = "tallgrassphase-$phase"
    $texJson = @"
      "north": { "base": "game:block/plant/tallgrass/free/veryshort-north", "tint": "$($cfg.Tint)" },
      "south": { "base": "game:block/plant/tallgrass/free/veryshort-south", "tint": "$($cfg.Tint)" }
"@
    $snowTex = "game:block/plant/tallgrass/snow/veryshort-north"

    $opts = @{
        SelectionY2 = $cfg.SelectionY2
        Replaceable = 6000
        SnowDrawnHeight = 8
        UseShapeByType = $true
        ExtraTopLines = "`n  `"randomDrawOffset`": true,"
        ClimateLines = @"
  "climateColorMap": "climatePlantTint",
  "seasonColorMap": "seasonalGrass",
"@
        FreeOnlyLines = @"

  "attributesByType": {
    "*-free": {
      "drawnHeight": 8,
      "overrideRandomDrawOffset": 2
    },
    "*-snow": {
      "drawnHeight": 8,
      "overrideRandomDrawOffset": 2,
      "allowOverlays": false,
      "allowStepWhenStuck": true
    }
  },
  "vertexFlagsByType": {
    "*-free": {
      "windMode": "NormalWind",
      "windData": 2
    },
    "*-snow": {
      "zOffset": 3,
      "windMode": "ExtraWeakWind",
      "windData": 2
    }
  },
  "faceCullModeByType": {
    "*-free": "Default",
    "*-snow": "MergeSnowLayer"
  },
"@
    }

    Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" "`"base`": `"game:block/basic/cross`"" $texJson $snowTex $opts
    Write-Host "Wrote $baseCode"
}

$seasonalLang = Join-Path $PSScriptRoot "GenerateSeasonalBlockLang.ps1"
if (Test-Path $seasonalLang) {
    & $seasonalLang
}

Write-Host "Done."
