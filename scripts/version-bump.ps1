<#
.SYNOPSIS
    Automates the version bump workflow for Feature Management .NET SDK.
.DESCRIPTION
    Updates MajorVersion, MinorVersion, PatchVersion, and PreviewVersion in all 3 package
    .csproj files, creates a branch, commits, pushes, and opens a PR via the GitHub CLI (gh).
.PARAMETER NewVersion
    The version to bump to (e.g. 4.2.0 or 4.0.0-preview5)
.PARAMETER Preview
    Target the preview branch instead of main
.EXAMPLE
    .\scripts\version-bump.ps1 4.2.0
    # stable release -> PR to main
.EXAMPLE
    .\scripts\version-bump.ps1 4.0.0-preview5 -Preview
    # preview release -> PR to preview
.NOTES
    Prerequisites: git and gh (GitHub CLI) must be installed and authenticated
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$NewVersion,

    [switch]$Preview
)

$ErrorActionPreference = "Stop"

# ── Validate version format ──────────────────────────────────────────────────

if ($NewVersion -notmatch '^\d+\.\d+\.\d+(-preview\d+)?$') {
    Write-Error "Invalid version format '$NewVersion'. Expected: X.Y.Z or X.Y.Z-previewN"
    exit 1
}

if ($NewVersion -match '-preview' -and -not $Preview) {
    Write-Error "Version '$NewVersion' looks like a preview version. Did you forget -Preview?"
    exit 1
}

# ── Parse new version ────────────────────────────────────────────────────────

if ($NewVersion -match '^(\d+)\.(\d+)\.(\d+)(-preview\d+)?$') {
    $NewMajor = $Matches[1]
    $NewMinor = $Matches[2]
    $NewPatch = $Matches[3]
    $NewPreview = if ($Matches[4]) { $Matches[4] } else { $null }
}

# ── Resolve paths & context ──────────────────────────────────────────────────

$ProjectDir = Split-Path $PSScriptRoot -Parent

$CsprojRelPaths = @(
    "src/Microsoft.FeatureManagement/Microsoft.FeatureManagement.csproj",
    "src/Microsoft.FeatureManagement.AspNetCore/Microsoft.FeatureManagement.AspNetCore.csproj",
    "src/Microsoft.FeatureManagement.Telemetry.ApplicationInsights/Microsoft.FeatureManagement.Telemetry.ApplicationInsights.csproj"
)

$CsprojFiles = $CsprojRelPaths | ForEach-Object { Join-Path $ProjectDir $_ }

# Determine target branch
$TargetBranch = if ($Preview) { "preview" } else { "main" }

# Get git username for branch naming
$GitUsername = git config user.name 2>$null
if (-not $GitUsername) {
    Write-Error "Could not determine git user.name. Please set it with: git config user.name <name>"
    exit 1
}
$BranchPrefix = ($GitUsername -split '\s+')[0].ToLower()
$BranchName = "$BranchPrefix/version-bump-$NewVersion"

# ── Show plan ────────────────────────────────────────────────────────────────

Write-Host "-- New version     : $NewVersion"
Write-Host "-- Target branch   : $TargetBranch"
Write-Host "-- New branch      : $BranchName"
Write-Host ""

# ── Confirm with user ────────────────────────────────────────────────────────

$confirm = Read-Host "Proceed? [y/N]"
if ($confirm -notmatch '^[Yy]$') {
    Write-Host "Aborted."
    exit 0
}
Write-Host ""

# ── Create branch from target ────────────────────────────────────────────────

