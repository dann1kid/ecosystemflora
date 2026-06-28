# Regenerate assets/ecosystemflora/species/season.csv from C# season tables.
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $env:ECOSYSTEMFLORA_EXPORT_SPECIES_SEASON_CSV = "1"
    dotnet test tests/WildFarming.Tests.csproj `
        -p:DeployModToGame=false `
        --filter "FullyQualifiedName~SpeciesSeasonExportTests.Export_writes_repo_csv_when_requested" `
        --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Wrote $repoRoot/assets/ecosystemflora/species/season.csv"
}
finally {
    Remove-Item Env:ECOSYSTEMFLORA_EXPORT_SPECIES_SEASON_CSV -ErrorAction SilentlyContinue
    Pop-Location
}
