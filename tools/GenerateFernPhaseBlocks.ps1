# Fern phenology phase blocks (dormant / dieback / sporulating) — cover variant groups (free / snow).
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "WritePlantPhaseSnowBlock.ps1")
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

function GameAsset([string]$path) {
    if ($path.StartsWith("game:")) { return $path }
    return "game:$path"
}

$phases = @{
    dormant = @{ Tint = "#5c6b52" }
    dieback = @{ Tint = "#8b7355" }
    sporulating = @{ Tint = $null }
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

function Format-PhaseTextures($tintKeys, $tint) {
    ($tintKeys | ForEach-Object {
        if ($tint) {
            "      `"$($_.Key)`": { `"base`": `"$($_.Base)`", `"tint`": `"$tint`" }"
        } else {
            "      `"$($_.Key)`": { `"base`": `"$($_.Base)`" }"
        }
    }) -join ",`n"
}

function Get-SnowTextureFromKeys($tintKeys) {
    $paths = $tintKeys | ForEach-Object { $_.Base }
    return Get-SnowCrossTextureFromPaths $paths
}

foreach ($entry in $species) {
    foreach ($phaseName in $phases.Keys) {
        $phaseCfg = $phases[$phaseName]
        $baseCode = "fernphase-$($entry.Name)-$phaseName"
        $shapeInner = "`"base`": `"$($entry.Shape)`", `"scale`": 1.0"
        $texJson = Format-PhaseTextures $entry.TintKeys $phaseCfg.Tint
        $snowTex = Get-SnowTextureFromKeys $entry.TintKeys

        $legacySnow = Join-Path $outDir "$baseCode-snow.json"
        if (Test-Path $legacySnow) { Remove-Item $legacySnow -Force }

        $opts = @{
            SelectionY2 = 0.6
            UseShapeByType = $true
            SnowDrawnHeight = 14
            FreeOnlyLines = @"

  "attributesByType": {
    "*-free": {
      "drawnHeight": 14
    },
    "*-snow": {
      "drawnHeight": 14,
      "allowOverlays": false,
      "allowStepWhenStuck": true
    }
  },
"@
        }

        Write-PlantPhaseSnowBlock $outDir $baseCode $entry.Class $shapeInner $texJson $snowTex $opts
        Write-Host "Wrote $baseCode"
    }
}

$seasonalLang = Join-Path $PSScriptRoot "GenerateSeasonalBlockLang.ps1"
if (Test-Path $seasonalLang) {
    & $seasonalLang
}

Write-Host "Done."
