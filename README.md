# GeoInfluence

Advanced spatial influence modeling for ArcGIS Pro — anisotropic, multi-factor, constrained and probabilistic spatial allocation.

## Status

Early development. The first milestone is **MVP 0.1 — Anisotropic Influence**.

## Why GeoInfluence?

GeoInfluence is intended to go beyond a classic or weighted Voronoi diagram. The long-term goal is a general spatial influence engine supporting multiple distance models, constraints, uncertainty and calibration.

Planned capabilities include:

- anisotropic influence;
- multi-factor influence models;
- cost-surface and barrier-aware allocation;
- network/travel-time allocation;
- capacity-constrained allocation;
- probabilistic influence surfaces;
- temporal models;
- parameter calibration from observed data.

## Architecture

The mathematical engine is kept independent from ArcGIS Pro.

```text
GeoInfluence
├── src
│   ├── GeoInfluence.Core
│   └── GeoInfluence.Pro
├── tests
│   └── GeoInfluence.Core.Tests
└── docs
```

The ArcGIS Pro integration will live in `GeoInfluence.Pro`, while `GeoInfluence.Core` contains reusable and testable mathematical models.

See [docs/architecture.md](docs/architecture.md).

## Target platform

- ArcGIS Pro 3.7
- .NET 10
- Visual Studio 2026
- Windows x64

## License

MIT
