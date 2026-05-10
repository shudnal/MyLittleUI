param(
    [switch]$Yes
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

# ---- Mod-specific values -----------------------------------------------------
$ModName = "MyLittleUI"

# Thunderstore team / namespace under which the package is published.
$AuthorName = "shudnal"

# Community URL slug. For Valheim this is normally "valheim" from thunderstore.io/c/valheim/.
$Communities = @("valheim")

# Optional category slugs. You can leave it empty for updates if categories are already set on the site.
$Categories = @()

# Optional per-community categories, e.g. @{ valheim = @("mods") }. Usually not needed for updates.
$CommunityCategories = @{}

$HasNsfwContent = $false
$ThunderstoreBaseUrl = "https://thunderstore.io"
$ThunderstoreTokenFileName = "thunderstore-token.txt"

# After upload has completed, wait a little and print current package review status.
# Thunderstore malware/review checks can mark a package as rejected shortly after submission.
$PackageStatusCheckDelaySeconds = 5

# The current service-account flow normally works as a bearer-like token.
# The script can try fallbacks because the generated swagger still advertises Basic auth.
$ThunderstoreAuthorizationSchemesToTry = @("Bearer", "Token", "Basic")
# -----------------------------------------------------------------------------

$ProjectRoot = $PSScriptRoot
$RepositoryRoot = Split-Path -Parent $ProjectRoot
$CommonScript = Join-Path $RepositoryRoot "API\CommonPublish.ps1"
. $CommonScript

$ApiDir = Join-Path $RepositoryRoot "API"
$Token = Read-SecretFile -Path (Join-Path $ApiDir $ThunderstoreTokenFileName)

$ArchivePath = Join-Path $ProjectRoot "package\thunderstore\$ModName.zip"
Assert-FileExists -Path $ArchivePath
$ArchiveInfo = Get-Item -LiteralPath $ArchivePath

# Catch the most common invalid package mistakes before API upload.
Test-ZipContainsRootEntries -ZipPath $ArchivePath -RequiredEntries @("manifest.json", "README.md", "icon.png", "CHANGELOG.md")

$ThunderstoreManifestText = Get-ZipRootTextEntry -ZipPath $ArchivePath -EntryName "manifest.json"
$ThunderstoreManifest = $ThunderstoreManifestText | ConvertFrom-Json
$PackageVersion = Normalize-PackageVersion -Version ([string]$ThunderstoreManifest.version_number) -Name "Thunderstore package version_number"

$ChangelogText = Get-ZipRootTextEntry -ZipPath $ArchivePath -EntryName "CHANGELOG.md"
if ([string]::IsNullOrWhiteSpace($ChangelogText)) {
    throw "Thunderstore CHANGELOG.md is empty in archive: $ArchivePath"
}

function New-ThunderstoreAuthHeaders {
    param(
        [Parameter(Mandatory = $true)][string]$Token,
        [Parameter(Mandatory = $true)][string]$Scheme
    )

    if ($Scheme -eq "Basic") {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Token + ":")
        return @{
            "Authorization" = "Basic " + [Convert]::ToBase64String($bytes)
            "Accept"        = "application/json"
        }
    }

    return @{
        "Authorization" = "$Scheme $Token"
        "Accept"        = "application/json"
    }
}

function Invoke-ThunderstoreJsonWithAuthFallback {
    param(
        [Parameter(Mandatory = $true)][ValidateSet("GET", "POST", "PUT", "PATCH", "DELETE")][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        $Body = $null
    )

    $lastResponse = $null
    foreach ($scheme in $ThunderstoreAuthorizationSchemesToTry) {
        $headers = New-ThunderstoreAuthHeaders -Token $Token -Scheme $scheme
        $response = Invoke-CurlJson -Method $Method -Url $Url -Headers $headers -Body $Body -AllowHttpError
        $lastResponse = $response

        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
            $script:ThunderstoreAuthHeaders = $headers
            $script:ThunderstoreAuthScheme = $scheme
            return $response
        }

        if ($response.StatusCode -ne 401 -and $response.StatusCode -ne 403) {
            throw "HTTP $($response.StatusCode) $Method $Url`n$($response.Body)"
        }
    }

    throw "Thunderstore authentication failed. Last response: HTTP $($lastResponse.StatusCode)`n$($lastResponse.Body)"
}

function Test-JsonProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Object) {
        return $false
    }

    return @($Object.PSObject.Properties.Name) -contains $Name
}

