# Generates juvenile flower blocktypes mirroring vanilla flower.json shape/texture groups.
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

$existing = @(
    "cowparsley","horsetail","mugwort","lupine","woad","redtopgrass","heather","westerngorse"
)

function Write-Block($species, $shape, $textures) {
    $path = Join-Path $outDir "juvenile-flower-$species-free.json"
    $texJson = ($textures.GetEnumerator() | ForEach-Object {
        "    `"$($_.Key)`": { `"base`": `"$($_.Value)`" }"
    }) -join ",`n"
    @"
{
  "code": "juvenile-flower-$species-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "shape": { "base": "$shape", "scale": 0.45 },
  "textures": {
$texJson
  },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.2, "y1": 0, "z1": 0.2, "x2": 0.8, "y2": 0.22, "z2": 0.8 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
}

function ThreePatch24($species) {
    $t = @{}
    foreach ($n in 1,2,3) {
        $t["north$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["south$n"] = "game:block/plant/flower/petal/${species}$n"
        $t["northTinted$n"] = "game:block/plant/flower/stem/${species}$n"
        $t["southTinted$n"] = "game:block/plant/flower/stem/${species}$n"
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-24x24" $t
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
        north1 = "game:block/plant/flower/petal/$species"
        south1 = "game:block/plant/flower/petal/$species"
        northTinted1 = "game:block/transparent"
        southTinted1 = "game:block/transparent"
    }
    Write-Block $species "game:block/plant/flower/1patch-3faces-16x16" $t
}

function CrotonBlock() {
    $path = Join-Path $outDir "juvenile-flower-croton-free.json"
    @"
{
  "code": "juvenile-flower-croton-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "randomDrawOffset": true,
  "randomizeAxes": "xz",
  "shape": { "base": "game:block/plant/croton/small/crimson-green", "scale": 0.45 },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.2, "y1": 0, "z1": 0.2, "x2": 0.8, "y2": 0.22, "z2": 0.8 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
}

function RafflesiaBlock($species, $color) {
    $path = Join-Path $outDir "juvenile-flower-$species-free.json"
    @"
{
  "code": "juvenile-flower-$species-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "randomDrawOffset": true,
  "randomizeAxes": "xz",
  "shape": { "base": "game:block/plant/rafflesia/$color", "scale": 0.45 },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.2, "y1": 0, "z1": 0.2, "x2": 0.8, "y2": 0.22, "z2": 0.8 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
}

$toGenerate = @{
    catmint = { ThreePatch24 "catmint" }
    cornflower = { ThreePatch24 "cornflower" }
    wilddaisy = { ThreePatch24 "wilddaisy" }
    forgetmenot = { ThreePatch16 "forgetmenot" }
    edelweiss = { ThreePatch16 "edelweiss" }
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
}

foreach ($kv in $toGenerate.GetEnumerator()) {
    if ($existing -contains $kv.Key) { continue }
    & $kv.Value
    Write-Host "Wrote juvenile-flower-$($kv.Key)-free.json"
}

Write-Host "Done."
