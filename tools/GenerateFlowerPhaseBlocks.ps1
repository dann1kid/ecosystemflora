# Full-size flower phenology blocks derived from juvenile-flower-* assets.
# Fixes invisible phases: wrong stem paths, transparent slots, mismatched shapes.
$ErrorActionPreference = "Stop"
$outDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\blocktypes\plant"
$plantDir = $outDir

function Get-TextureBasePath($texEntry) {
    if ($null -eq $texEntry) { return $null }
    if ($texEntry -is [string]) { return $texEntry }
    return $texEntry.base
}

function Test-IsPetalSlot($key, $path) {
    if ($path -match "/petal/") { return $true }
    if ($key -match "^(petal|flower2)$") { return $true }
    if ($key -match "^(north|south)\d+$" -and $key -notmatch "Tinted") { return $true }
    return $false
}

function Convert-ToPhaseTextures($textures, $phase, $species) {
    $fallbackStem = $null
    $fallbackPetal = $null
    $fallbackLeaves = $null
    foreach ($key in $textures.Keys) {
        $path = Get-TextureBasePath $textures[$key]
        if ([string]::IsNullOrEmpty($path) -or $path -match "transparent") { continue }
        if ($path -match "/stem/" -or $key -match "stem" -or $key -match "Tinted") {
            if (-not $fallbackStem) { $fallbackStem = $path }
        }
        if ($path -match "/petal/") {
            if (-not $fallbackPetal) { $fallbackPetal = $path }
        }
        if ($key -match "leaves") {
            $fallbackLeaves = $path
        }
    }

    # Prefer petal fallback — stem PNGs are often sparse/transparent on flower faces.
    $fallback = $fallbackPetal
    if (-not $fallback) { $fallback = $fallbackLeaves }
    if (-not $fallback) { $fallback = $fallbackStem }
    if (-not $fallback) { $fallback = "game:block/plant/flower/petal/wilddaisy1" }

    $vegetativePetalTint = "#6d7d62"
    $dormantTint = "#5c6b52"
    $diebackTint = "#8b7355"

    $result = @{}
    foreach ($key in $textures.Keys) {
        $path = Get-TextureBasePath $textures[$key]

        if ([string]::IsNullOrEmpty($path) -or $path -match "transparent") {
            $path = $fallback
        }

        if ([string]::IsNullOrEmpty($path) -or $path -match "transparent") {
            $path = "game:block/plant/flower/petal/wilddaisy1"
        }

        $isPetal = Test-IsPetalSlot $key $path
        $tint = $null
        switch ($phase) {
            "vegetative" {
                if ($isPetal) { $tint = $vegetativePetalTint }
            }
            "dormant" { $tint = $dormantTint }
            "dieback" { $tint = $diebackTint }
        }

        $result[$key] = @{ Path = $path; Tint = $tint }
    }
    return $result
}

function Format-TextureJson($phaseTextures) {
    $lines = @()
    foreach ($kv in ($phaseTextures.GetEnumerator() | Sort-Object Name)) {
        $key = $kv.Key
        $path = $kv.Value.Path
        $tint = $kv.Value.Tint
        if ($tint) {
            $lines += "    `"$key`": { `"base`": `"$path`", `"tint`": `"$tint`" }"
        }
        else {
            $lines += "    `"$key`": { `"base`": `"$path`" }"
        }
    }
    return $lines -join ",`n"
}

