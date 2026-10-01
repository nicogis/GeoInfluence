# GeoInfluence architecture

GeoInfluence is designed as a spatial influence engine with ArcGIS Pro as its first host.

## Design principles

1. **Core first** — mathematical models do not depend on ArcGIS Pro.
2. **Pluggable distance metrics** — Euclidean, geodesic, anisotropic, cost-surface and network metrics can share the same influence engine.
3. **Pluggable influence models** — weighted, gravity, constrained, probabilistic and custom models can be added independently.
4. **Deterministic and testable** — core calculations are isolated from UI and GIS threading requirements.
5. **ArcGIS integration at the edge** — feature/raster IO, map interaction, symbology and geoprocessing belong to the Pro project.

## Planned solution

```text
GeoInfluence
├── src
│   ├── GeoInfluence.Core
│   └── GeoInfluence.Pro
├── tests
│   └── GeoInfluence.Core.Tests
└── docs
```

## MVP 0.1 — Anisotropic Influence

The 0.1 milestone implements:

- read point sites from an ArcGIS Pro layer;
- map fields to weight, bearing, major scale and minor scale;
- compute an anisotropic influence allocation;
- provide an interactive preview in an ArcGIS Pro dock pane;
- expose winner-confidence diagnostics;
- export grid cells;
- dissolve influence regions by SiteId;
- generate a categorical raster output.

The initial mathematical primitive is an elliptical anisotropic distance:

```text
D = sqrt((u / majorScale)^2 + (v / minorScale)^2)
```

where `u` and `v` are the candidate vector components in the site's local major/minor coordinate system.


## 0.2 architecture direction

GeoInfluence 0.2 starts by separating two concepts that were coupled in 0.1:

- **distance metric** — computes effective spatial distance;
- **influence score model** — converts that distance into a comparable score.

The allocator now depends on both abstractions:

```text
effectiveDistance = distanceMetric.Calculate(site, x, y)
score = influenceScoreModel.CalculateScore(site, effectiveDistance, x, y)
```

The default score model preserves 0.1 behavior:

```text
score = effectiveDistance / Weight
```

This keeps the current results unchanged while creating a stable extension point for future gravity, resistance-aware, capacity-aware, probabilistic, or multi-factor influence models.
