# GeoInfluence

Advanced spatial influence modeling for ArcGIS Pro.

GeoInfluence extends the classic weighted Voronoi idea with per-site anisotropy, confidence diagnostics, multiple persistent outputs, and an ArcGIS Pro-native workflow.

## MVP 0.1

Version **0.1.0** implements the first complete anisotropic influence workflow.

### Main capabilities

- reads point sites from the active ArcGIS Pro map;
- uses the current selection when present, otherwise the full point layer;
- maps fields for:
  - Site ID;
  - Weight;
  - Bearing;
  - Major scale;
  - Minor scale;
- automatically projects geographic input to a local UTM working coordinate system;
- computes anisotropic weighted allocation on a configurable grid;
- previews allocation in the active map;
- optionally shades the preview by winner confidence;
- exports:
  - allocation cells;
  - dissolved influence regions;
  - categorical raster output;
- applies coherent SiteId symbology to vector and raster outputs;
- validates duplicate IDs and invalid model parameters.

## Mathematical model

The current distance primitive is an elliptical anisotropic distance:

```text
D = sqrt((u / majorScale)^2 + (v / minorScale)^2)
```

where `u` and `v` are the candidate vector components in the site's local major/minor coordinate system.

The current weighted score is:

```text
Score = D / Weight
```

The site with the lowest score wins the grid cell.

A larger `Weight` increases influence. A larger `MajorScale` or `MinorScale` makes distance along that local axis less costly.

Bearing follows GIS convention: clockwise from north.

See [docs/model.md](docs/model.md) for details.

## Winner confidence

Each allocation cell stores the best and second-best scores.

```text
Margin = RunnerUpScore - WinnerScore

Confidence = Margin / RunnerUpScore
```

Confidence is normalized to `0..1`:

- near `0`: the cell is close to a decision boundary;
- near `1`: the winning site is much stronger than the runner-up.

Confidence shading is diagnostic only. GeoInfluence does not silently remove weak or disconnected cells.

## Requirements

- ArcGIS Pro 3.7
- .NET 10
- Visual Studio 2026
- Windows x64

The ArcGIS Pro 3.7 SDK targets .NET 10 and Visual Studio 2026.

## Build from source

Open `GeoInfluence.slnx` in Visual Studio 2026 and build the solution.

For debugging, set `GeoInfluence.Pro` as the startup project and press **F5**. The repository launch profile starts ArcGIS Pro directly.

You can also build a release package from a Developer PowerShell:

```powershell
dotnet build .\src\GeoInfluence.Pro\GeoInfluence.Pro.csproj -c Release
```

The ArcGIS Pro SDK MSBuild targets package the add-in as an `.esriAddInX` during the Release build. The packaging script searches the project's Release output and copies the package into `artifacts\release`.

For a repeatable release copy, use:

```powershell
.\scripts\package-release.ps1
```

See [docs/release.md](docs/release.md).

## Installation

Build or download `GeoInfluence.esriAddInX`, then:

1. double-click the `.esriAddInX` package;
2. confirm the ArcGIS Pro add-in installation;
3. start or restart ArcGIS Pro;
4. open the **GeoInfluence** ribbon tab;
5. click **GeoInfluence** to open the dock pane.

## Basic workflow

1. Add a point feature layer to the active map.
2. Open the GeoInfluence dock pane.
3. Select the point layer.
4. Map `ID`, `Weight`, `Bearing`, `MajorScale`, and `MinorScale`.
5. Click **Load influence sites**.
6. Configure preview resolution and margin.
7. Optionally enable **Confidence shading**.
8. Click **Calculate preview**.
9. Export **cells**, **regions**, and/or **raster**.

See [docs/usage.md](docs/usage.md) for a detailed walkthrough.

## Outputs

### Cells

One polygon per allocation grid cell, including diagnostic attributes such as winner score, runner-up score, score margin, and confidence.

### Regions

Grid cells are dissolved by `SiteId`. A site may legitimately produce multipart or disconnected influence regions under the anisotropic model.

### Raster

Integer categorical raster where pixel values map back to SiteId classes. GeoInfluence applies SiteId labels and the same palette used by the vector outputs.

All persistent outputs are currently written to the ArcGIS Pro project default geodatabase.

## Screenshots

Release screenshots are stored under `docs/screenshots`.

The final MVP release should include at least:

- the GeoInfluence ribbon command;
- the dock pane with mapped fields;
- a normal allocation preview;
- confidence shading;
- dissolved regions and raster output.

See [docs/screenshots/README.md](docs/screenshots/README.md).

## Architecture

```text
GeoInfluence
├── src
│   ├── GeoInfluence.Core
│   └── GeoInfluence.Pro
├── tests
│   └── GeoInfluence.Core.Tests
├── docs
└── scripts
```

- `GeoInfluence.Core`: ArcGIS-independent mathematical engine.
- `GeoInfluence.Pro`: ArcGIS Pro integration, UI, GIS IO, symbology and geoprocessing.
- `GeoInfluence.Core.Tests`: unit tests for the allocation model.

See [docs/architecture.md](docs/architecture.md).

## Roadmap

Post-0.1 candidates include:

- cost/resistance surfaces;
- barriers;
- network/travel-time influence;
- multi-factor expressions;
- capacity-constrained allocation;
- demand-aware models;
- temporal influence;
- probabilistic influence;
- Monte Carlo uncertainty;
- parameter calibration.

## License

MIT — see [LICENSE](LICENSE).
