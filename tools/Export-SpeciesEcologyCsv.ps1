# Regenerate assets/ecosystemflora/species/ecology.csv from C# ecology tables.
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $env:ECOSYSTEMFLORA_EXPORT_SPECIES_CSV = "1"
    dotnet test tests/WildFarming.Tests.csproj `
        -p:DeployModToGame=false `
        --filter "FullyQualifiedName~SpeciesEcologyExportTests.Export_writes_repo_csv_when_requested" `
        --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Wrote $repoRoot/assets/ecosystemflora/species/ecology.csv"
}
finally {
    Remove-Item Env:ECOSYSTEMFLORA_EXPORT_SPECIES_CSV -ErrorAction SilentlyContinue
    Pop-Location
}
