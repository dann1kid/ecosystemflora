# Copies examples/ecologysample-mynewplant to a standalone git working tree (e.g. public GitHub clone).
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination
)

$ErrorActionPreference = "Stop"
$Source = Join-Path $PSScriptRoot "..\examples\ecologysample-mynewplant" | Resolve-Path

if (-not (Test-Path $Source)) {
    throw "Source not found: $Source"
}

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

# Robocopy: mirror sample folder; exclude .git if present in destination only
robocopy $Source $Destination /E /XD .git /NFL /NDL /NJH /NJS /nc /ns /np
if ($LASTEXITCODE -ge 8) { exit $LASTEXITCODE }
exit 0
