# Full-size flower phenology blocks derived from juvenile-flower-* assets.
# Fixes invisible phases: wrong stem paths, transparent slots, mismatched shapes.
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "WritePlantPhaseSnowBlock.ps1")
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
    if ($key -match "^plant\d+[ab]$") { return $true }
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

        if ($path -match "transparent") {
            $result[$key] = @{ Path = "game:block/transparent"; Tint = $null }
            continue
        }

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
            "dormant" { if ($isPetal) { $tint = $dormantTint } }
            "dieback" { if ($isPetal) { $tint = $diebackTint } }
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

function Get-JuvenileFreeShape($juvenile) {
    if ($juvenile.shapeByType.'*-free'.base) {
        return $juvenile.shapeByType.'*-free'.base
    }
    if ($juvenile.shape.base) {
        return $juvenile.shape.base
    }
    return "game:block/basic/cross"
}

function Get-JuvenileFreeTextures($juvenile) {
    $textures = @{}
    $source = $juvenile.texturesByType.'*-free'
    if (-not $source -and $juvenile.textures) {
        $source = $juvenile.textures
    }
    if ($source) {
        foreach ($prop in $source.PSObject.Properties) {
            $textures[$prop.Name] = $prop.Value
        }
    }
    return $textures
}

function Write-PhaseBlockFromJuvenile($species, $phase, $juvenile) {
    $shapeBase = Get-JuvenileFreeShape $juvenile
    $textures = Get-JuvenileFreeTextures $juvenile

    $phaseTextures = Convert-ToPhaseTextures $textures $phase $species
    $texJson = Format-TextureJson $phaseTextures

    $y2 = 0.55
    if ($juvenile.selectionbox -and $juvenile.selectionbox.y2) {
        $y2 = [math]::Round([double]$juvenile.selectionbox.y2 / 0.45, 2)
        if ($y2 -gt 0.85) { $y2 = 0.85 }
        if ($y2 -lt 0.35) { $y2 = 0.35 }
    }

    $legacyFree = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"
    if (Test-Path $legacyFree) { Remove-Item $legacyFree -Force }

    $climateBlock = ""
    if ($phase -eq "dormant") {
        $climateBlock = @"

  "climateColorMap": "climatePlantTint",
"@
    }
    elseif ($phase -eq "dieback") {
        $climateBlock = @"

  "climateColorMap": "climatePlantTint",
"@
    }

    $extraTop = ""
    if ($species -eq "heather") {
        $extraTop = "`n  `"randomDrawOffset`": true,"
        $y2 = 0.31
    }

    $snowPaths = $phaseTextures.Values | ForEach-Object { $_.Path }
    $snowTex = Get-SnowCrossTextureFromPaths @($snowPaths)

    $opts = @{
        SelectionY2 = $y2
        UseShapeByType = $true
        ClimateLines = $climateBlock
        ExtraTopLines = $extraTop
    }
    if ($species -eq "heather") {
        $opts.FreeOnlyLines = "`n  `"attributesByType`": { `"*-free`": { `"overrideRandomDrawOffset`": 3 }, `"*-snow`": { `"allowOverlays`": false, `"allowStepWhenStuck`": true } },"
    }

    $baseCode = "flowerphase-${species}-${phase}"
    $shapeInner = "`"base`": `"$shapeBase`""
    Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" $shapeInner $texJson $snowTex $opts
}

function Write-GhostpipePhases($species) {
    $petal = "game:block/plant/flower/petal/${species}*"

    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        $legacyFree = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"
        if (Test-Path $legacyFree) { Remove-Item $legacyFree -Force }

        $northPetal = if ($phase -eq "vegetative") {
            "      `"north1`": { `"base`": `"$petal`", `"tint`": `"#6d7d62`" },"
        } else {
            "      `"north1`": { `"base`": `"$petal`" },"
        }
        $southPetal = if ($phase -eq "vegetative") {
            "      `"south1`": { `"base`": `"$petal`", `"tint`": `"#6d7d62`" },"
        } else {
            "      `"south1`": { `"base`": `"$petal`" },"
        }

        $texJson = @"
$northPetal
      "northTinted1": { "base": "game:block/transparent" },
