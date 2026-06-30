# Shared helper: emit one block JSON with cover variant group (free / snow).
function Get-SnowCrossTextureFromPaths([string[]]$paths) {
    foreach ($p in $paths) {
        if (-not [string]::IsNullOrEmpty($p) -and $p -notmatch "transparent") { return $p }
    }
    return "game:block/plant/flower/petal/wilddaisy1"
}

function Remove-LegacyPhaseFile($outDir, $baseName) {
    foreach ($suffix in @("-free", "-snow")) {
        $legacy = Join-Path $outDir "$baseName$suffix.json"
        if (Test-Path $legacy) { Remove-Item $legacy -Force }
    }
}

function Write-PlantPhaseSnowBlock(
    [string]$OutDir,
    [string]$Code,
    [string]$Class,
    [string]$FreeShapeJson,
    [string]$FreeTexturesJson,
    [string]$SnowTexture,
    [hashtable]$Options = @{}
) {
    Remove-LegacyPhaseFile $OutDir $Code
    $dest = Join-Path $OutDir "$Code.json"

    $y2 = if ($Options.SelectionY2) { $Options.SelectionY2 } else { 0.55 }
    $replaceable = if ($Options.Replaceable) { $Options.Replaceable } else { 3000 }
    $extraTop = if ($Options.ExtraTopLines) { $Options.ExtraTopLines } else { "" }
    $climate = if ($Options.ClimateLines) { $Options.ClimateLines } else { "" }
    $snowDrawnHeight = if ($Options.SnowDrawnHeight) { $Options.SnowDrawnHeight } else { 11 }

    if ([string]::IsNullOrEmpty($SnowTexture)) {
        $SnowTexture = "game:block/plant/flower/petal/wilddaisy1"
    }

    $attrsBlock = if ($Options.FreeOnlyLines) {
        $Options.FreeOnlyLines
    } else {
        @"

  "attributesByType": {
    "*-free": {
      "drawnHeight": $snowDrawnHeight
    },
    "*-snow": {
      "drawnHeight": $snowDrawnHeight,
      "allowOverlays": false,
      "allowStepWhenStuck": true
    }
  },
"@
    }

    $shapeByType = if ($Options.UseShapeByType) {
        @"
  "shapeByType": {
    "*-free": { $FreeShapeJson },
    "*-snow": { "base": "game:block/basic/cross" }
  },
"@
    } else {
        @"
  "shape": { $FreeShapeJson },
"@
    }

    $freeTexSection = if ([string]::IsNullOrWhiteSpace($FreeTexturesJson)) {
        "    "
    } else {
        $FreeTexturesJson
    }

    @"
{
  "code": "$Code",
  "variantgroups": [
    { "code": "cover", "states": ["free", "snow"] }
  ],
  "class": "$Class",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "blockmaterialByType": {
    "*-snow": "Snow"
  },
  "drawtypeByType": {
    "*-free": "JSON",
    "*-snow": "crossandsnowlayer"
  },
  "randomizeRotations": true,$extraTop
$shapeByType
  "texturesByType": {
    "*-free": {
$freeTexSection
    },
    "*-snow": {
      "north": { "base": "$SnowTexture" },
      "south": { "base": "$SnowTexture" }
    }
  },
$attrsBlock
  "vertexFlagsByType": {
    "*-snow": {
      "zOffset": 3,
      "windMode": "ExtraWeakWind",
      "windData": 2
    }
  },
  "faceCullModeByType": {
    "*-snow": "MergeSnowLayer"
  },$climate
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": $replaceable,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.15, "y1": 0, "z1": 0.15, "x2": 0.85, "y2": $y2, "z2": 0.85 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant" },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $dest -Encoding UTF8
}
