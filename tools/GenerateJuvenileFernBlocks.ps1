# Juvenile fern spread blocks — cover variant groups (free / snow).
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

function GameAsset([string]$path) {
    if ($path.StartsWith("game:")) { return $path }
    return "game:$path"
}

function Get-SnowTextureFromKeys($tintKeys) {
    $paths = $tintKeys | ForEach-Object { $_.Base }
    foreach ($p in $paths) {
        if ($p -and $p -notmatch "transparent" -and $p -notmatch '\*') { return $p }
    }
    foreach ($p in $paths) {
        if ($p -and $p -notmatch "transparent") { return $p }
    }
    return "game:block/plant/fern/eaglefern/tall"
}

$species = @(
    @{
        Name = "eaglefern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/eaglefern/var*"
        SnowFrom = @(
            @{ Base = GameAsset "block/plant/fern/eaglefern/tall" }
        )
    },
    @{
        Name = "cinnamonfern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/cinnamonfern/var*"
        SnowFrom = @(
            @{ Base = GameAsset "block/plant/fern/cinnamonfern/short" }
        )
    },
    @{
        Name = "deerfern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/deerfern/var*"
        SnowFrom = @(
            @{ Base = GameAsset "block/plant/fern/deerfern/tall1" }
        )
    },
    @{
        Name = "hartstongue"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/hartstongue/var*"
        SnowFrom = @(
            @{ Base = GameAsset "block/plant/fern/hartstongue/straight" }
        )
    },
    @{
        Name = "tallfern"
        Class = "BlockPlant"
        Shape = GameAsset "block/plant/fern/tallfern/var1"
        FreeTextures = @(
            "    `"all`": { `"base`": `"$(GameAsset 'block/plant/fern/tallfern/fern*')`" }"
        )
        SnowFrom = @(
            @{ Base = GameAsset "block/plant/fern/tallfern/fern*" }
        )
    }
)

foreach ($entry in $species) {
    $name = $entry.Name
    $path = Join-Path $outDir "juvenile-fern-$name.json"
    $legacyPath = Join-Path $outDir "juvenile-fern-$name-free.json"
    if (Test-Path $legacyPath) { Remove-Item $legacyPath -Force }

    $snowTex = Get-SnowTextureFromKeys $entry.SnowFrom
    $freeTexBlock = if ($entry.FreeTextures) {
        "`n  `"texturesByType`": {`n    `"*-free`": {`n$($entry.FreeTextures -join ",`n")`n    },`n    `"*-snow`": {`n      `"north`": { `"base`": `"$snowTex`" },`n      `"south`": { `"base`": `"$snowTex`" }`n    }`n  },"
    } else {
        "`n  `"texturesByType`": {`n    `"*-snow`": {`n      `"north`": { `"base`": `"$snowTex`" },`n      `"south`": { `"base`": `"$snowTex`" }`n    }`n  },"
    }

    @"
{
  "code": "juvenile-fern-$name",
  "variantgroups": [
    { "code": "cover", "states": ["free", "snow"] }
  ],
  "class": "$($entry.Class)",
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
  "shapeByType": {
    "*-free": { "base": "$($entry.Shape)", "scale": 0.42 },
    "*-snow": { "base": "game:block/basic/cross" }
  },$freeTexBlock
  "attributesByType": {
    "*-free": { "drawnHeight": 10 },
    "*-snow": { "drawnHeight": 10, "allowOverlays": false, "allowStepWhenStuck": true }
  },
  "vertexFlagsByType": {
    "*-snow": { "zOffset": 3, "windMode": "ExtraWeakWind", "windData": 2 }
  },
  "faceCullModeByType": {
    "*-snow": "MergeSnowLayer"
  },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.25, "y1": 0, "z1": 0.25, "x2": 0.75, "y2": 0.28, "z2": 0.75 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant" },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
    Write-Host "Wrote $path"
}

Write-Host "Done."
