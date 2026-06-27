$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

function GameAsset([string]$path) {
    if ($path.StartsWith("game:")) { return $path }
    return "game:$path"
}

$plantSounds = '"place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant"'

$species = @(
    @{
        Name = "eaglefern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/eaglefern/var*"
        Textures = $null
    },
    @{
        Name = "cinnamonfern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/cinnamonfern/var*"
        Textures = $null
    },
    @{
        Name = "deerfern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/deerfern/var*"
        Textures = $null
    },
    @{
        Name = "hartstongue"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/hartstongue/var*"
        Textures = $null
    },
    @{
        Name = "tallfern"
        Class = "BlockPlant"
        Shape = GameAsset "block/plant/fern/tallfern/var1"
        Textures = @(
            "    `"all`": { `"base`": `"$(GameAsset 'block/plant/fern/tallfern/fern*')`" }"
        )
    }
)

foreach ($entry in $species) {
    $name = $entry.Name
    $path = Join-Path $outDir "juvenile-fern-$name-free.json"
    $texturesBlock = if ($entry.Textures) {
        "`n  `"textures`": {`n$($entry.Textures -join ",`n")`n  },"
    } else {
        ""
    }
    @"
{
  "code": "juvenile-fern-$name-free",
  "class": "$($entry.Class)",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "shape": { "base": "$($entry.Shape)", "scale": 0.42 },$texturesBlock
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.25, "y1": 0, "z1": 0.25, "x2": 0.75, "y2": 0.28, "z2": 0.75 },
  "sounds": { $plantSounds },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
    Write-Host "Wrote $path"
}
