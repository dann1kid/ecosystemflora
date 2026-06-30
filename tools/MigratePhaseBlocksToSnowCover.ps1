# One-shot migration: merge free-only (or free+snow pair) phase blocks into cover variant groups.
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "WritePlantPhaseSnowBlock.ps1")

$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

function Get-TexturePath($tex) {
    if ($null -eq $tex) { return $null }
    if ($tex -is [string]) { return $tex }
    return $tex.base
}

function Textures-ToJsonLines($textures) {
    $lines = @()
    foreach ($prop in $textures.PSObject.Properties | Sort-Object Name) {
        $val = $prop.Value
        $path = Get-TexturePath $val
        $tint = $val.tint
        if ($tint) {
            $lines += "      `"$($prop.Name)`": { `"base`": `"$path`", `"tint`": `"$tint`" }"
        } else {
            $lines += "      `"$($prop.Name)`": { `"base`": `"$path`" }"
        }
    }
    return $lines -join ",`n"
}

function Pick-SnowTexture($block) {
    $paths = @()
    if ($block.textures) {
        foreach ($prop in $block.textures.PSObject.Properties) {
            $paths += Get-TexturePath $prop.Value
        }
    }
    if ($block.texturesByType.'*-free') {
        foreach ($prop in $block.texturesByType.'*-free'.PSObject.Properties) {
            $paths += Get-TexturePath $prop.Value
        }
    }
    return Get-SnowCrossTextureFromPaths $paths
}

# --- Flower phases (flowerphase-species-phase-free.json) ---
$flowerCount = 0
foreach ($file in Get-ChildItem -Path $outDir -Filter "flowerphase-*-free.json") {
    if ($file.Name -notmatch "^flowerphase-(.+)-(vegetative|dormant|dieback)-free\.json$") { continue }
    $baseCode = "flowerphase-$($Matches[1])-$($Matches[2])"
    $block = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json

    $shapeInner = if ($block.shape.base) { "`"base`": `"$($block.shape.base)`"" } else { "`"base`": `"game:block/basic/cross`"" }
    if ($block.shape.scale) { $shapeInner += ", `"scale`": $($block.shape.scale)" }

    $texJson = if ($block.textures -and $block.textures.PSObject.Properties.Count -gt 0) {
        Textures-ToJsonLines $block.textures
    } else {
        ""
    }
    $snowTex = Pick-SnowTexture $block

    $extraTop = ""
    if ($block.randomDrawOffset) { $extraTop += "`n  `"randomDrawOffset`": true," }
    if ($block.randomizeAxes) { $extraTop += "`n  `"randomizeAxes`": `"$($block.randomizeAxes)`"," }

    $climate = ""
    if ($block.climateColorMap) {
        $climate = "`n  `"climateColorMap`": `"$($block.climateColorMap)`","
    }

    $y2 = if ($block.selectionbox.y2) { $block.selectionbox.y2 } else { 0.55 }

    $opts = @{
        SelectionY2 = $y2
        ExtraTopLines = $extraTop
        ClimateLines = $climate
        UseShapeByType = $true
    }
    if ($block.attributes -and $block.attributes.PSObject.Properties.Count -gt 0) {
        $attrParts = @()
        foreach ($ap in $block.attributes.PSObject.Properties) {
            if ($ap.Value -is [string]) {
                $attrParts += "`"$($ap.Name)`": `"$($ap.Value)`""
            } else {
                $attrParts += "`"$($ap.Name)`": $($ap.Value)"
            }
        }
        $attrInner = $attrParts -join ", "
        $opts.FreeOnlyLines = "`n  `"attributesByType`": { `"*-free`": { $attrInner }, `"*-snow`": { `"allowOverlays`": false, `"allowStepWhenStuck`": true } },"
    }
    if ($block.attributes.drawnHeight) {
        $opts.SnowDrawnHeight = $block.attributes.drawnHeight
    }

    Write-PlantPhaseSnowBlock $outDir $baseCode $block.class $shapeInner $texJson $snowTex $opts
    $flowerCount++
}
Write-Host "Migrated $flowerCount flower phase blocks."

# --- Fern phases: legacy codes without -free suffix; snow via WriteFernPhaseSnowCompanions.ps1 ---
$fernCount = 0
foreach ($file in Get-ChildItem -Path $outDir -Filter "fernphase-*.json") {
    if ($file.Name -match "-snow\.json$") { continue }
    if ($file.Name -notmatch "^fernphase-(.+)-(dormant|dieback)\.json$") { continue }
    $block = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json
    if ($block.variantgroups) { continue }
    $fernCount++
}
Write-Host "Skipped $fernCount fern phase blocks (legacy codes; run WriteFernPhaseSnowCompanions.ps1 for snow)."

# --- Tallgrass: merge free+snow pairs ---
$tallCount = 0
foreach ($phase in @("dormant", "dieback")) {
    $freePath = Join-Path $outDir "tallgrassphase-$phase-free.json"
    $snowPath = Join-Path $outDir "tallgrassphase-$phase-snow.json"
    if (-not (Test-Path $freePath)) { continue }

    $free = Get-Content -Raw -Path $freePath | ConvertFrom-Json
    $snow = if (Test-Path $snowPath) { Get-Content -Raw -Path $snowPath | ConvertFrom-Json } else { $free }

    $baseCode = "tallgrassphase-$phase"
    $texJson = Textures-ToJsonLines $free.textures
    $snowNorth = Get-TexturePath $snow.textures.north
    $snowTex = Get-SnowCrossTextureFromPaths @($snowNorth, (Get-TexturePath $free.textures.north))

    $climate = @"
  "climateColorMap": "climatePlantTint",
  "seasonColorMap": "seasonalGrass",
"@

    $extraTop = @"
  "randomDrawOffset": true,
  "attributes": {
    "drawnHeight": 8,
    "overrideRandomDrawOffset": 2,
    "allowOverlays": false,
    "allowStepWhenStuck": true
  },
"@

    $y2 = $free.selectionbox.y2
    $dest = Join-Path $outDir "$baseCode.json"
    Remove-LegacyPhaseFile $outDir $baseCode

    @"
{
  "code": "$baseCode",
  "variantgroups": [
    { "code": "cover", "states": ["free", "snow"] }
  ],
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "blockmaterialByType": {
    "*-snow": "Snow"
  },
  "drawtype": "JSON",
  "drawtypeByType": {
    "*-snow": "crossandsnowlayer"
  },
  "randomizeRotations": true,
$extraTop
  "shape": { "base": "game:block/basic/cross" },
  "texturesByType": {
    "*-free": {
$texJson
    },
    "*-snow": {
      "north": { "base": "$snowTex", "tint": "$($free.textures.north.tint)" },
      "south": { "base": "$(Get-TexturePath $snow.textures.south)", "tint": "$($free.textures.south.tint)" }
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
$climate
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 6000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.1, "y1": 0, "z1": 0.1, "x2": 0.9, "y2": $y2, "z2": 0.9 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant" },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | ForEach-Object { [System.IO.File]::WriteAllText($dest, $_, $utf8NoBom) }

    Remove-Item $freePath -Force
    if (Test-Path $snowPath) { Remove-Item $snowPath -Force }
    $tallCount++
}
Write-Host "Migrated $tallCount tallgrass phase blocks."
Write-Host "Done."
