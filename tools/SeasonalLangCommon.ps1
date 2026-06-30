# Shared helpers for seasonal / phenology block display names.
$ErrorActionPreference = "Stop"

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

function Get-CoverVariantBlockCodes($plantDir) {
    $codes = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($file in Get-ChildItem -Path $plantDir -Filter "*.json") {
        $block = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json
        if (-not $block.code) { continue }

        $hasCover = $false
        if ($block.variantgroups) {
            foreach ($vg in $block.variantgroups) {
                if ($vg.code -eq "cover") {
                    $hasCover = $true
                    foreach ($state in $vg.states) {
                        [void]$codes.Add("$($block.code)-$state")
                    }
                    break
                }
            }
        }
        if ($hasCover) { continue }

        if ($block.code -match "^fernphase-.+-(dormant|dieback|sporulating)$") {
            [void]$codes.Add($block.code)
            [void]$codes.Add("$($block.code)-snow")
        }
    }
    return $codes
}

function Add-CoverLangEntries(
    $entries,
    $plantDir,
    $langData,
    $enData,
    $labels,
    [string[]]$CodePatterns
) {
    foreach ($file in Get-ChildItem -Path $plantDir -Filter "*.json") {
        $block = Get-Content -Raw -Path $file.FullName | ConvertFrom-Json
        if (-not $block.code) { continue }

        $matched = $false
        foreach ($pattern in $CodePatterns) {
            if ($block.code -match $pattern) { $matched = $true; break }
        }
        if (-not $matched) { continue }

        $species = $null
        $phase = $null
        $isSeedling = $false

        if ($block.code -match "^juvenile-fern-(.+)$") {
            $species = $Matches[1]
            $isSeedling = $true
        }
        elseif ($block.code -match "^juvenile-sedge-(.+)$") {
            $species = $Matches[1]
            $isSeedling = $true
        }
        elseif ($block.code -match "^fernphase-(.+)-(dormant|dieback|sporulating)$") {
            $species = $Matches[1]
            $phase = $Matches[2]
        }
        elseif ($block.code -match "^tallgrassphase-(dormant|dieback)$") {
            $species = "tallgrass"
            $phase = $Matches[1]
        }
        elseif ($block.code -match "^sedgephase-(dormant|dieback)$") {
            $species = "brownsedge"
            $phase = $Matches[1]
        }

        if (-not $species) { continue }
        $speciesName = Resolve-SpeciesName $species $langData $enData

        if ($isSeedling) {
            $label = "$speciesName ($($labels.seedling))"
            $prefix = if ($block.code.StartsWith("juvenile-sedge")) { "juvenile-sedge" } else { "juvenile-fern" }
            $entries["block-${prefix}-${species}-free"] = $label
            $entries["block-${prefix}-${species}-snow"] = $label
            continue
        }

        $phaseLabel = $labels.$phase
        $label = "$speciesName ($phaseLabel)"
        if ($block.variantgroups) {
            $entries["block-$($block.code)-free"] = $label
            $entries["block-$($block.code)-snow"] = $label
        }
        else {
            $entries["block-$($block.code)"] = $label
            $entries["block-$($block.code)-snow"] = $label
        }
    }
}

function Add-FlowerCoverLangEntries($entries, $plantDir, $langData, $enData, $labels) {
    foreach ($file in Get-ChildItem -Path $plantDir -Filter "juvenile-flower-*.json" |
        Where-Object { $_.Name -notmatch "-(free|snow)\.json$" }) {
        if ($file.Name -notmatch "^juvenile-flower-(.+)\.json$") { continue }
        $species = $Matches[1]
        $speciesName = Resolve-SpeciesName $species $langData $enData
        $label = "$speciesName ($($labels.seedling))"
        $entries["block-juvenile-flower-${species}-free"] = $label
        $entries["block-juvenile-flower-${species}-snow"] = $label
    }

    foreach ($file in Get-ChildItem -Path $plantDir -Filter "flowerphase-*.json" |
        Where-Object { $_.Name -notmatch "-(free|snow)\.json$" }) {
        if ($file.Name -notmatch "^flowerphase-(.+)-(vegetative|dormant|dieback)\.json$") { continue }
        $species = $Matches[1]
        $phase = $Matches[2]
        $speciesName = Resolve-SpeciesName $species $langData $enData
        $label = "$speciesName ($($labels.$phase))"
        $entries["block-flowerphase-${species}-${phase}-free"] = $label
        $entries["block-flowerphase-${species}-${phase}-snow"] = $label
    }
}
