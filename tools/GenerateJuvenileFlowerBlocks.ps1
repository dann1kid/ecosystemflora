# Vanilla uses a single petal/stem file (wildcard {flower}* group), not numbered variants.
$wildcardSingle24 = @("catmint")

$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

$existing = @(
    "cowparsley","horsetail","mugwort","lupine","woad","heather","westerngorse"
)

function Get-SnowCrossTexture($textures) {
    foreach ($key in @("north1", "petal1", "flower2", "north", "plant1a")) {
        if ($textures.ContainsKey($key)) {
            $path = $textures[$key]
            if (-not [string]::IsNullOrEmpty($path) -and $path -notmatch "transparent") {
                return $path
            }
        }
    }
    foreach ($val in $textures.Values) {
        if (-not [string]::IsNullOrEmpty($val) -and $val -match "/petal/" -and $val -notmatch "transparent") {
            return $val
        }
    }
    return "game:block/plant/flower/petal/wilddaisy1"
}

function Write-Block($species, $shape, $textures, [hashtable]$extra = $null) {
    $path = Join-Path $outDir "juvenile-flower-$species.json"
    $legacyPath = Join-Path $outDir "juvenile-flower-$species-free.json"
    if (Test-Path $legacyPath) { Remove-Item $legacyPath -Force }

    $selY2 = 0.22
    # Match vanilla flower drawnHeightByType — clipping to 11 left only stem stubs / half petals.
    $drawnHeight = Get-DrawnHeightForShape $shape
    $texJson = ($textures.GetEnumerator() | ForEach-Object {
        "    `"$($_.Key)`": { `"base`": `"$($_.Value)`" }"
    }) -join ",`n"

    $snowTex = Get-SnowCrossTexture $textures
    $extraAttrs = ""
    if ($null -ne $extra) {
        if ($extra.ContainsKey("randomDrawOffset")) {
            $extraAttrs += "`n  `"randomDrawOffset`": true,"
        }
        if ($extra.ContainsKey("randomizeAxes")) {
            $extraAttrs += "`n  `"randomizeAxes`": `"xz`","
        }
        if ($extra.ContainsKey("selectionY2")) {
            $selY2 = $extra["selectionY2"]
        }
        if ($extra.ContainsKey("drawnHeight")) {
            $drawnHeight = $extra["drawnHeight"]
        }
    }
    if (-not $selY2) { $selY2 = 0.22 }

    @"
{
  "code": "juvenile-flower-$species",
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
  "randomizeRotations": true,$extraAttrs
  "shapeByType": {
    "*-free": { "base": "$shape", "scale": 0.45 }
  },
  "shape": { "base": "game:block/basic/cross" },
  "texturesByType": {
    "*-free": {
$texJson
    },
    "*-snow": {
      "north": { "base": "$snowTex" },
      "south": { "base": "$snowTex" }
    }
  },
  "attributesByType": {
    "*-free": {
      "drawnHeight": $drawnHeight
    },
    "*-snow": {
      "drawnHeight": $drawnHeight,
      "allowOverlays": false,
      "allowStepWhenStuck": true
    }
  },
  "vertexFlagsByType": {
    "*-snow": {
      "zOffset": 3,
      "windMode": "ExtraWeakWind",
      "windData": 2
    }
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
  "selectionbox": { "x1": 0.2, "y1": 0, "z1": 0.2, "x2": 0.8, "y2": $selY2, "z2": 0.8 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant", "hit": "game:block/plant" },
  "frostable": true,
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
}

function Get-DrawnHeightForShape($shape) {
    # Vanilla flower.json drawnHeightByType (VS 1.22 HiDPI petal/stem PNGs).
    if ($shape -match "16x16") { return 32 }
    if ($shape -match "lilyofthevalley") { return 48 }
    if ($shape -match "lupine") { return 48 }
    if ($shape -match "croton") { return 48 }
    if ($shape -match "rafflesia") { return 48 }
    return 48
}

function ThreePatch24($species) {
    $t = @{}
    foreach ($n in 1,2,3) {
        $t["north$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["south$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["northTinted$n"] = "game:block/plant/flower/stem/${species}$n"
        $t["southTinted$n"] = "game:block/plant/flower/stem/${species}$n"
    }
    $drawn = switch ($species) {
        "goldenpoppy" { 32 }
        "horsetail" { 40 }
        default { 48 }
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-24x24" $t @{ drawnHeight = $drawn }
}

function MugwortBlock() {
    $t = @{}
    foreach ($n in 1,2,3) {
        $t["north$n"] = "game:block/plant/flower/petal/mugwort$n"
        $t["south$n"] = "game:block/plant/flower/petal/mugwort$n"
        $t["northTinted$n"] = "game:block/transparent"
        $t["southTinted$n"] = "game:block/transparent"
    }
    Write-Block "mugwort" "game:block/plant/flower/3patches-3faces-24x24" $t
}

function HeatherBlock() {
    $t = @{
        north1 = "game:block/plant/flower/petal/heather1"
        south1 = "game:block/plant/flower/petal/heather1"
        northTinted1 = "game:block/plant/flower/stem/heather1"
        southTinted1 = "game:block/plant/flower/stem/heather1"
        north2 = "game:block/plant/flower/petal/heather2"
        south2 = "game:block/plant/flower/petal/heather2"
        northTinted2 = "game:block/plant/flower/stem/heather2"
        southTinted2 = "game:block/plant/flower/stem/heather2"
        north3 = "game:block/plant/flower/petal/heather2"
        south3 = "game:block/plant/flower/petal/heather2"
        northTinted3 = "game:block/plant/flower/stem/heather2"
        southTinted3 = "game:block/plant/flower/stem/heather2"
        flower2 = "game:block/plant/flower/petal/heather2"
        flower2Tinted = "game:block/plant/flower/stem/heather2"
    }
    Write-Block "heather" "game:block/plant/flower/1patch-3faces-24x24" $t @{
        randomDrawOffset = $true
        drawnHeight = 28
    }
}

function WesternGorseBlock() {
    $t = @{
        north1 = "game:block/plant/flower/petal/westerngorse1"
        south1 = "game:block/plant/flower/petal/westerngorse1"
        northTinted1 = "game:block/plant/flower/stem/westerngorse1"
        southTinted1 = "game:block/plant/flower/stem/westerngorse1"
        north2 = "game:block/plant/flower/petal/westerngorse2"
        south2 = "game:block/plant/flower/petal/westerngorse2"
        northTinted2 = "game:block/plant/flower/stem/westerngorse2"
        southTinted2 = "game:block/plant/flower/stem/westerngorse2"
        north3 = "game:block/plant/flower/petal/westerngorse2"
        south3 = "game:block/plant/flower/petal/westerngorse2"
        northTinted3 = "game:block/plant/flower/stem/westerngorse2"
        southTinted3 = "game:block/plant/flower/stem/westerngorse2"
        flower2 = "game:block/plant/flower/petal/westerngorse2"
        flower2Tinted = "game:block/plant/flower/stem/westerngorse2"
    }
    Write-Block "westerngorse" "game:block/plant/flower/1patch-3faces-24x24" $t
}

function CrossNumbered24($species) {
    # Vanilla flower-redtopgrass uses petal/{flower}* — redtopgrass1 alone is pale/white.
    $t = @{
        north1 = "game:block/plant/flower/petal/${species}*"
        south1 = "game:block/plant/flower/petal/${species}*"
        northTinted1 = "game:block/plant/flower/stem/${species}*"
        southTinted1 = "game:block/plant/flower/stem/${species}*"
    }
    $drawn = if ($species -eq "redtopgrass") { 38 } else { 48 }
    Write-Block $species "game:block/plant/flower/1patch-cross-24x24" $t @{ drawnHeight = $drawn }
}

# Vanilla default texturesByType uses petal/{flower}* — single file (catmint) or wildcard pick.
function WildcardSingle24($species) {
    $t = @{}
    foreach ($n in 1,2,3) {
        $t["north$n"] = "game:block/plant/flower/petal/$species"
        $t["south$n"] = "game:block/plant/flower/petal/$species"
        $t["northTinted$n"] = "game:block/plant/flower/stem/$species"
        $t["southTinted$n"] = "game:block/plant/flower/stem/$species"
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-24x24" $t
}

function TwoVariant24($species) {
    $t = @{}
    foreach ($n in 1,2) {
        $t["north$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["south$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["northTinted$n"] = "game:block/plant/flower/stem/${species}$n"
        $t["southTinted$n"] = "game:block/plant/flower/stem/${species}$n"
    }
    $t["north3"] = "game:block/plant/flower/petal/${species}2"
    $t["south3"] = "game:block/plant/flower/petal/${species}2"
    $t["northTinted3"] = "game:block/plant/flower/stem/${species}2"
    $t["southTinted3"] = "game:block/plant/flower/stem/${species}2"
    Write-Block $species "game:block/plant/flower/1patch-3faces-24x24" $t
}

function TwoVariant16($species) {
    $t = @{}
    foreach ($n in 1,2) {
        $t["north$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["south$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["northTinted$n"] = "game:block/plant/flower/stem/${species}$n"
        $t["southTinted$n"] = "game:block/plant/flower/stem/${species}$n"
    }
    $t["north3"] = "game:block/plant/flower/petal/${species}2"
    $t["south3"] = "game:block/plant/flower/petal/${species}2"
    $t["northTinted3"] = "game:block/plant/flower/stem/${species}2"
    $t["southTinted3"] = "game:block/plant/flower/stem/${species}2"
    $drawn = if ($species -eq "forgetmenot") { 16 } elseif ($species -eq "edelweiss") { 36 } else { 32 }
    Write-Block $species "game:block/plant/flower/1patch-3faces-16x16" $t @{ drawnHeight = $drawn }
}

function ThreePatch16($species) {
    $t = @{}
    foreach ($n in 1,2,3) {
        $t["north$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["south$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["northTinted$n"] = "game:block/plant/flower/stem/${species}$n"
        $t["southTinted$n"] = "game:block/plant/flower/stem/${species}$n"
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-16x16" $t
}

function LilyShape($species) {
    $t = @{
        stem1 = "game:block/plant/flower/stem/${species}1"
        stem2 = "game:block/plant/flower/stem/${species}2"
        stem3 = "game:block/plant/flower/stem/${species}3"
        petal1 = "game:block/plant/flower/petal/${species}1"
        petal2 = "game:block/plant/flower/petal/${species}2"
        petal3 = "game:block/plant/flower/petal/${species}3"
        leaves1 = "game:block/plant/flower/stem/${species}leaves1"
        north1 = "game:block/transparent"
        south1 = "game:block/transparent"
        northTinted1 = "game:block/transparent"
        southTinted1 = "game:block/transparent"
    }
    Write-Block $species "game:block/plant/flower/lilyofthevalley" $t
}

function LupineBlock() {
    # Vanilla lupine uses game:block/plant/lupine/one-plant — not 1patch-3faces / lupine1..3 paths.
    $t = @{}
    foreach ($n in 1..5) {
        $t["plant${n}a"] = "game:block/plant/flower/petal/lupine/blue${n}-a"
        $t["plant${n}b"] = "game:block/plant/flower/petal/lupine/blue${n}-b"
        $t["plant${n}astem"] = "game:block/plant/flower/stem/lupine/normal${n}-a"
        $t["plant${n}bstem"] = "game:block/plant/flower/stem/lupine/normal${n}-b"
    }
    Write-Block "lupine" "game:block/plant/lupine/one-plant" $t
}

function OrangemallowBlock() {
    $species = "orangemallow"
    $t = @{
        north1 = "game:block/plant/flower/petal/${species}1"
        south1 = "game:block/plant/flower/petal/${species}3"
        northTinted1 = "game:block/plant/flower/stem/${species}1"
        southTinted1 = "game:block/plant/flower/stem/${species}3"
        flower2 = "game:block/plant/flower/petal/${species}2"
        flower2Tinted = "game:block/plant/flower/stem/${species}2"
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-24x24" $t
}

function GhostpipeBlock($species) {
    $t = @{
        north1 = "game:block/plant/flower/petal/$species*"
        south1 = "game:block/plant/flower/petal/$species*"
        northTinted1 = "game:block/transparent"
        southTinted1 = "game:block/transparent"
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-16x16" $t
}

function CrotonBlock() {
    $species = "croton"
    Write-Block $species "game:block/plant/croton/small/crimson-green" @{} @{
        randomDrawOffset = $true
        randomizeAxes = $true
    }
}

function RafflesiaBlock($species, $color) {
    Write-Block $species "game:block/plant/rafflesia/$color" @{} @{
        randomDrawOffset = $true
        randomizeAxes = $true
    }
}

$toGenerate = @{
    catmint = { WildcardSingle24 "catmint" }
    cornflower = { ThreePatch24 "cornflower" }
    wilddaisy = { TwoVariant24 "wilddaisy" }
    forgetmenot = { TwoVariant16 "forgetmenot" }
    edelweiss = { TwoVariant16 "edelweiss" }
    goldenpoppy = { ThreePatch24 "goldenpoppy" }
    bluebell = { LilyShape "bluebell" }
    daffodil = { LilyShape "daffodil" }
    lilyofthevalley = { LilyShape "lilyofthevalley" }
    orangemallow = { OrangemallowBlock }
    ghostpipewhite = { GhostpipeBlock "ghostpipewhite" }
    ghostpipepink = { GhostpipeBlock "ghostpipepink" }
    ghostpipered = { GhostpipeBlock "ghostpipered" }
    croton = { CrotonBlock }
    rafflesiabrown = { RafflesiaBlock "rafflesiabrown" "brown" }
    rafflesiared = { RafflesiaBlock "rafflesiared" "red" }
    redtopgrass = { CrossNumbered24 "redtopgrass" }
    cowparsley = { ThreePatch24 "cowparsley" }
    horsetail = { ThreePatch24 "horsetail" }
    mugwort = { MugwortBlock }
    lupine = { LupineBlock }
    woad = { ThreePatch24 "woad" }
    heather = { HeatherBlock }
    westerngorse = { WesternGorseBlock }
}

foreach ($kv in $toGenerate.GetEnumerator()) {
    & $kv.Value
    Write-Host "Wrote juvenile-flower-$($kv.Key).json"
}

Get-ChildItem -Path $outDir -Filter "juvenile-flower-*-free.json" | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "Removed legacy $($_.Name)"
}

Write-Host "Done."
