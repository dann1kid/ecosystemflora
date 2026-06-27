# Fern phenology phase blocks (dormant/dieback) — vanilla fern shapes + tinted texture overrides.
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

function GameAsset([string]$path) {
    if ($path.StartsWith("game:")) { return $path }
    return "game:$path"
}

$plantSounds = '"place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant"'

$phases = @{
    dormant = "#5c6b52"
    dieback = "#8b7355"
}

$species = @(
    @{
        Name = "eaglefern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/eaglefern/var*"
        TintKeys = @(
            @{ Key = "tall2"; Base = GameAsset "block/plant/fern/eaglefern/tall" },
            @{ Key = "center1"; Base = GameAsset "block/plant/fern/eaglefern/center1" }
        )
    },
    @{
        Name = "cinnamonfern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/cinnamonfern/var*"
        TintKeys = @(
            @{ Key = "tall"; Base = GameAsset "block/plant/fern/cinnamonfern/tall" },
            @{ Key = "short"; Base = GameAsset "block/plant/fern/cinnamonfern/short" },
            @{ Key = "center1"; Base = GameAsset "block/plant/fern/cinnamonfern/center1" },
            @{ Key = "center2"; Base = GameAsset "block/plant/fern/cinnamonfern/center2" }
        )
    },
    @{
        Name = "deerfern"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/deerfern/var*"
        TintKeys = @(
            @{ Key = "tall1"; Base = GameAsset "block/plant/fern/deerfern/tall1" },
            @{ Key = "tall2"; Base = GameAsset "block/plant/fern/deerfern/tall2" },
            @{ Key = "tall3"; Base = GameAsset "block/plant/fern/deerfern/tall3" },
            @{ Key = "center1"; Base = GameAsset "block/plant/fern/deerfern/center1" }
        )
    },
    @{
        Name = "hartstongue"
        Class = "BlockFern"
        Shape = GameAsset "block/plant/fern/hartstongue/var*"
        TintKeys = @(
            @{ Key = "straight"; Base = GameAsset "block/plant/fern/hartstongue/straight" },
            @{ Key = "curved"; Base = GameAsset "block/plant/fern/hartstongue/curved" }
        )
    },
    @{
        Name = "tallfern"
        Class = "BlockPlant"
        Shape = GameAsset "block/plant/fern/tallfern/var1"
        TintKeys = @(
            @{ Key = "all"; Base = GameAsset "block/plant/fern/tallfern/fern*" }
        )
    }
)

function Format-TintedTextures($tintKeys, $tint) {
    ($tintKeys | ForEach-Object {
        "    `"$($_.Key)`": { `"base`": `"$($_.Base)`", `"tint`": `"$tint`" }"
    }) -join ",`n"
}

foreach ($entry in $species) {
    foreach ($phase in $phases.Keys) {
        $tint = $phases[$phase]
        $texJson = Format-TintedTextures $entry.TintKeys $tint
        $dest = Join-Path $outDir "fernphase-$($entry.Name)-$phase.json"
        @"
{
  "code": "fernphase-$($entry.Name)-$phase",
  "class": "$($entry.Class)",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "shape": { "base": "$($entry.Shape)", "scale": 1.0 },
  "textures": {
$texJson
  },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.125, "y1": 0, "z1": 0.125, "x2": 0.875, "y2": 0.6, "z2": 0.875 },
  "sounds": { $plantSounds },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Encoding UTF8 $dest
        Write-Host "Wrote $dest"
    }
}

$seasonalLang = Join-Path $PSScriptRoot "GenerateSeasonalBlockLang.ps1"
if (Test-Path $seasonalLang) {
    & $seasonalLang
}

Write-Host "Done."