function Write-ThunderstorePackageStatus {
    param(
        [Parameter(Mandatory = $true)][string]$Namespace,
        [Parameter(Mandatory = $true)][string]$PackageName,
        [Parameter(Mandatory = $true)][string]$ExpectedVersion
    )

    Write-Host ""
    Write-Host "Waiting $PackageStatusCheckDelaySeconds seconds before Thunderstore status check..."
    Start-Sleep -Seconds $PackageStatusCheckDelaySeconds

    $packageUrl = "$ThunderstoreBaseUrl/api/experimental/package/$Namespace/$PackageName/"
    $packageResponse = Invoke-CurlJson -Method GET -Url $packageUrl -Headers $script:ThunderstoreAuthHeaders -AllowHttpError

    if ($packageResponse.StatusCode -lt 200 -or $packageResponse.StatusCode -ge 300) {
        Write-Warning "Could not read Thunderstore package status. HTTP $($packageResponse.StatusCode) $packageUrl"
        if (-not [string]::IsNullOrWhiteSpace($packageResponse.Body)) {
            Write-Host $packageResponse.Body
        }
        return
    }

    if ($null -eq $packageResponse.Json) {
        Write-Warning "Could not parse Thunderstore package status response."
        Write-Host $packageResponse.Body
        return
    }

    $package = $packageResponse.Json

    Write-Host "Thunderstore current package status:"
    Write-Host "  package:   $Namespace-$PackageName"

    if (Test-JsonProperty -Object $package -Name "package_url") {
        Write-Host "  url:       $($package.package_url)"
    }

    if (Test-JsonProperty -Object $package -Name "is_deprecated") {
        Write-Host "  deprecated: $($package.is_deprecated)"
    }

    if (Test-JsonProperty -Object $package -Name "latest" -and $null -ne $package.latest) {
        $latest = $package.latest
        $latestVersion = ""
        $latestActive = ""

        if (Test-JsonProperty -Object $latest -Name "version_number") {
            $latestVersion = [string]$latest.version_number
        }

        if (Test-JsonProperty -Object $latest -Name "is_active") {
            $latestActive = [string]$latest.is_active
        }

        Write-Host "  latest:   version=$latestVersion is_active=$latestActive"

        if (-not [string]::IsNullOrWhiteSpace($latestVersion) -and $latestVersion -ne $ExpectedVersion) {
            Write-Warning "Latest visible Thunderstore version is '$latestVersion', expected just uploaded '$ExpectedVersion'. Review status may still be updating."
        }
    }

    $hasRejectedListing = $false

    if (Test-JsonProperty -Object $package -Name "community_listings" -and $null -ne $package.community_listings) {
        $listings = @($package.community_listings)

        if ($listings.Count -eq 0) {
            Write-Host "  community listings: empty"
        }
        else {
            Write-Host "  community listings:"
            foreach ($listing in $listings) {
                $community = ""
                $reviewStatus = ""
                $hasNsfw = ""
                $categories = ""

                if (Test-JsonProperty -Object $listing -Name "community") {
                    $community = [string]$listing.community
                }
                if (Test-JsonProperty -Object $listing -Name "review_status") {
                    $reviewStatus = [string]$listing.review_status
                }
                if (Test-JsonProperty -Object $listing -Name "has_nsfw_content") {
                    $hasNsfw = [string]$listing.has_nsfw_content
                }
                if (Test-JsonProperty -Object $listing -Name "categories") {
                    $categories = [string]$listing.categories
                }

                Write-Host "    community=$community review_status=$reviewStatus nsfw=$hasNsfw categories=$categories"

                if ($reviewStatus -eq "rejected") {
                    $hasRejectedListing = $true
                }
            }
        }
    }
    else {
        Write-Host "  community listings: not returned"
    }

    $versionUrl = "$ThunderstoreBaseUrl/api/experimental/package/$Namespace/$PackageName/$ExpectedVersion/"
    $versionResponse = Invoke-CurlJson -Method GET -Url $versionUrl -Headers $script:ThunderstoreAuthHeaders -AllowHttpError

    if ($versionResponse.StatusCode -ge 200 -and $versionResponse.StatusCode -lt 300 -and $null -ne $versionResponse.Json) {
        $version = $versionResponse.Json
        $versionActive = ""
        $versionCreated = ""

        if (Test-JsonProperty -Object $version -Name "is_active") {
            $versionActive = [string]$version.is_active
        }
        if (Test-JsonProperty -Object $version -Name "date_created") {
            $versionCreated = [string]$version.date_created
        }

        Write-Host "  uploaded version: version=$ExpectedVersion is_active=$versionActive date_created=$versionCreated"
    }
    else {
        Write-Warning "Uploaded version '$ExpectedVersion' is not readable yet. HTTP $($versionResponse.StatusCode) $versionUrl"
    }

    if ($hasRejectedListing) {
        Write-Warning "Thunderstore package listing status is rejected. Check the package page and Thunderstore moderation/Discord."
    }
}