function Write-PhaseBlockFromJuvenile($species, $phase, $juvenile) {
    $shapeBase = $juvenile.shape.base
    $textures = @{}
    foreach ($prop in $juvenile.textures.PSObject.Properties) {
        $textures[$prop.Name] = $prop.Value
    }

    $phaseTextures = Convert-ToPhaseTextures $textures $phase $species
    $texJson = Format-TextureJson $phaseTextures

    $y2 = 0.55
    if ($juvenile.selectionbox -and $juvenile.selectionbox.y2) {
        $y2 = [math]::Round([double]$juvenile.selectionbox.y2 / 0.45, 2)
        if ($y2 -gt 0.85) { $y2 = 0.85 }
        if ($y2 -lt 0.35) { $y2 = 0.35 }
    }

    $path = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"

    $climateBlock = ""
    if ($phase -eq "dormant") {
        $climateBlock = @"

  "climateColorMap": "climatePlantTint",
"@
    }
    elseif ($phase -eq "dieback") {
        $climateBlock = @"

  "climateColorMap": "climatePlantTint",
  "frostable": true,
"@
    }

    $extraAttrs = ""
    if ($species -eq "heather") {
        $extraAttrs = @"

  "attributes": {
    "overrideRandomDrawOffset": 3
  },
"@
        $y2 = 0.31
    }

    @"
{
  "code": "flowerphase-${species}-${phase}-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,$extraAttrs
  "shape": { "base": "$shapeBase" },
  "textures": {
$texJson
  },$climateBlock
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.15, "y1": 0, "z1": 0.15, "x2": 0.85, "y2": $y2, "z2": 0.85 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
}

function Write-RafflesiaPhases($species, $color) {
    $inside = "game:block/plant/rafflesia/$color/inside"
    $shape = "game:block/plant/rafflesia/$color"

    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        $path = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"
        $tint = switch ($phase) {
            "dormant" { "#5c6b52" }
            "dieback" { "#8b7355" }
            default { $null }
        }

        $insideLine = if ($tint) {
            "    `"inside`": { `"base`": `"$inside`", `"tint`": `"$tint`" },"
        } else {
            "    `"inside`": { `"base`": `"$inside`" },"
        }
        $petalsLine = if ($tint) {
            "    `"petals`": { `"base`": `"$inside`", `"tint`": `"$tint`" }"
        } else {
            "    `"petals`": { `"base`": `"$inside`" }"
        }

        $climateLine = ""
        if ($phase -eq "dormant") {
            $climateLine = "`n  `"climateColorMap`": `"climatePlantTint`","
        }
        elseif ($phase -eq "dieback") {
            $climateLine = "`n  `"climateColorMap`": `"climatePlantTint`",`n  `"frostable`": true,"
        }

        @"
{
  "code": "flowerphase-${species}-${phase}-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "randomizeAxes": "xz",
  "randomDrawOffset": true,
  "shape": { "base": "$shape" },
  "textures": {
$insideLine
$petalsLine
  },$climateLine
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.125, "y1": 0, "z1": 0.125, "x2": 0.875, "y2": 0.25, "z2": 0.875 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
    }
}

function Write-ShapeOnlyPhases($species, $shape, $selectionY2 = 0.55) {
    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        $path = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"
        $climateBlock = ""
        if ($phase -eq "dormant") {
            $climateBlock = @"

  "climateColorMap": "climatePlantTint",
"@
        }
        elseif ($phase -eq "dieback") {
            $climateBlock = @"

  "climateColorMap": "climatePlantTint",
  "frostable": true,
"@
        }

        @"
{
  "code": "flowerphase-${species}-${phase}-free",
  "class": "BlockPlant",
  "enabled": true,
  "renderpass": "OpaqueNoCull",
  "blockmaterial": "Plant",
  "drawtype": "JSON",
  "randomizeRotations": true,
  "randomizeAxes": "xz",
  "randomDrawOffset": true,
  "shape": { "base": "$shape" },$climateBlock
  "sideopaque": { "all": false },
  "sidesolid": { "all": false },
  "replaceable": 3000,
  "resistance": 0.5,
  "lightAbsorption": 0,
  "collisionbox": null,
  "selectionbox": { "x1": 0.15, "y1": 0, "z1": 0.15, "x2": 0.85, "y2": $selectionY2, "z2": 0.85 },
  "sounds": { "place": "game:block/plant", "break": "game:block/plant" },
  "materialDensity": 200,
  "drops": []
}
"@ | Set-Content -Path $path -Encoding UTF8
    }
}

$shapeOnly = @{
    croton = @{ shape = "game:block/plant/croton/small/crimson-green"; y2 = 0.5 }
}

foreach ($kv in $shapeOnly.GetEnumerator()) {
    Write-ShapeOnlyPhases $kv.Key $kv.Value.shape $kv.Value.y2
    Write-Host "Wrote shape-only flowerphase-$($kv.Key)-*"
}

Write-RafflesiaPhases "rafflesiabrown" "brown"
Write-RafflesiaPhases "rafflesiared" "red"
Write-Host "Wrote flowerphase-rafflesia-{brown,red}-*"

