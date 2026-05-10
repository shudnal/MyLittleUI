# Manual Nexus Mods upload script.

param(
    [switch]$Yes
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

# ---- Mod-specific values -----------------------------------------------------
$ModName = "MyLittleUI.zip"

# Nexus game domain from the URL, for Valheim it is always "valheim".
# Example: https://www.nexusmods.com/valheim/mods/1234
$GameDomain = "valheim"

# Nexus mod id from the mod page URL.
# Example: https://www.nexusmods.com/valheim/mods/1234 -> 1234
$GameScopedModId = "2562"

# Optional. If empty, the script uses package\nexus\manifest.json -> name.
# Set this only if the Nexus file update group has a different name than the file name.
$FileGroupName = ""

# Optional escape hatch. Leave empty for automatic lookup.
# If Nexus changes the API or the lookup is ambiguous, you can temporarily set the group id here.
$FileGroupIdOverride = ""

$NexusApiBase = "https://api.nexusmods.com/v3"
$NexusApiKeyFileName = "nexus-api-key.txt"
$NexusDescriptionMaxLength = 255
$NexusApplicationName = "shudnal-valheim-publish"
$NexusApplicationVersion = "1.0.0"
# -----------------------------------------------------------------------------

$ProjectRoot = $PSScriptRoot
$RepositoryRoot = Split-Path -Parent $ProjectRoot
$CommonScript = Join-Path $RepositoryRoot "API\CommonPublish.ps1"
. $CommonScript

function Get-NexusInternalModId {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][string]$GameDomain,
        [Parameter(Mandatory = $true)][string]$GameScopedModId
    )

    if ([string]::IsNullOrWhiteSpace($GameDomain)) {
        throw "Set `$GameDomain in PublishNexus.ps1. For Valheim use 'valheim'."
    }

    if ([string]::IsNullOrWhiteSpace($GameScopedModId) -or $GameScopedModId -eq "PUT_NEXUS_MOD_ID_FROM_URL_HERE") {
        throw "Set `$GameScopedModId in PublishNexus.ps1. Example: https://www.nexusmods.com/valheim/mods/1234 -> 1234."
    }

    Write-Host "Resolving Nexus internal mod id..."
    $response = Invoke-CurlJson `
        -Method GET `
        -Url "$BaseUrl/games/$GameDomain/mods/$GameScopedModId" `
        -Headers $Headers

    if ($null -eq $response.Json -or $null -eq $response.Json.data) {
        throw "Nexus did not return mod data. Response: $($response.Body)"
    }

    $internalModId = [string]$response.Json.data.id
    if ([string]::IsNullOrWhiteSpace($internalModId)) {
        throw "Nexus mod response does not contain data.id. Response: $($response.Body)"
    }

    return $internalModId
}

function Format-NexusFileGroupList {
    param([Parameter(Mandatory = $true)][object[]]$Groups)

    if ($Groups.Count -eq 0) {
        return "<no groups>"
    }

    return (($Groups | ForEach-Object {
        $id = [string]$_.id
        $name = [string]$_.name
        $isActive = [string]$_.is_active
        $versionsCount = [string]$_.versions_count
        $lastUploadedAt = [string]$_.last_file_uploaded_at
        "  $id | '$name' | active=$isActive | versions=$versionsCount | last=$lastUploadedAt"
    }) -join [Environment]::NewLine)
}

function Get-NexusFileGroupId {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][string]$InternalModId,
        [Parameter(Mandatory = $true)][string]$RequestedGroupName
    )

    Write-Host "Resolving Nexus file update group id..."
    $response = Invoke-CurlJson `
        -Method GET `
        -Url "$BaseUrl/mods/$InternalModId/file-update-groups" `
        -Headers $Headers

    if ($null -eq $response.Json -or $null -eq $response.Json.data) {
        throw "Nexus did not return file update group data. Response: $($response.Body)"
    }

    $groups = @($response.Json.data.groups)
    if ($groups.Count -eq 0) {
        throw "Nexus file update groups were not found for internal mod id '$InternalModId'. Upload the first file manually or add a separate create-file flow."
    }

    $activeGroups = @($groups | Where-Object { $true -eq [bool]$_.is_active })
    $inactiveExactGroups = @($groups | Where-Object {
        ([string]$_.name).Equals($RequestedGroupName, [System.StringComparison]::OrdinalIgnoreCase) -and -not ([bool]$_.is_active)
    })

    $exactActiveGroups = @($activeGroups | Where-Object {
        ([string]$_.name).Equals($RequestedGroupName, [System.StringComparison]::OrdinalIgnoreCase)
    })

    if ($exactActiveGroups.Count -eq 1) {
        return [string]$exactActiveGroups[0].id
    }

    if ($exactActiveGroups.Count -gt 1) {
        throw "More than one active Nexus file update group matched '$RequestedGroupName':$([Environment]::NewLine)$(Format-NexusFileGroupList -Groups $exactActiveGroups)"
    }

    if ($inactiveExactGroups.Count -gt 0) {
        throw "Nexus file update group '$RequestedGroupName' exists, but it is not active:$([Environment]::NewLine)$(Format-NexusFileGroupList -Groups $inactiveExactGroups)"
    }

    if ($activeGroups.Count -eq 1) {
        Write-Warning "File update group '$RequestedGroupName' was not found, but there is exactly one active group. Using it: '$($activeGroups[0].name)'."
        return [string]$activeGroups[0].id
    }

    throw "Nexus file update group '$RequestedGroupName' was not found or selection is ambiguous. Set `$FileGroupName to one of available active group names, or set `$FileGroupIdOverride manually.$([Environment]::NewLine)Available groups:$([Environment]::NewLine)$(Format-NexusFileGroupList -Groups $groups)"
}