Write-Host "Thunderstore upload prepared:"
Write-Host "  archive:     $ArchivePath"
Write-Host "  size:        $($ArchiveInfo.Length) bytes"
Write-Host "  package:     $($ThunderstoreManifest.name)"
Write-Host "  version:     $PackageVersion"
Write-Host "  author/team: $AuthorName"
Write-Host "  communities: $($Communities -join ', ')"
Write-Host "  categories:  $($Categories -join ', ')"
Write-Host "  changelog:   $($ChangelogText.Length) chars"

Confirm-ManualPublish -Yes:$Yes -Prompt "This will upload $($ArchiveInfo.Name) to Thunderstore as version $PackageVersion."

Write-Host "Initiating Thunderstore usermedia upload..."
$InitiateBody = @{
    filename        = $ArchiveInfo.Name
    file_size_bytes = [Int64]$ArchiveInfo.Length
}
$Initiate = Invoke-ThunderstoreJsonWithAuthFallback -Method POST -Url "$ThunderstoreBaseUrl/api/experimental/usermedia/initiate-upload/" -Body $InitiateBody
Write-Host "Thunderstore auth scheme accepted: $script:ThunderstoreAuthScheme"

$UserMedia = $Initiate.Json.user_media
$UploadUuid = [string]$UserMedia.uuid
$UploadUrls = @($Initiate.Json.upload_urls)

if ([string]::IsNullOrWhiteSpace($UploadUuid) -or $UploadUrls.Count -eq 0) {
    throw "Thunderstore did not return user_media.uuid or upload_urls. Response: $($Initiate.Body)"
}

Write-Host "Uploading archive parts..."
$TempDir = New-TempDirectory
$CompletedParts = @()