$southPetal
      "southTinted1": { "base": "game:block/transparent" }
"@

        $climateBlock = ""
        if ($phase -eq "dormant") {
            $climateBlock = "`n  `"climateColorMap`": `"climatePlantTint`","
        }
        elseif ($phase -eq "dieback") {
            $climateBlock = "`n  `"climateColorMap`": `"climatePlantTint`","
        }

        $baseCode = "flowerphase-${species}-${phase}"
        $shapeInner = "`"base`": `"game:block/plant/flower/1patch-3faces-16x16`""
        Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" $shapeInner $texJson $petal @{
            SelectionY2 = 0.25
            UseShapeByType = $true
            ClimateLines = $climateBlock
        }
    }
}

function Write-RafflesiaPhases($species, $color) {
    $inside = "game:block/plant/rafflesia/$color/inside"
    $shape = "game:block/plant/rafflesia/$color"

    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        $legacyFree = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"
        if (Test-Path $legacyFree) { Remove-Item $legacyFree -Force }

        $tint = switch ($phase) {
            "dormant" { "#5c6b52" }
            "dieback" { "#8b7355" }
            default { $null }
        }

        $insideLine = if ($tint) {
            "      `"inside`": { `"base`": `"$inside`", `"tint`": `"$tint`" },"
        } else {
            "      `"inside`": { `"base`": `"$inside`" },"
        }
        $petalsLine = if ($tint) {
            "      `"petals`": { `"base`": `"$inside`", `"tint`": `"$tint`" }"
        } else {
            "      `"petals`": { `"base`": `"$inside`" }"
        }
        $texJson = "$insideLine`n$petalsLine"

        $climateLine = ""
        if ($phase -eq "dormant") {
            $climateLine = "`n  `"climateColorMap`": `"climatePlantTint`","
        }
        elseif ($phase -eq "dieback") {
            $climateLine = "`n  `"climateColorMap`": `"climatePlantTint`","
        }

        $baseCode = "flowerphase-${species}-${phase}"
        $shapeInner = "`"base`": `"$shape`""
        Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" $shapeInner $texJson $inside @{
            SelectionY2 = 0.25
            UseShapeByType = $true
            ClimateLines = $climateLine
            ExtraTopLines = "`n  `"randomizeAxes`": `"xz`",`n  `"randomDrawOffset`": true,"
        }
    }
}

function Write-LupinePhases() {
    $shape = "game:block/plant/lupine/one-plant"
    $snowPetal = "game:block/plant/flower/petal/lupine/blue1-a"

    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        $legacyFree = Join-Path $outDir "flowerphase-lupine-${phase}-free.json"
        if (Test-Path $legacyFree) { Remove-Item $legacyFree -Force }

        $petalTint = switch ($phase) {
            "vegetative" { "#6d7d62" }
            "dormant" { "#5c6b52" }
            "dieback" { "#8b7355" }
            default { $null }
        }

        $texLines = New-Object System.Collections.Generic.List[string]
        foreach ($n in 1..5) {
            foreach ($side in @("a", "b")) {
                $petalKey = "plant${n}${side}"
                $stemKey = "plant${n}${side}stem"
                $petalPath = "game:block/plant/flower/petal/lupine/blue${n}-${side}"
                $stemPath = "game:block/plant/flower/stem/lupine/normal${n}-${side}"
                if ($petalTint) {
                    $texLines.Add("    `"$petalKey`": { `"base`": `"$petalPath`", `"tint`": `"$petalTint`" },")
                } else {
                    $texLines.Add("    `"$petalKey`": { `"base`": `"$petalPath`" },")
                }
                $texLines.Add("    `"$stemKey`": { `"base`": `"$stemPath`" },")
            }
        }
        $texJson = ($texLines -join "`n").TrimEnd(",")

        $climateBlock = ""
        if ($phase -eq "dormant" -or $phase -eq "dieback") {
            $climateBlock = "`n  `"climateColorMap`": `"climatePlantTint`","
        }

        $baseCode = "flowerphase-lupine-$phase"
        $shapeInner = "`"base`": `"$shape`""
        Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" $shapeInner $texJson $snowPetal @{
            SelectionY2 = 0.49
            UseShapeByType = $true
            ClimateLines = $climateBlock
        }
    }
}

