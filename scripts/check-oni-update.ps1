param(
    [int]$AppId = 457140,
    [string]$Branch = "public",
    [string]$BaselineFile = ".github/oni-watch.json",
    [string]$OutputPath = "",
    [string]$MockResponsePath = ""
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return Join-Path (Get-Location).Path $Path
}

function Get-JsonProperty($Object, [string]$Name) {
    return $Object.PSObject.Properties[$Name].Value
}

function Write-GitHubOutput([string]$Name, [string]$Value) {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        return
    }

    $delimiter = "EOF_$([Guid]::NewGuid().ToString('N'))"
    Add-Content -Path $env:GITHUB_OUTPUT -Value "$Name<<$delimiter"
    Add-Content -Path $env:GITHUB_OUTPUT -Value $Value
    Add-Content -Path $env:GITHUB_OUTPUT -Value $delimiter
}

function Get-OniAppInfo {
    if (![string]::IsNullOrWhiteSpace($MockResponsePath)) {
        return Get-Content -Raw -Path (Resolve-RepoPath $MockResponsePath) | ConvertFrom-Json
    }

    $uri = "https://api.steamcmd.net/v1/info/$AppId" + "?pretty=0"
    return Invoke-RestMethod -Uri $uri -TimeoutSec 45
}

$baselinePath = Resolve-RepoPath $BaselineFile
if (!(Test-Path $baselinePath)) {
    throw "Baseline file not found: $baselinePath"
}

$baseline = Get-Content -Raw -Path $baselinePath | ConvertFrom-Json
if ($baseline.appId) {
    $AppId = [int]$baseline.appId
}
if (![string]::IsNullOrWhiteSpace($baseline.branch)) {
    $Branch = [string]$baseline.branch
}

$response = Get-OniAppInfo
if ($response.status -and $response.status -ne "success") {
    throw "SteamCMD API returned status '$($response.status)'"
}

$app = Get-JsonProperty $response.data ([string]$AppId)
if ($null -eq $app) {
    throw "Steam app $AppId was not present in the API response."
}

$branchInfo = Get-JsonProperty $app.depots.branches $Branch
if ($null -eq $branchInfo) {
    throw "Branch '$Branch' was not present in app $AppId info."
}

$currentBuildId = [string]$branchInfo.buildid
if ([string]::IsNullOrWhiteSpace($currentBuildId)) {
    throw "Branch '$Branch' did not include a buildid."
}

$baselineBuildId = [string]$baseline.steamBuildId
$updateDetected = $currentBuildId -ne $baselineBuildId
$timeUpdated = $null
if ($branchInfo.timeupdated) {
    $timeUpdated = [DateTimeOffset]::FromUnixTimeSeconds([int64]$branchInfo.timeupdated).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
}

$issueTitle = "ONI update detected: Steam build $currentBuildId ($Branch)"
$tick = [char]96
$issueBody = @(
    "Oxygen Not Included appears to have a new Steam build on the $tick$Branch$tick branch.",
    "",
    "- App ID: $tick$AppId$tick",
    "- Previous watched Steam build: $tick$baselineBuildId$tick",
    "- Current Steam build: $tick$currentBuildId$tick",
    "- Steam change number: $tick$($app._change_number)$tick",
    "- Branch updated: $tick$timeUpdated$tick",
    "- Watch config: $tick$BaselineFile$tick",
    "",
    "Compatibility checklist:",
    "",
    "- [ ] Rebuild against the latest local ONI assemblies.",
    "- [ ] Verify $tick`SkillGroup$tick registration and available interest IDs.",
    "- [ ] Verify $tick`Db.Initialize$tick patch still runs at the expected point.",
    "- [ ] Verify $tick`MinionStartingStats$tick aptitude generation patch still applies.",
    "- [ ] Verify $tick`CharacterContainer.archetypeDropDownSort$tick still controls reroll dropdown ordering.",
    "- [ ] Test vanilla categories remain unchanged.",
    "- [ ] Test custom categories appear after vanilla categories.",
    "- [ ] Test selecting a custom category produces exactly the configured interests.",
    "- [ ] Test the PLib options editor opens, saves, validates, and requests restart.",
    "- [ ] If compatible, update $tick`.github/oni-watch.json$tick and $tick`mod_info.yaml$tick as appropriate."
) -join "`n"

$result = [ordered]@{
    appId = $AppId
    branch = $Branch
    baselineBuildId = $baselineBuildId
    currentBuildId = $currentBuildId
    updateDetected = $updateDetected
    steamChangeNumber = [string]$app._change_number
    branchUpdatedUtc = $timeUpdated
    issueTitle = $issueTitle
    issueBody = $issueBody
}

$json = $result | ConvertTo-Json -Depth 8
if (![string]::IsNullOrWhiteSpace($OutputPath)) {
    $json | Set-Content -Path (Resolve-RepoPath $OutputPath) -Encoding UTF8
}

Write-GitHubOutput "update_detected" ([string]$updateDetected).ToLowerInvariant()
Write-GitHubOutput "current_build_id" $currentBuildId
Write-GitHubOutput "baseline_build_id" $baselineBuildId
Write-GitHubOutput "issue_title" $issueTitle
Write-GitHubOutput "issue_body" $issueBody

Write-Output $json
