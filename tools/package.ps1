<#
.SYNOPSIS
  Build and package ecosystemflora into a release zip.
.DESCRIPTION
  1. Runs unit tests (unless -SkipTests).
  2. Cleans the output directory (removes stale files from previous builds).
  3. Builds the project in the chosen configuration.
  4. Reads the version from modinfo.json.
  5. Creates ecosystemflora-<version>.zip in the repo root.
.PARAMETER Configuration
  MSBuild configuration. Default: Release.
.PARAMETER SkipTests
  Skip `dotnet test` before packaging (not recommended for releases).
.EXAMPLE
  .\tools\package.ps1
  .\tools\package.ps1 -Configuration Debug
  .\tools\package.ps1 -SkipTests
#>
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot  = Split-Path $PSScriptRoot -Parent
$csproj    = Join-Path $repoRoot 'wildfarming.csproj'
$testProj  = Join-Path $repoRoot 'tests\WildFarming.Tests.csproj'
$outputDir = Join-Path $repoRoot "bin\$Configuration\Mods\ecosystemflora"

if (-not $SkipTests) {
    Write-Host "Running tests ..."
    dotnet test $testProj -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw "Tests failed (exit code $LASTEXITCODE)." }
}

# Clean stale output so deleted assets don't linger
if (Test-Path $outputDir) {
    Write-Host "Cleaning $outputDir ..."
    Remove-Item -Recurse -Force $outputDir
}

# Build
Write-Host "Building ($Configuration) ..."
dotnet build $csproj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)." }

# Read version from modinfo.json
$modinfo = Get-Content (Join-Path $outputDir 'modinfo.json') -Raw | ConvertFrom-Json
$version = $modinfo.version
if (-not $version) { throw 'Could not read version from modinfo.json.' }

$zipName = "ecosystemflora-$version.zip"
$zipPath = Join-Path $repoRoot $zipName

if (Test-Path $zipPath) { Remove-Item $zipPath }

$pdbFiles = @(Get-ChildItem $outputDir -Filter *.pdb -Recurse -File -ErrorAction SilentlyContinue)
if ($pdbFiles.Count -gt 0) {
    Write-Host "Removing $($pdbFiles.Count) .pdb file(s) from package output ..."
    $pdbFiles | Remove-Item -Force
}

Write-Host "Packaging $zipName ..."
Compress-Archive -Path "$outputDir\*" -DestinationPath $zipPath -Force

$size = [math]::Round((Get-Item $zipPath).Length / 1KB)
Write-Host "Done: $zipName ($size KB)"
