# GeoInfluence release process

## Version

GeoInfluence MVP release version:

```text
0.1.0
```

The ArcGIS Pro add-in version is defined in `src/GeoInfluence.Pro/Config.daml`.

## Prerequisites

Release packaging should be performed on a Windows development machine with:

- ArcGIS Pro 3.7;
- ArcGIS Pro SDK for .NET 3.7;
- .NET 10 SDK;
- Visual Studio 2026.

## 1. Update main

```powershell
git switch main
git pull
```

## 2. Run Core tests

```powershell
dotnet test .\tests\GeoInfluence.Core.Tests\GeoInfluence.Core.Tests.csproj -c Release
```

## 3. Build the ArcGIS Pro project

```powershell
dotnet build .\src\GeoInfluence.Pro\GeoInfluence.Pro.csproj -c Release
```

The ArcGIS Pro SDK build targets create the `.esriAddInX` package in the project Release output.

## 4. Copy the release artifact

Run:

```powershell
.\scripts\package-release.ps1
```

The script validates the manifest version, finds the generated package under the Release output, copies it to `artifacts\release\GeoInfluence-0.1.0.esriAddInX`, and calculates a SHA-256 checksum.

## 5. Smoke test the package

Install the copied package on a clean ArcGIS Pro profile or test machine.

Verify:

- GeoInfluence tab and icons;
- dock pane opening;
- light and dark themes;
- point layer discovery;
- field mapping;
- site loading and validation;
- normal preview;
- confidence preview;
- cells export;
- regions export;
- raster export.

## 6. Create the GitHub release

Recommended tag:

```text
v0.1.0
```

Recommended release title:

```text
GeoInfluence 0.1.0 — Anisotropic Influence MVP
```

Attach the `.esriAddInX`, the checksum text file, and screenshots.

Use [release-notes-0.1.0.md](release-notes-0.1.0.md) as the release description.

## Release checklist

- [ ] main is up to date;
- [ ] Core tests pass;
- [ ] Release build succeeds;
- [ ] `.esriAddInX` generated;
- [ ] package installs successfully;
- [ ] light theme verified;
- [ ] dark theme verified;
- [ ] preview verified;
- [ ] confidence verified;
- [ ] cells export verified;
- [ ] regions export verified;
- [ ] raster export verified;
- [ ] screenshots updated;
- [ ] tag `v0.1.0` created;
- [ ] GitHub release published.
