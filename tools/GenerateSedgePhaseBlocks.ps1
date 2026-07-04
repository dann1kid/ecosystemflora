# Shore sedge (brownsedge) phenology phase blocks — full-scale reed shape, no free drawnHeight clip.
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "WritePlantPhaseSnowBlock.ps1")

$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

$phases = @{
    dormant = @{
        Tint = "#6e6254"
        SelectionY2 = 0.45
    }
    dieback = @{
        Tint = "#8b7355"
        SelectionY2 = 0.38
    }
}

foreach ($phase in $phases.Keys) {
    $cfg = $phases[$phase]
    $baseCode = "sedgephase-$phase"
    $shapeInner = "`"base`": `"game:block/plant/reedpapyrus/sedge`", `"scale`": 1.0"
    $texJson = @"
      "leaves": { "base": "game:block/plant/reeds/brownsedge", "tint": "$($cfg.Tint)" }
"@
    $snowTex = "game:block/plant/reeds/brownsedge"

    $opts = @{
        SelectionY2 = $cfg.SelectionY2
        SnowDrawnHeight = 10
        SnowTextureSouth = $snowTex
        UseShapeByType = $true
        SkipDefaultSnowPresentation = $true
        ExtraTopLines = "`n  `"faceCullMode`": `"NeverCull`",`n  `"randomDrawOffset`": true,"
        ClimateLines = @"
  "climateColorMap": "climatePlantTint",
"@
        FreeOnlyLines = @"

  "attributesByType": {
    "*-snow": {
      "drawnHeight": 10,
      "allowOverlays": false,
      "allowStepWhenStuck": true
    }
  },
  "vertexFlagsByType": {
    "*-free": { "windMode": "NormalWind", "windData": 2 },
    "*-snow": { "zOffset": 3, "windMode": "ExtraWeakWind", "windData": 2 }
  },
  "faceCullModeByType": {
    "*-free": "NeverCull",
    "*-snow": "MergeSnowLayer"
  },
"@
    }

    Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" $shapeInner $texJson $snowTex $opts
    Write-Host "Wrote $baseCode"
}

$seasonalLang = Join-Path $PSScriptRoot "GenerateSeasonalBlockLang.ps1"
if (Test-Path $seasonalLang) {
    & $seasonalLang
}

Write-Host "Done."
