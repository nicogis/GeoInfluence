# GeoInfluence 0.1.0 — Anisotropic Influence MVP

GeoInfluence 0.1.0 is the first complete MVP of the ArcGIS Pro spatial influence add-in.

## Highlights

- weighted anisotropic influence allocation;
- GIS bearing and per-site major/minor scales;
- automatic local UTM projection for geographic maps;
- configurable grid resolution and extent margin;
- interactive ArcGIS Pro preview;
- winner-confidence diagnostics;
- confidence shading;
- persistent cell output;
- dissolved influence-region output;
- categorical raster output;
- coherent SiteId symbology across outputs;
- duplicate SiteId and numeric parameter validation;
- light/dark ArcGIS Pro add-in icons;
- ArcGIS Pro 3.7 / .NET 10 / Visual Studio 2026.

## Model

The 0.1 allocation score is:

```text
Score = AnisotropicDistance / Weight
```

The anisotropic distance uses site-specific bearing, major scale and minor scale.

## Confidence diagnostics

Every cell tracks the second-best candidate and exposes RunnerUpScore, ScoreMargin, and Confidence.

Confidence is used as a diagnostic and does not alter allocation.

## Known model behavior

Influence regions may be multipart or disconnected. This can be a legitimate result when sites use different anisotropic metrics and weights.

## Not included in 0.1

Future versions may add resistance/cost surfaces, barriers, network travel-time allocation, capacity and demand constraints, multi-factor expressions, temporal models, probabilistic influence, Monte Carlo uncertainty, and calibration.