$juvenileFiles = Get-ChildItem -Path $plantDir -Filter "juvenile-flower-*-free.json"
$count = 0
foreach ($file in $juvenileFiles) {
    $juvenile = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json
    if (-not $juvenile.code -or -not $juvenile.textures) { continue }

    $species = $juvenile.code -replace "^juvenile-flower-", "" -replace "-free$", ""
    if ($shapeOnly.ContainsKey($species)) { continue }

    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        Write-PhaseBlockFromJuvenile $species $phase $juvenile
        $count++
    }
    Write-Host "Wrote flowerphase-$species-{vegetative,dormant,dieback}"
}

Write-Host "Done. Generated $count phase blocktypes from juvenile sources."

$langDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\lang"
$labelsPath = Join-Path $langDir "flowerphase-labels.json"
$phaseLabels = Get-Content -Raw -Encoding UTF8 -Path $labelsPath | ConvertFrom-Json

function Read-LangSpeciesMap($langPath) {
    $map = @{}
    if (-not (Test-Path $langPath)) { return $map }
    $content = [System.IO.File]::ReadAllText($langPath, [System.Text.Encoding]::UTF8)
    foreach ($m in [regex]::Matches($content, '"ecosystemflora:species-([^"]+)":\s*"([^"]*)"')) {
        $map[$m.Groups[1].Value] = $m.Groups[2].Value
    }
    return $map
}

function Merge-LangEntriesIntoMain($langPath, $entries) {
    if (-not (Test-Path $langPath)) { return }
    $json = [System.IO.File]::ReadAllText($langPath, [System.Text.Encoding]::UTF8)
    $obj = $json | ConvertFrom-Json

    foreach ($key in $entries.Keys) {
        $prop = $obj.PSObject.Properties[$key]
        if ($null -ne $prop) {
            $prop.Value = $entries[$key]
        }
        else {
            $obj | Add-Member -NotePropertyName $key -NotePropertyValue $entries[$key]
        }
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $outJson = ($obj | ConvertTo-Json -Depth 5 -Compress:$false)
    [System.IO.File]::WriteAllText($langPath, $outJson, $utf8NoBom)
}

function Write-FlowerPhaseLang($langCode) {
    $mainPath = Join-Path $langDir "$langCode.json"
    $enPath = Join-Path $langDir "en.json"
    $langData = Read-LangSpeciesMap $mainPath
    $enData = Read-LangSpeciesMap $enPath

    $labels = $phaseLabels.$langCode
    $entries = [ordered]@{}

    foreach ($file in (Get-ChildItem -Path $outDir -Filter "juvenile-flower-*-free.json")) {
        if ($file.Name -match "^juvenile-flower-(.+)-free\.json$") {
            $species = $Matches[1]
            $speciesName = $null
            if ($langData.ContainsKey($species)) { $speciesName = $langData[$species] }
            elseif ($enData.ContainsKey($species)) { $speciesName = $enData[$species] }
            else { $speciesName = ($species.Substring(0, 1).ToUpper() + $species.Substring(1)) }

            $blockKey = "block-juvenile-flower-${species}-free"
            $entries[$blockKey] = "$speciesName ($($labels.seedling))"
        }
    }

    foreach ($file in (Get-ChildItem -Path $outDir -Filter "flowerphase-*-free.json")) {
        if ($file.Name -match "^flowerphase-(.+)-(vegetative|dormant|dieback)-free\.json$") {
            $species = $Matches[1]
            $phase = $Matches[2]

            $speciesName = $null
            if ($langData.ContainsKey($species)) { $speciesName = $langData[$species] }
            elseif ($enData.ContainsKey($species)) { $speciesName = $enData[$species] }
            else { $speciesName = ($species.Substring(0, 1).ToUpper() + $species.Substring(1)) }

            $blockKey = "block-flowerphase-${species}-${phase}-free"
            $phaseLabel = $labels.$phase
            $entries[$blockKey] = "$speciesName ($phaseLabel)"
        }
    }

    $outPath = Join-Path $langDir "$langCode-flowerphases.json"
    $json = ($entries | ConvertTo-Json -Depth 3)
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($outPath, $json, $utf8NoBom)
    Merge-LangEntriesIntoMain $mainPath $entries
    Write-Host "Wrote $($entries.Count) block lang entries to $langCode.json and $outPath"
}

foreach ($langCode in @("en", "ru", "de")) {
    Write-FlowerPhaseLang $langCode
}