try {
    foreach ($part in $UploadUrls) {
        $partNumber = [int]$part.part_number
        $offset = [Int64]$part.offset
        $length = [Int64]$part.length
        $url = [string]$part.url
        $partPath = Join-Path $TempDir ("part-{0}.bin" -f $partNumber)

        Write-Host "  part $partNumber offset=$offset length=$length"
        New-FilePart -SourcePath $ArchivePath -Offset $offset -Length $length -DestinationPath $partPath
        $uploadResponse = Invoke-CurlUploadFile -Method PUT -Url $url -FilePath $partPath
        $etag = Get-HeaderValue -HeadersText $uploadResponse.Headers -Name "ETag"

        if ([string]::IsNullOrWhiteSpace($etag)) {
            throw "ETag was not returned for Thunderstore upload part $partNumber. Headers: $($uploadResponse.Headers)"
        }

        $CompletedParts += [ordered]@{
            ETag       = $etag
            PartNumber = $partNumber
        }
    }
}
finally {
    Remove-Item -LiteralPath $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Finishing Thunderstore usermedia upload..."
$FinishBody = @{
    parts = @($CompletedParts)
}
$Finish = Invoke-CurlJson -Method POST -Url "$ThunderstoreBaseUrl/api/experimental/usermedia/$UploadUuid/finish-upload/" -Headers $script:ThunderstoreAuthHeaders -Body $FinishBody

if ([string]$Finish.Json.status -ne "upload_complete") {
    Write-Host "Thunderstore finish-upload response: $($Finish.Body)"
}

Write-Host "Submitting Thunderstore package asynchronously..."
$SubmitBody = [ordered]@{
    author_name      = $AuthorName
    communities      = @($Communities)
    has_nsfw_content = [bool]$HasNsfwContent
    upload_uuid      = $UploadUuid
}

if ($Categories.Count -gt 0) {
    $SubmitBody["categories"] = @($Categories)
}

if ($CommunityCategories.Count -gt 0) {
    $SubmitBody["community_categories"] = $CommunityCategories
}

$Submit = Invoke-CurlJson -Method POST -Url "$ThunderstoreBaseUrl/api/experimental/submission/submit-async/" -Headers $script:ThunderstoreAuthHeaders -Body $SubmitBody
$SubmissionId = [string]$Submit.Json.id

if ([string]::IsNullOrWhiteSpace($SubmissionId)) {
    Write-Host "submit-async response did not include id. Response: $($Submit.Body)"
    Write-Host "Trying synchronous submit endpoint as fallback..."
    $SyncSubmit = Invoke-CurlJson -Method POST -Url "$ThunderstoreBaseUrl/api/experimental/submission/submit/" -Headers $script:ThunderstoreAuthHeaders -Body $SubmitBody
    Write-Host "Thunderstore upload completed."
    Write-Host ($SyncSubmit.Body)
    Write-ThunderstorePackageStatus -Namespace $AuthorName -PackageName ([string]$ThunderstoreManifest.name) -ExpectedVersion $PackageVersion
    return
}

Write-Host "Thunderstore submission id: $SubmissionId"
Write-Host "Polling Thunderstore submission result..."

$LastPrintedStatus = ""

for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Seconds 3

    $Poll = Invoke-CurlJson `
        -Method GET `
        -Url "$ThunderstoreBaseUrl/api/experimental/submission/poll-async/$SubmissionId/" `
        -Headers $script:ThunderstoreAuthHeaders `
        -AllowHttpError

    if ($Poll.StatusCode -ge 200 -and $Poll.StatusCode -lt 300) {
        if ($null -eq $Poll.Json) {
            Write-Host "  response: $($Poll.Body)"
            continue
        }

        $PollPropertyNames = @($Poll.Json.PSObject.Properties.Name)

        if ($PollPropertyNames -contains "package_version") {
            Write-Host "Thunderstore upload completed."
            Write-Host ($Poll.Body)

            Write-ThunderstorePackageStatus `
                -Namespace $AuthorName `
                -PackageName ([string]$ThunderstoreManifest.name) `
                -ExpectedVersion $PackageVersion

            return
        }

        $Status = ""
        if ($PollPropertyNames -contains "status") {
            $Status = ([string]$Poll.Json.status).Trim()
        }

        if (-not [string]::IsNullOrWhiteSpace($Status) -and $Status -ne $LastPrintedStatus) {
            Write-Host "  status: $Status"
            $LastPrintedStatus = $Status
        }
        elseif ([string]::IsNullOrWhiteSpace($Status)) {
            Write-Host "  response: $($Poll.Body)"
        }

        if ($PollPropertyNames -contains "task_error" -and [bool]$Poll.Json.task_error) {
            throw "Thunderstore submission task_error=true. Response: $($Poll.Body)"
        }

        if ($PollPropertyNames -contains "form_errors" -and $null -ne $Poll.Json.form_errors) {
            $FormErrorsText = $Poll.Json.form_errors | ConvertTo-Json -Depth 20 -Compress
            if ($FormErrorsText -ne "{}" -and $FormErrorsText -ne "null") {
                throw "Thunderstore submission returned form_errors. Response: $($Poll.Body)"
            }
        }

        $StatusUpper = $Status.ToUpperInvariant()

        if ($StatusUpper -in @("FINISHED", "COMPLETED", "SUCCESS", "SUCCEEDED")) {
            Write-Host "Thunderstore upload completed."

            if ($PollPropertyNames -contains "result" -and -not [string]::IsNullOrWhiteSpace([string]$Poll.Json.result)) {
                Write-Host "Thunderstore submission result:"
                Write-Host ([string]$Poll.Json.result)
            }

            Write-ThunderstorePackageStatus `
                -Namespace $AuthorName `
                -PackageName ([string]$ThunderstoreManifest.name) `
                -ExpectedVersion $PackageVersion

            return
        }

        if ($StatusUpper -in @("FAILED", "FAILURE", "ERROR", "ERRORED", "CANCELLED", "CANCELED", "REVOKED")) {
            throw "Thunderstore submission failed with status '$Status'. Response: $($Poll.Body)"
        }
    }
    elseif ($Poll.StatusCode -eq 404) {
        Write-Host "  poll returned 404, submission may still be initializing"
    }
    else {
        throw "HTTP $($Poll.StatusCode) while polling Thunderstore submission.`n$($Poll.Body)"
    }
}

throw "Thunderstore submission did not finish in time. Submission id: $SubmissionId"