function Write-ShapeOnlyPhases($species, $shape, $selectionY2 = 0.55) {
    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        $legacyFree = Join-Path $outDir "flowerphase-${species}-${phase}-free.json"
        if (Test-Path $legacyFree) { Remove-Item $legacyFree -Force }

        $climateBlock = ""
        if ($phase -eq "dormant") {
            $climateBlock = "`n  `"climateColorMap`": `"climatePlantTint`","
        }
        elseif ($phase -eq "dieback") {
            $climateBlock = "`n  `"climateColorMap`": `"climatePlantTint`","
        }

        $baseCode = "flowerphase-${species}-${phase}"
        $shapeInner = "`"base`": `"$shape`""
        Write-PlantPhaseSnowBlock $outDir $baseCode "BlockPlant" $shapeInner "" "game:block/plant/flower/petal/wilddaisy1" @{
            SelectionY2 = $selectionY2
            UseShapeByType = $true
            ClimateLines = $climateBlock
            ExtraTopLines = "`n  `"randomizeAxes`": `"xz`",`n  `"randomDrawOffset`": true,"
        }
    }
}

$ghostpipeSpecies = @("ghostpipewhite", "ghostpipepink", "ghostpipered")

$shapeOnly = @{
    croton = @{ shape = "game:block/plant/croton/small/crimson-green"; y2 = 0.5 }
}

foreach ($kv in $shapeOnly.GetEnumerator()) {
    Write-ShapeOnlyPhases $kv.Key $kv.Value.shape $kv.Value.y2
    Write-Host "Wrote shape-only flowerphase-$($kv.Key)-*"
}

Write-LupinePhases
Write-Host "Wrote flowerphase-lupine-{vegetative,dormant,dieback}"

$skipJuvenilePhase = @("lupine") + $shapeOnly.Keys + $ghostpipeSpecies

Write-RafflesiaPhases "rafflesiabrown" "brown"
Write-RafflesiaPhases "rafflesiared" "red"
Write-Host "Wrote flowerphase-rafflesia-{brown,red}-*"

foreach ($species in $ghostpipeSpecies) {
    Write-GhostpipePhases $species
    Write-Host "Wrote flowerphase-$species-{vegetative,dormant,dieback}"
}

$juvenileFiles = Get-ChildItem -Path $plantDir -Filter "juvenile-flower-*.json" |
    Where-Object { $_.Name -notmatch "-(free|snow)\.json$" }
$count = 0
foreach ($file in $juvenileFiles) {
    $juvenile = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json
    if (-not $juvenile.code) { continue }

    $species = $juvenile.code -replace "^juvenile-flower-", ""
    if ($skipJuvenilePhase -contains $species) { continue }
    if ($shapeOnly.ContainsKey($species)) { continue }
    if ($ghostpipeSpecies -contains $species) { continue }

    $textures = $juvenile.texturesByType.'*-free'
    if (-not $textures) { $textures = $juvenile.textures }
    if (-not $textures) { continue }

    foreach ($phase in @("vegetative", "dormant", "dieback")) {
        Write-PhaseBlockFromJuvenile $species $phase $juvenile
        $count++
    }
    Write-Host "Wrote flowerphase-$species-{vegetative,dormant,dieback}"
}

Write-Host "Done. Generated $count phase blocktypes from juvenile sources."

. (Join-Path $PSScriptRoot "SeasonalLangCommon.ps1")
$langDir = Join-Path $PSScriptRoot "..\assets\ecosystemflora\lang"
$labelsPath = Join-Path $langDir "flowerphase-labels.json"
$phaseLabels = Get-Content -Raw -Encoding UTF8 -Path $labelsPath | ConvertFrom-Json

function Write-FlowerPhaseLang($langCode) {
    $mainPath = Join-Path $langDir "$langCode.json"
    $enPath = Join-Path $langDir "en.json"
    $langData = Read-LangSpeciesMap $mainPath
    $enData = Read-LangSpeciesMap $enPath
    $labels = $phaseLabels.$langCode
    $entries = [ordered]@{}
    Add-FlowerCoverLangEntries $entries $outDir $langData $enData $labels

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
