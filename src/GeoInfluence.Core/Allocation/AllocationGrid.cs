namespace GeoInfluence.Core.Allocation;

public sealed record AllocationCell(
    int Column,
    int Row,
    double CenterX,
    double CenterY,
    int SiteIndex,
    string SiteId,
    double Score);

public sealed record AllocationGrid(
    double XMin,
    double YMin,
    double XMax,
    double YMax,
    int Columns,
    int Rows,
    double CellWidth,
    double CellHeight,
    IReadOnlyList<AllocationCell> Cells);
