param(
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$modInfoPath = Join-Path $repo "mod_info.yaml"
$versionLine = Get-Content -Path $modInfoPath | Where-Object { $_ -match '^\s*version\s*:' } | Select-Object -First 1
if (!$versionLine) {
    throw "Could not find version in mod_info.yaml"
}

$version = ($versionLine -replace '^\s*version\s*:\s*', '').Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Version in mod_info.yaml is empty"
}

if (!$NoBuild) {
    dotnet build (Join-Path $repo "src\InterestPicker.csproj") -c $Configuration --no-restore
}

$dll = Join-Path $repo "src\bin\$Configuration\net48\DupeInterestSets.dll"
$plib = Join-Path $repo "lib\PLib.dll"
if (!(Test-Path $dll)) {
    throw "Build output not found: $dll"
}
if (!(Test-Path $plib)) {
    throw "PLib.dll not found: $plib"
}

$dist = Join-Path $repo "dist"
$packageRoot = Join-Path $dist "DupeInterestSets"
$zipPath = Join-Path $dist "DupeInterestSets-v$version.zip"

if (Test-Path $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

Copy-Item -Force $dll (Join-Path $packageRoot "DupeInterestSets.dll")
Copy-Item -Force $plib (Join-Path $packageRoot "PLib.dll")
Copy-Item -Force (Join-Path $repo "mod.yaml") (Join-Path $packageRoot "mod.yaml")
Copy-Item -Force $modInfoPath (Join-Path $packageRoot "mod_info.yaml")
Copy-Item -Force (Join-Path $repo "Config.json") (Join-Path $packageRoot "Config.json")
Copy-Item -Force (Join-Path $repo "CHANGELOG.md") (Join-Path $packageRoot "CHANGELOG.md")
Copy-Item -Force (Join-Path $repo "README.md") (Join-Path $packageRoot "README.md")
Copy-Item -Force (Join-Path $repo "STEAM.md") (Join-Path $packageRoot "STEAM.md")

$translationsSource = Join-Path $repo "translations"
if (Test-Path $translationsSource) {
    Copy-Item -Force -Recurse $translationsSource (Join-Path $packageRoot "translations")
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force

Write-Output "Created release package: $zipPath"
Get-ChildItem -Recurse $packageRoot | Select-Object FullName, Length
