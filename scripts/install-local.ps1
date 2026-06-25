param(
    [string]$ModName = "InterestPicker"
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$dest = Join-Path $env:USERPROFILE "Documents\Klei\OxygenNotIncluded\mods\local\$ModName"
$dll = Join-Path $repo "src\bin\Release\net48\DupeInterestSets.dll"
$plib = Join-Path $repo "lib\PLib.dll"
$configSource = Join-Path $repo "Config.json"
$configDest = Join-Path $dest "Config.json"

if (!(Test-Path $dll)) {
    throw "Build output not found: $dll"
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null

if (!(Test-Path $configDest)) {
    Copy-Item -Force $configSource $configDest
    Write-Output "Created default Config.json"
} else {
    Write-Output "Kept existing Config.json"
}

$oldDll = Join-Path $dest "InterestPicker.dll"
if (Test-Path $oldDll) {
    Remove-Item -LiteralPath $oldDll -Force
}

try {
    Copy-Item -Force $dll (Join-Path $dest "DupeInterestSets.dll")
} catch {
    throw "Could not update DupeInterestSets.dll. Close Oxygen Not Included and run this script again. Config.json was not overwritten."
}

Copy-Item -Force $plib (Join-Path $dest "PLib.dll")
Copy-Item -Force (Join-Path $repo "mod.yaml") (Join-Path $dest "mod.yaml")
Copy-Item -Force (Join-Path $repo "mod_info.yaml") (Join-Path $dest "mod_info.yaml")
Copy-Item -Force (Join-Path $repo "README.md") (Join-Path $dest "README.md")
Copy-Item -Force (Join-Path $repo "STEAM.md") (Join-Path $dest "STEAM.md")

$translationsSource = Join-Path $repo "translations"
$translationsDest = Join-Path $dest "translations"
if (Test-Path $translationsSource) {
    New-Item -ItemType Directory -Force -Path $translationsDest | Out-Null
    Copy-Item -Force -Recurse (Join-Path $translationsSource "*") $translationsDest
}

Get-ChildItem $dest | Select-Object Name, Length, LastWriteTime
