# Writes Properties/launchSettings.json with resolved Vintage Story paths for Visual Studio F5.
$vsDir = Join-Path $env:APPDATA "Vintagestory"
$exe = Join-Path $vsDir "Vintagestory.exe"

if (-not (Test-Path $exe)) {
    Write-Error "Vintage Story not found at: $exe"
    exit 1
}

$launchSettings = @{
    profiles = @{
        "Vintage Story (F5)" = @{
            commandName      = "Executable"
            executablePath   = $exe
            workingDirectory = $vsDir
        }
    }
} | ConvertTo-Json -Depth 4

$outPath = Join-Path $PSScriptRoot "..\Properties\launchSettings.json"
$launchSettings | Set-Content -Path $outPath -Encoding UTF8
Write-Host "Updated: $outPath"
Write-Host "Open wildfarming.sln in Visual Studio and press F5."
