---
name: prepare-release
description: "Prepare a release for the Feature Management .NET SDK. Use when user mentions release preparation, version bump, creating merge PRs, preview release, or stable release for this project."
---

# Prepare Release

This skill automates the release preparation workflow for the [Feature Management .NET SDK](https://github.com/microsoft/FeatureManagement-Dotnet) project.

## When to Use This Skill

Use this skill when you need to:
- Bump the package version for a new stable or preview release
- Create merge PRs to sync branches (main → release, preview → release)
- Prepare all the PRs needed before publishing a new release

## Background

### Repository Information
- **GitHub Repo**: https://github.com/microsoft/FeatureManagement-Dotnet
- **Packages** (all 3 are released together with the same version):
  1. `Microsoft.FeatureManagement` — Base package
  2. `Microsoft.FeatureManagement.AspNetCore` — ASP.NET Core package
  3. `Microsoft.FeatureManagement.Telemetry.ApplicationInsights` — Application Insights telemetry package

### Branch Structure
- `main` – primary development branch for stable releases
- `preview` – development branch for preview releases
- `release/v{major}` – release branch (e.g., `release/v4`)

### Version Files
The version is defined by `<MajorVersion>`, `<MinorVersion>`, `<PatchVersion>`, and optionally `<PreviewVersion>` properties in **all three** `.csproj` files simultaneously:
1. `src/Microsoft.FeatureManagement/Microsoft.FeatureManagement.csproj`
2. `src/Microsoft.FeatureManagement.AspNetCore/Microsoft.FeatureManagement.AspNetCore.csproj`
3. `src/Microsoft.FeatureManagement.Telemetry.ApplicationInsights/Microsoft.FeatureManagement.Telemetry.ApplicationInsights.csproj`

Each file contains a version block like:
```xml
<!-- Official Version -->
<PropertyGroup>
  <MajorVersion>4</MajorVersion>
  <MinorVersion>1</MinorVersion>
  <PatchVersion>0</PatchVersion>
</PropertyGroup>
```

For preview versions, a `<PreviewVersion>` line is also present:
```xml
<!-- Official Version -->
<PropertyGroup>
  <MajorVersion>4</MajorVersion>
  <MinorVersion>0</MinorVersion>
  <PatchVersion>0</PatchVersion>
  <PreviewVersion>-preview5</PreviewVersion>
</PropertyGroup>
```

### Version Format
- **Stable**: `{major}.{minor}.{patch}` (e.g., `4.1.0`)
- **Preview**: `{major}.{minor}.{patch}-preview{N}` (e.g., `4.0.0-preview5`)

## Quick Start

Ask the user whether this is a **stable** or **preview** release, and what the **new version number** should be. Then follow the appropriate workflow below.

---

### Workflow A: Stable Release

#### Step 1: Version Bump PR

Create a version bump PR targeting `main` by running the version bump script:

```powershell
.\scripts\version-bump.ps1 <new_version>
```

For example: `.\scripts\version-bump.ps1 4.2.0`

The script will automatically:
1. Read the current version from the first `.csproj` file.
2. Create a new branch from `main` named `<username>/version-bump-<new_version>` (e.g., `linglingye/version-bump-4.2.0`).
3. Update `<MajorVersion>`, `<MinorVersion>`, `<PatchVersion>` in all three `.csproj` files, and remove `<PreviewVersion>` if present.
4. Commit, push, and create a PR to `main` with title: `Version bump <new_version>`.

When the script prompts `Proceed? [y/N]`, confirm by entering `y`.

**Sample PR**: https://github.com/microsoft/FeatureManagement-Dotnet/pull/540

#### Step 2: Merge Main to Release Branch

After the version bump PR is merged, create a PR to merge `main` into the release branch by running:

```powershell
.\scripts\merge-to-release.ps1 <new_version>
```

For example: `.\scripts\merge-to-release.ps1 4.2.0`

When the script prompts `Proceed? [y/N]`, confirm by entering `y`.

> **Important**: Use "Create a merge commit" (not "Squash and merge") when merging this PR to preserve commit history.

**Sample PR**: https://github.com/microsoft/FeatureManagement-Dotnet/pull/541

---

### Workflow B: Preview Release

#### Step 1: Version Bump PR

Create a version bump PR targeting `preview` by running the version bump script with the `-Preview` flag:

```powershell
.\scripts\version-bump.ps1 <new_version> -Preview
```

For example: `.\scripts\version-bump.ps1 4.0.0-preview4 -Preview`

When the script prompts `Proceed? [y/N]`, confirm by entering `y`.

**Sample PR**: https://github.com/microsoft/FeatureManagement-Dotnet/pull/476

#### Step 2: Merge Preview to Release Branch

After the version bump PR is merged, create a PR to merge `preview` into the release branch by running:

```powershell
.\scripts\merge-to-release.ps1 <new_version> -Preview
```

For example: `.\scripts\merge-to-release.ps1 4.0.0-preview4 -Preview`

When the script prompts `Proceed? [y/N]`, confirm by entering `y`.

> **Important**: Use "Create a merge commit" (not "Squash and merge") when merging this PR to preserve commit history.

**Sample PR**: https://github.com/microsoft/FeatureManagement-Dotnet/pull/477

---

## Review Checklist

Each PR should be reviewed with the following checks:
- [ ] Version is updated consistently across all 3 `.csproj` files
- [ ] No unintended file changes are included
- [ ] Merge PRs use **merge commit** strategy (not squash)
- [ ] Branch names follow the naming conventions
- [ ] All CI checks pass
