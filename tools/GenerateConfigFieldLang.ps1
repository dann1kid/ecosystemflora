# Generates assets/ecosystemflora/lang/en-configfields.json and ru-configfields.json
# Run: dotnet build && powershell -File tools/GenerateConfigFieldLang.ps1

$ErrorActionPreference = "Stop"
$repo = if (Test-Path "$PSScriptRoot\..\wildfarming.csproj") { Join-Path $PSScriptRoot ".." } else { $PSScriptRoot }

Push-Location $repo
try {
    dotnet test tests/WildFarming.Tests.csproj `
        --filter "FullyQualifiedName~RegenerateLangJsonFiles" `
        -p:DeployModToGame=false `
        --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Regenerated config field lang via ConfigFieldLangBuilder."
}
finally {
    Pop-Location
}
