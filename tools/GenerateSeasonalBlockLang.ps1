# Readable block names for fern/tallgrass/sedge phenology + juvenile fern spread blocks.
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "SeasonalLangCommon.ps1")

$repoRoot = Split-Path $PSScriptRoot -Parent
$plantDir = Join-Path $repoRoot "assets\ecosystemflora\blocktypes\plant"
$langDir = Join-Path $repoRoot "assets\ecosystemflora\lang"
$labelsPath = Join-Path $langDir "flowerphase-labels.json"
$phaseLabels = Get-Content -Raw -Encoding UTF8 -Path $labelsPath | ConvertFrom-Json

function Build-SeasonalBlockLang($langCode) {
    $mainPath = Join-Path $langDir "$langCode.json"
    $enPath = Join-Path $langDir "en.json"
    $langData = Read-LangSpeciesMap $mainPath
    $enData = Read-LangSpeciesMap $enPath
    $labels = $phaseLabels.$langCode
    $entries = [ordered]@{}

    Add-CoverLangEntries $entries $plantDir $langData $enData $labels @(
        "^juvenile-fern-",
        "^juvenile-sedge-",
        "^fernphase-",
        "^tallgrassphase-",
        "^sedgephase-"
    )

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