$ApiDir = Join-Path $RepositoryRoot "API"


function Get-PublishProxyConfigPropertyValue {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-PublishProxyConfigString {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $value = Get-PublishProxyConfigPropertyValue -Object $Object -Name $Name
    if ($null -eq $value) {
        return ""
    }

    return ([string]$value).Trim()
}

function Clear-CurlProxyEnvironment {
    foreach ($name in @("HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "NO_PROXY", "http_proxy", "https_proxy", "all_proxy", "no_proxy")) {
        [System.Environment]::SetEnvironmentVariable($name, $null, "Process")
    }
}

function New-CurlProxyUrlFromConfig {
    param([Parameter(Mandatory = $true)]$Config)

    $rawUrl = Get-PublishProxyConfigString -Object $Config -Name "url"
    if ([string]::IsNullOrWhiteSpace($rawUrl)) {
        throw "curl proxy config is enabled but 'url' is empty."
    }

    $uri = $null
    try {
        $uri = [System.Uri]$rawUrl
    }
    catch {
        throw "curl proxy url is invalid: $rawUrl"
    }

    if ($uri.Scheme -ne "http" -and $uri.Scheme -ne "https" -and $uri.Scheme -ne "socks5" -and $uri.Scheme -ne "socks5h") {
        throw "curl proxy url scheme '$($uri.Scheme)' is not supported by this script. Use http://host:port for your HTTP proxy."
    }

    $hostAndPort = $uri.GetComponents([System.UriComponents]::HostAndPort, [System.UriFormat]::UriEscaped)
    if ([string]::IsNullOrWhiteSpace($hostAndPort)) {
        throw "curl proxy url must contain host and port: $rawUrl"
    }

    $userInfo = $uri.UserInfo
    $username = Get-PublishProxyConfigString -Object $Config -Name "username"
    $password = Get-PublishProxyConfigString -Object $Config -Name "password"

    if (-not [string]::IsNullOrWhiteSpace($username)) {
        $userInfo = [System.Uri]::EscapeDataString($username)
        if (-not [string]::IsNullOrWhiteSpace($password)) {
            $userInfo += ":" + [System.Uri]::EscapeDataString($password)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($userInfo)) {
        return "$($uri.Scheme)://$userInfo@$hostAndPort"
    }

    return "$($uri.Scheme)://$hostAndPort"
}

function Initialize-CurlProxyFromApiDirectory {
    param([Parameter(Mandatory = $true)][string]$ApiDir)

    $proxyConfigPath = Join-Path $ApiDir "curl-proxy.json"
    if (-not (Test-Path -LiteralPath $proxyConfigPath -PathType Leaf)) {
        return
    }

    $configText = Get-Content -LiteralPath $proxyConfigPath -Raw
    if ([string]::IsNullOrWhiteSpace($configText)) {
        throw "curl proxy config is empty: $proxyConfigPath"
    }

    $config = $configText | ConvertFrom-Json

    $enabledValue = Get-PublishProxyConfigPropertyValue -Object $config -Name "enabled"
    $enabled = $true
    if ($null -ne $enabledValue) {
        $enabled = [System.Convert]::ToBoolean($enabledValue)
    }

    if (-not $enabled) {
        Clear-CurlProxyEnvironment
        Write-Host "curl proxy disabled by API\\curl-proxy.json. Existing proxy environment variables were cleared for this process."
        return
    }

    $proxyUrl = New-CurlProxyUrlFromConfig -Config $config
    $safeUri = [System.Uri]$proxyUrl
    $safeProxyUrl = "$($safeUri.Scheme)://$($safeUri.GetComponents([System.UriComponents]::HostAndPort, [System.UriFormat]::UriEscaped))"

    foreach ($name in @("HTTPS_PROXY", "HTTP_PROXY", "ALL_PROXY", "https_proxy", "http_proxy", "all_proxy")) {
        [System.Environment]::SetEnvironmentVariable($name, $proxyUrl, "Process")
    }

    $noProxy = Get-PublishProxyConfigString -Object $config -Name "no_proxy"
    if (-not [string]::IsNullOrWhiteSpace($noProxy)) {
        [System.Environment]::SetEnvironmentVariable("NO_PROXY", $noProxy, "Process")
        [System.Environment]::SetEnvironmentVariable("no_proxy", $noProxy, "Process")
    }

    Write-Host "curl proxy enabled: $safeProxyUrl"
}

Initialize-CurlProxyFromApiDirectory -ApiDir $ApiDir
$ApiKey = Read-SecretFile -Path (Join-Path $ApiDir $NexusApiKeyFileName)

$ArchivePath = Join-Path $ProjectRoot "package\nexus\$ModName.zip"
$ManifestPath = Join-Path $ProjectRoot "package\nexus\manifest.json"

Assert-FileExists -Path $ArchivePath
Assert-FileExists -Path $ManifestPath
$ArchiveInfo = Get-Item -LiteralPath $ArchivePath

$Manifest = Get-JsonFile -Path $ManifestPath

$FileName = [string]$Manifest.name
$Description = ""
if ($null -ne $Manifest.description) {
    $Description = [string]$Manifest.description
}
$Version = Normalize-PackageVersion -Version ([string]$Manifest.version) -Name "Nexus file version"
$FileCategory = [string]$Manifest.file_category

Assert-MaxLength -Value $FileName -Name "Nexus file name" -MaxLength 50
Assert-RegexMatch -Value $FileName -Name "Nexus file name" -Pattern "^[a-zA-Z0-9 _'().-]+$"
Assert-MaxLength -Value $Version -Name "Nexus file version" -MaxLength 50
Assert-MaxLength -Value $Description -Name "Nexus file description" -MaxLength $NexusDescriptionMaxLength

if (@("main", "optional", "miscellaneous") -notcontains $FileCategory) {
    throw "Nexus file_category must be one of: main, optional, miscellaneous. Current value: $FileCategory"
}

if ([string]::IsNullOrWhiteSpace($FileGroupName)) {
    $FileGroupName = $FileName
}

$RequestHeaders = @{
    "apikey"              = $ApiKey
    "Accept"              = "application/json"
    "Application-Name"    = $NexusApplicationName
    "Application-Version" = $NexusApplicationVersion
}

$FileGroupId = ""
if (-not [string]::IsNullOrWhiteSpace($FileGroupIdOverride)) {
    $FileGroupId = $FileGroupIdOverride.Trim()
    Write-Warning "Using manual Nexus file update group id override: $FileGroupId"
}
else {
    $InternalModId = Get-NexusInternalModId `
        -BaseUrl $NexusApiBase `
        -Headers $RequestHeaders `
        -GameDomain $GameDomain `
        -GameScopedModId $GameScopedModId

    $FileGroupId = Get-NexusFileGroupId `
        -BaseUrl $NexusApiBase `
        -Headers $RequestHeaders `
        -InternalModId $InternalModId `
        -RequestedGroupName $FileGroupName
}

if ([string]::IsNullOrWhiteSpace($FileGroupId)) {
    throw "Nexus file update group id is empty."
}

Write-Host "Nexus upload prepared:"
Write-Host "  archive:          $ArchivePath"
Write-Host "  size:             $($ArchiveInfo.Length) bytes"
Write-Host "  game domain:      $GameDomain"
Write-Host "  nexus mod id:     $GameScopedModId"
Write-Host "  file group name:  $FileGroupName"
Write-Host "  file group id:    $FileGroupId"
Write-Host "  file name:        $FileName"
Write-Host "  version:          $Version"
Write-Host "  category:         $FileCategory"
Write-Host "  description:      $($Description.Length)/$NexusDescriptionMaxLength chars"

Confirm-ManualPublish -Yes:$Yes -Prompt "This will upload $($ArchiveInfo.Name) to Nexus Mods as version $Version."

# Nexus v3 single-part upload. For Valheim plugin ZIPs this is normally enough.
# If a package grows above 100 MiB, switch this script to /uploads/multipart.
$MaxSinglePartBytes = 100MB
if ($ArchiveInfo.Length -gt $MaxSinglePartBytes) {
    throw "Archive is larger than 100 MiB. This script intentionally handles only Nexus single-part uploads. Size: $($ArchiveInfo.Length) bytes."
}

Write-Host "Creating Nexus upload session..."
$CreateUploadBody = @{
    size_bytes = [Int64]$ArchiveInfo.Length
    filename   = $ArchiveInfo.Name
}
$CreateUpload = Invoke-CurlJson -Method POST -Url "$NexusApiBase/uploads" -Headers $RequestHeaders -Body $CreateUploadBody
$Upload = $CreateUpload.Json.data
$UploadId = [string]$Upload.id
$PresignedUrl = [string]$Upload.presigned_url

if ([string]::IsNullOrWhiteSpace($UploadId) -or [string]::IsNullOrWhiteSpace($PresignedUrl)) {
    throw "Nexus did not return upload id or presigned_url. Response: $($CreateUpload.Body)"
}

Write-Host "Uploading archive to presigned URL..."
Invoke-CurlUploadFile -Method PUT -Url $PresignedUrl -FilePath $ArchivePath | Out-Null

Write-Host "Finalising Nexus upload..."
Invoke-CurlJson -Method POST -Url "$NexusApiBase/uploads/$UploadId/finalise" -Headers $RequestHeaders | Out-Null

Write-Host "Waiting until Nexus upload becomes available..."
$UploadState = $null
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 2
    $GetUpload = Invoke-CurlJson -Method GET -Url "$NexusApiBase/uploads/$UploadId" -Headers $RequestHeaders
    $UploadState = [string]$GetUpload.Json.data.state
    Write-Host "  state: $UploadState"
    if ($UploadState -eq "available") {
        break
    }
}

if ($UploadState -ne "available") {
    throw "Nexus upload did not become available in time. Upload id: $UploadId, last state: $UploadState"
}

$CreateVersionBody = [ordered]@{
    upload_id                  = $UploadId
    name                       = $FileName
    description                = $Description
    version                    = $Version
    file_category              = $FileCategory
    allow_mod_manager_download = [bool]$Manifest.allow_mod_manager_download
    show_requirements_pop_up   = [bool]$Manifest.show_requirements_pop_up
    archive_existing_file      = [bool]$Manifest.archive_existing_file
}

if ($Manifest.PSObject.Properties.Name -contains "primary_mod_manager_download") {
    $CreateVersionBody["primary_mod_manager_download"] = [bool]$Manifest.primary_mod_manager_download
}

Write-Host "Creating Nexus file update group version..."
$CreateVersion = Invoke-CurlJson -Method POST -Url "$NexusApiBase/mod-file-update-groups/$FileGroupId/versions" -Headers $RequestHeaders -Body $CreateVersionBody

Write-Host "Nexus upload completed."
Write-Host ($CreateVersion.Body)
