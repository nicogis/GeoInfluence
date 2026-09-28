param(
    [string]$Configuration = "Release",
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot "src\GeoInfluence.Pro"
$configDaml = Join-Path $projectDir "Config.daml"
$outputDir = Join-Path $repoRoot "artifacts\release"

if (-not (Test-Path $configDaml)) {
    throw "Config.daml not found: $configDaml"
}

[xml]$daml = Get-Content $configDaml
$manifestVersion = $daml.ArcGIS.AddInInfo.version

if ($manifestVersion -ne $Version) {
    throw "Version mismatch. Config.daml=$manifestVersion, requested=$Version"
}

$releaseRoot = Join-Path $projectDir "bin\$Configuration"

if (-not (Test-Path $releaseRoot)) {
    throw "Release output not found: $releaseRoot. Build GeoInfluence.Pro first."
}

$packages = Get-ChildItem -Path $releaseRoot -Filter "*.esriAddInX" -File -Recurse | Sort-Object LastWriteTimeUtc -Descending

if (-not $packages) {
    throw "No .esriAddInX package found under $releaseRoot. Build GeoInfluence.Pro with the ArcGIS Pro SDK installed."
}

$package = $packages[0]

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$destination = Join-Path $outputDir "GeoInfluence-$Version.esriAddInX"
Copy-Item $package.FullName $destination -Force

$hash = Get-FileHash $destination -Algorithm SHA256
$checksumPath = "$destination.sha256.txt"
"$($hash.Hash)  $(Split-Path $destination -Leaf)" | Set-Content -Path $checksumPath -Encoding ascii

Write-Host "Release package:"
Write-Host "  $destination"
Write-Host "SHA-256:"
Write-Host "  $($hash.Hash)"
