# Updates the local Thunderstore package manifest before ZIP creation.
param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [Parameter(Mandatory = $true)][string]$Version
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Thunderstore manifest was not found: $ManifestPath"
}

# Assembly versions may have a trailing revision; package versions use major.minor.patch.
$NormalizedVersion = $Version.Trim()
if ($NormalizedVersion -notmatch '^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?$') {
    throw "Invalid package version '$Version'. Expected major.minor.patch or major.minor.patch.0."
}
$AssemblyVersion = [System.Version]::Parse($NormalizedVersion)
if ($AssemblyVersion.Revision -gt 0) {
    throw "Package version '$Version' has a nonzero assembly revision that cannot be represented in the manifest."
}
$PackageVersion = "{0}.{1}.{2}" -f $AssemblyVersion.Major, $AssemblyVersion.Minor, $AssemblyVersion.Build

$ResolvedManifestPath = (Resolve-Path -LiteralPath $ManifestPath).ProviderPath
$Manifest = [System.IO.File]::ReadAllText($ResolvedManifestPath) | ConvertFrom-Json
if ($null -eq $Manifest -or $null -eq $Manifest.PSObject.Properties["version_number"]) {
    throw "Thunderstore manifest must contain a version_number property: $ManifestPath"
}

$OldVersion = [string]$Manifest.version_number
if ($OldVersion -eq $PackageVersion) {
    Write-Host "Thunderstore manifest version is already $PackageVersion"
    return
}

$Manifest.version_number = $PackageVersion
$Json = ($Manifest | ConvertTo-Json -Depth 20) + [Environment]::NewLine
$Utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($ResolvedManifestPath, $Json, $Utf8)
Write-Host "Thunderstore manifest version updated: $OldVersion -> $PackageVersion"
