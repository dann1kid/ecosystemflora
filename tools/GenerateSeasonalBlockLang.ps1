# Readable block names for fern/tallgrass phenology + juvenile fern spread blocks.
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$plantDir = Join-Path $repoRoot "assets\ecosystemflora\blocktypes\plant"
$langDir = Join-Path $repoRoot "assets\ecosystemflora\lang"
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

function Resolve-SpeciesName($species, $langData, $enData) {
    if ($langData.ContainsKey($species)) { return $langData[$species] }
    if ($enData.ContainsKey($species)) { return $enData[$species] }
    return ($species.Substring(0, 1).ToUpper() + $species.Substring(1))
}

function Build-SeasonalBlockLang($langCode) {
    $mainPath = Join-Path $langDir "$langCode.json"
    $enPath = Join-Path $langDir "en.json"
    $langData = Read-LangSpeciesMap $mainPath
    $enData = Read-LangSpeciesMap $enPath
    $labels = $phaseLabels.$langCode
    $entries = [ordered]@{}

    foreach ($file in (Get-ChildItem -Path $plantDir -Filter "juvenile-fern-*-free.json")) {
        if ($file.Name -match "^juvenile-fern-(.+)-free\.json$") {
            $species = $Matches[1]
            $speciesName = Resolve-SpeciesName $species $langData $enData
            $blockKey = "block-juvenile-fern-${species}-free"
            $entries[$blockKey] = "$speciesName ($($labels.seedling))"
        }
    }

    foreach ($file in (Get-ChildItem -Path $plantDir -Filter "fernphase-*.json")) {
        if ($file.Name -match "-(free|snow)\.json$") { continue }
        if ($file.Name -match "^fernphase-(.+)-(dormant|dieback)\.json$") {
            $species = $Matches[1]
            $phase = $Matches[2]
            $speciesName = Resolve-SpeciesName $species $langData $enData
            $label = "$speciesName ($($labels.$phase))"
            $entries["block-fernphase-${species}-${phase}-free"] = $label
            $entries["block-fernphase-${species}-${phase}-snow"] = $label
        }
    }

    foreach ($file in (Get-ChildItem -Path $plantDir -Filter "tallgrassphase-*.json")) {
        if ($file.Name -match "-(free|snow)\.json$") { continue }
        if ($file.Name -match "^tallgrassphase-(dormant|dieback)\.json$") {
            $phase = $Matches[1]
            $speciesName = Resolve-SpeciesName "tallgrass" $langData $enData
            $label = "$speciesName ($($labels.$phase))"
            $entries["block-tallgrassphase-${phase}-free"] = $label
            $entries["block-tallgrassphase-${phase}-snow"] = $label
        }
    }

    $outPath = Join-Path $langDir "$langCode-seasonalblocks.json"
    $json = ($entries | ConvertTo-Json -Depth 3)
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($outPath, $json, $utf8NoBom)
    Merge-LangEntriesIntoMain $mainPath $entries
    Write-Host "Wrote $($entries.Count) seasonal block lang entries to $langCode.json and $outPath"
}

foreach ($langCode in @("en", "ru", "de")) {
    Build-SeasonalBlockLang $langCode
}

Write-Host "Done."
