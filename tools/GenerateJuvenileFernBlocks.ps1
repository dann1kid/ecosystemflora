$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"

$species = @(
    @{ Name = "eaglefern"; Texture = "eaglefern" },
    @{ Name = "cinnamonfern"; Texture = "cinnamonfern" },
    @{ Name = "deerfern"; Texture = "deerfern" },
    @{ Name = "hartstongue"; Texture = "hartstongue" },
    @{ Name = "tallfern"; Texture = "tallfern" }
)

foreach ($entry in $species) {
    $name = $entry.Name
    $tex = $entry.Texture
    $path = Join-Path $outDir "juvenile-fern-$name-free.json"
    @"
{
  "code": "juvenile-fern-$name-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "shape": { "base": "game:block/plant/fern/cross", "scale": 0.42 },
  "textures": {
    "east": { "base": "game:block/plant/fern/$tex" },
    "west": { "base": "game:block/plant/fern/$tex" }
  },
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.25, "y1": 0, "z1": 0.25, "x2": 0.75, "y2": 0.28, "z2": 0.75 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
    Write-Host "Wrote $path"
}
