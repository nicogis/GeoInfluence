# GeoInfluence 0.1 usage

## 1. Prepare the input layer

Add a point feature layer to the active ArcGIS Pro map.

The layer should contain fields equivalent to:

| Field | Meaning | Example |
|---|---|---:|
| SiteId | Unique site identifier | A |
| Weight | Relative influence strength | 1.2 |
| Bearing | Clockwise degrees from north | 90 |
| MajorScale | Major-axis scale | 3.0 |
| MinorScale | Minor-axis scale | 1.0 |

The actual field names can differ because they are mapped in the GeoInfluence dock pane.

## 2. Open GeoInfluence

Open the **GeoInfluence** ribbon tab and click the **GeoInfluence** button.

The dock pane opens next to the map.

## 3. Select the input layer

Choose a point layer in **Input sites**.

Use **Refresh** if the map contents changed after opening the pane.

If the selected layer contains a feature selection, GeoInfluence loads only the selected points. Otherwise it loads the full layer.

## 4. Map fields

Map:

- ID;
- Weight;
- Bearing;
- Major scale;
- Minor scale.

Common field names are selected automatically when available.

## 5. Load influence sites

Click **Load influence sites**.

GeoInfluence validates:

- required field mappings;
- positive finite Weight;
- finite Bearing;
- positive finite MajorScale;
- positive finite MinorScale;
- unique SiteId values;
- valid point geometry and spatial reference.

If the active map uses geographic coordinates, GeoInfluence automatically derives a local UTM working spatial reference.

## 6. Configure the preview

Set:

- **Resolution** — number of grid cells on each axis, from 10 to 200;
- **Margin %** — expansion of the modeled extent around the input sites;
- **Confidence shading** — optional diagnostic display.

Higher resolution gives finer output but increases processing and overlay cost.

## 7. Calculate preview

Click **Calculate preview**.

Each cell is assigned to the site with the lowest weighted anisotropic score.

With **Confidence shading** enabled, low-confidence cells appear more transparent and high-confidence cells more opaque.

## 8. Clear preview

**Clear preview** removes the temporary map overlay only.

The calculated allocation remains available for export.

## 9. Export cells

**Export cells** writes one polygon per allocation grid cell to the project's default geodatabase.

The output includes winner score, runner-up score, score margin, confidence, and grid row/column information.

## 10. Export regions

**Export regions** dissolves winning cells by SiteId.

The output includes region-level confidence statistics such as minimum and average confidence.

A region can be multipart or disconnected. This is valid for the current anisotropic model.

## 11. Export raster

**Export raster** creates an integer categorical allocation raster in the project's default geodatabase.

Raster classes are relabeled with SiteId values and use the same site palette as the vector outputs.

## Recommended validation workflow

Before using an output analytically:

1. inspect the normal preview;
2. enable confidence shading;
3. inspect isolated or narrow regions;
4. export regions and review confidence statistics;
5. increase preview resolution to check whether questionable cells persist.

Persistent low-confidence islands should be interpreted as mathematically valid but unstable allocation areas rather than silently removed.
