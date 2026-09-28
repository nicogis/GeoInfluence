# GeoInfluence 0.1 model

## Overview

GeoInfluence 0.1 allocates each grid cell to the influence site with the lowest weighted anisotropic distance score.

The model is deterministic and operates in projected working coordinates.

## Input parameters

Each site requires:

- **SiteId** — unique identifier.
- **Weight** — positive finite value. Larger values increase influence.
- **Bearing** — anisotropy orientation in degrees, clockwise from north.
- **MajorScale** — positive finite scale along the major axis.
- **MinorScale** — positive finite scale along the minor axis.

## Anisotropic distance

For each candidate location, the vector from the site to the candidate is rotated into the site's local major/minor coordinate system.

The distance is:

```text
D = sqrt((u / MajorScale)^2 + (v / MinorScale)^2)
```

where:

- `u` is the component along the major axis;
- `v` is the component along the minor axis.

A larger scale makes movement along that local axis less costly.

For example, `MajorScale = 4` and `MinorScale = 1` means that distance along the major axis is four times less restrictive than along the minor axis.

## Weighted score

The current influence score is:

```text
Score = D / Weight
```

The site with the lowest score wins the cell.

## Coordinate system

GeoInfluence performs the model in projected coordinates.

- If the active map is already projected, that spatial reference is used.
- If the active map is geographic, GeoInfluence derives a local UTM zone from the input sites and projects them automatically.
- Automatic UTM projection is limited to the normal UTM latitude range, approximately 80°S to 84°N.

## Winner confidence

For every cell GeoInfluence keeps the best and second-best scores.

```text
Margin = RunnerUpScore - WinnerScore

Confidence = Margin / RunnerUpScore
```

Confidence is normalized to `0..1`.

- Values near `0` indicate a weak winner or near-tie.
- Values near `1` indicate a strong winner.
- A single-site allocation has confidence `1`.

Confidence is diagnostic only. It does not change the winner.

## Disconnected regions

Unlike a classic Euclidean Voronoi diagram, GeoInfluence sites can have different anisotropic metrics and weights. As a result, a site's winning area can legitimately be multipart or disconnected.

GeoInfluence therefore does not automatically remove isolated cells or islands.

## Current limitations

The 0.1 model does not yet include:

- cost or resistance surfaces;
- hard barriers;
- network travel time;
- capacity constraints;
- demand constraints;
- multi-factor expressions;
- temporal behavior;
- probabilistic allocation;
- Monte Carlo uncertainty;
- calibration from observed outcomes.

These are post-0.1 roadmap items.