Push-Location $ProjectDir
try {
    Write-Host "-- Fetching latest $TargetBranch..."
    git fetch origin $TargetBranch

    Write-Host "-- Creating branch '$BranchName' from origin/$TargetBranch..."
    git checkout -b $BranchName "origin/$TargetBranch"

    # ── Read current version ─────────────────────────────────────────────────

    $content = [System.IO.File]::ReadAllText($CsprojFiles[0])
    $curMajor = if ($content -match '<MajorVersion>(\d+)</MajorVersion>') { $Matches[1] } else { throw "Could not find MajorVersion" }
    $curMinor = if ($content -match '<MinorVersion>(\d+)</MinorVersion>') { $Matches[1] } else { throw "Could not find MinorVersion" }
    $curPatch = if ($content -match '<PatchVersion>(\d+)</PatchVersion>') { $Matches[1] } else { throw "Could not find PatchVersion" }
    $curPreview = if ($content -match '<PreviewVersion>([^<]+)</PreviewVersion>') { $Matches[1] } else { "" }
    $CurrentVersion = "$curMajor.$curMinor.$curPatch$curPreview"

    Write-Host "-- Current version : $CurrentVersion"

    if ($CurrentVersion -eq $NewVersion) {
        throw "Current version is already $NewVersion. Nothing to do."
    }

    # ── Update version in all .csproj files ──────────────────────────────────

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)

    foreach ($csproj in $CsprojFiles) {
        $fileName = Split-Path $csproj -Leaf
        Write-Host "-- Updating $fileName..."
        $text = [System.IO.File]::ReadAllText($csproj)

        # Update MajorVersion, MinorVersion, PatchVersion
        $text = $text -replace '<MajorVersion>\d+</MajorVersion>', "<MajorVersion>$NewMajor</MajorVersion>"
        $text = $text -replace '<MinorVersion>\d+</MinorVersion>', "<MinorVersion>$NewMinor</MinorVersion>"
        $text = $text -replace '<PatchVersion>\d+</PatchVersion>', "<PatchVersion>$NewPatch</PatchVersion>"

        # Handle PreviewVersion
        if ($NewPreview) {
            # Add or update PreviewVersion
            if ($text -match '<PreviewVersion>[^<]*</PreviewVersion>') {
                $text = $text -replace '<PreviewVersion>[^<]*</PreviewVersion>', "<PreviewVersion>$NewPreview</PreviewVersion>"
            } else {
                # Insert PreviewVersion after PatchVersion line
                $text = $text -replace '(<PatchVersion>\d+</PatchVersion>)', "`$1`n    <PreviewVersion>$NewPreview</PreviewVersion>"
            }
        } else {
            # Remove PreviewVersion line if present (stable release)
            $text = $text -replace '\s*<PreviewVersion>[^<]*</PreviewVersion>', ''
        }

        [System.IO.File]::WriteAllText($csproj, $text, $utf8NoBom)
    }

    # ── Verify changes ───────────────────────────────────────────────────────

    Write-Host "-- Verifying updates..."
    foreach ($csproj in $CsprojFiles) {
        $text = [System.IO.File]::ReadAllText($csproj)
        if ($text -notmatch "<MajorVersion>$NewMajor</MajorVersion>") { throw "MajorVersion not updated in $(Split-Path $csproj -Leaf)" }
        if ($text -notmatch "<MinorVersion>$NewMinor</MinorVersion>") { throw "MinorVersion not updated in $(Split-Path $csproj -Leaf)" }
        if ($text -notmatch "<PatchVersion>$NewPatch</PatchVersion>") { throw "PatchVersion not updated in $(Split-Path $csproj -Leaf)" }
        if ($NewPreview -and $text -notmatch "<PreviewVersion>$([regex]::Escape($NewPreview))</PreviewVersion>") {
            throw "PreviewVersion not updated in $(Split-Path $csproj -Leaf)"
        }
    }
    Write-Host "-- All version files updated"
    Write-Host ""

    # ── Commit, push, and create PR ──────────────────────────────────────────

    Write-Host "-- Committing changes..."
    git add $CsprojRelPaths
    git commit -m "Version bump $NewVersion"

    Write-Host "-- Pushing branch '$BranchName'..."
    git push origin $BranchName

    Write-Host "-- Creating pull request..."
    $Body = @"
Bump version from ``$CurrentVersion`` to ``$NewVersion``.

### Changes
- Updated version properties in all 3 package .csproj files:
  - ``Microsoft.FeatureManagement.csproj``
  - ``Microsoft.FeatureManagement.AspNetCore.csproj``
  - ``Microsoft.FeatureManagement.Telemetry.ApplicationInsights.csproj``

---
*This PR was created automatically by ``scripts/version-bump.ps1``.*
"@

    $PrUrl = gh pr create --base $TargetBranch --head $BranchName --title "Version bump $NewVersion" --body $Body

    Write-Host ""
    Write-Host "-- Done! PR created: $PrUrl"
}
finally {
    Pop-Location
}
