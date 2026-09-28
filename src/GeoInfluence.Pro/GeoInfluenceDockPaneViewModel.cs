using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Allocation;
using GeoInfluence.Core.Models;
using GeoInfluence.Pro.Models;
using GeoInfluence.Pro.Services;

namespace GeoInfluence.Pro;

internal class GeoInfluenceDockPaneViewModel : DockPane
{
    internal const string DockPaneId = "GeoInfluence_Pro_DockPane";

    private readonly ObservableCollection<LayerOption> _pointLayers = [];
    private readonly ObservableCollection<FieldOption> _allFields = [];
    private readonly ObservableCollection<FieldOption> _numericFields = [];
    private readonly ObservableCollection<InfluenceSite> _loadedSites = [];

    private LayerOption? _selectedLayer;
    private FieldOption? _selectedIdField;
    private FieldOption? _selectedWeightField;
    private FieldOption? _selectedBearingField;
    private FieldOption? _selectedMajorScaleField;
    private FieldOption? _selectedMinorScaleField;
    private string _heading = "Spatial Influence Modeling";
    private string _status = "Select or refresh a point layer from the active map.";
    private string _loadScope = "No sites loaded.";
    private bool _isBusy;
    private SpatialReference? _workingSpatialReference;
    private int _previewResolution = 40;
    private double _extentMarginPercent = 20.0;
    private bool _showConfidence;
    private AllocationGrid? _lastAllocationGrid;

    protected GeoInfluenceDockPaneViewModel()
    {
        RefreshLayersCommand = new RelayCommand(
            () => _ = RefreshLayersAsync());

        LoadSitesCommand = new RelayCommand(
            () => _ = LoadSitesAsync());

        CalculatePreviewCommand = new RelayCommand(
            () => _ = CalculatePreviewAsync());

        ClearPreviewCommand = new RelayCommand(
            () => _ = ClearPreviewAsync());

        ExportPolygonsCommand = new RelayCommand(
            () => _ = ExportPolygonsAsync());

        ExportRasterCommand = new RelayCommand(
            () => _ = ExportRasterAsync());

        ExportRegionsCommand = new RelayCommand(
            () => _ = ExportRegionsAsync());
    }

    public string Heading
    {
        get => _heading;
        set => SetProperty(ref _heading, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string LoadScope
    {
        get => _loadScope;
        private set => SetProperty(ref _loadScope, value);
    }

    public override bool IsBusy => _isBusy;

    public bool CanLoadSites =>
        !IsBusy &&
        SelectedLayer is not null &&
        SelectedIdField is not null &&
        SelectedWeightField is not null &&
        SelectedBearingField is not null &&
        SelectedMajorScaleField is not null &&
        SelectedMinorScaleField is not null;

    public bool CanCalculatePreview =>
        !IsBusy &&
        LoadedSites.Count > 0 &&
        _workingSpatialReference is not null;

    public bool CanExport =>
        !IsBusy &&
        _lastAllocationGrid is not null &&
        _workingSpatialReference is not null;

    public bool CanClearPreview =>
        !IsBusy &&
        _lastAllocationGrid is not null;

    public int PreviewResolution
    {
        get => _previewResolution;
        set
        {
            if (SetProperty(ref _previewResolution, value))
                _lastAllocationGrid = null;
        }
    }

    public double ExtentMarginPercent
    {
        get => _extentMarginPercent;
        set
        {
            if (SetProperty(ref _extentMarginPercent, value))
                _lastAllocationGrid = null;
        }
    }

    public bool ShowConfidence
    {
        get => _showConfidence;
        set => SetProperty(ref _showConfidence, value);
    }

    public ObservableCollection<LayerOption> PointLayers => _pointLayers;

    public ObservableCollection<FieldOption> AllFields => _allFields;

    public ObservableCollection<FieldOption> NumericFields => _numericFields;

    public ObservableCollection<InfluenceSite> LoadedSites => _loadedSites;

    public LayerOption? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (SetProperty(ref _selectedLayer, value))
            {
                NotifyActionStateChanged();
                _ = LoadFieldsAsync(value);
            }
        }
    }

    public FieldOption? SelectedIdField
    {
        get => _selectedIdField;
        set
        {
            if (SetProperty(ref _selectedIdField, value))
                NotifyActionStateChanged();
        }
    }

    public FieldOption? SelectedWeightField
    {
        get => _selectedWeightField;
        set
        {
            if (SetProperty(ref _selectedWeightField, value))
                NotifyActionStateChanged();
        }
    }

    public FieldOption? SelectedBearingField
    {
        get => _selectedBearingField;
        set
        {
            if (SetProperty(ref _selectedBearingField, value))
                NotifyActionStateChanged();
        }
    }

    public FieldOption? SelectedMajorScaleField
    {
        get => _selectedMajorScaleField;
        set
        {
            if (SetProperty(ref _selectedMajorScaleField, value))
                NotifyActionStateChanged();
        }
    }

    public FieldOption? SelectedMinorScaleField
    {
        get => _selectedMinorScaleField;
        set
        {
            if (SetProperty(ref _selectedMinorScaleField, value))
                NotifyActionStateChanged();
        }
    }

    public ICommand RefreshLayersCommand { get; }

    public ICommand LoadSitesCommand { get; }

    public ICommand CalculatePreviewCommand { get; }

    public ICommand ClearPreviewCommand { get; }

    public ICommand ExportPolygonsCommand { get; }

    public ICommand ExportRasterCommand { get; }

    public ICommand ExportRegionsCommand { get; }

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RefreshLayersAsync();
    }

    internal static void Show()
    {
        FrameworkApplication.DockPaneManager.Find(DockPaneId)?.Activate();
    }

    private async Task RefreshLayersAsync()
    {
        if (IsBusy)
            return;

        SetBusy(true);

        try
        {
            var layers = await QueuedTask.Run(() =>
            {
                var map = MapView.Active?.Map;
                if (map is null)
                    return new List<LayerOption>();

                return map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .Where(layer => layer.ShapeType == esriGeometryType.esriGeometryPoint)
                    .Select(layer => new LayerOption(layer.Name, layer))
                    .OrderBy(layer => layer.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });

            var previousLayerName = SelectedLayer?.Name;

            PointLayers.Clear();
            foreach (var layer in layers)
                PointLayers.Add(layer);

            SelectedLayer = PointLayers.FirstOrDefault(
                                layer => string.Equals(
                                    layer.Name,
                                    previousLayerName,
                                    StringComparison.Ordinal))
                            ?? PointLayers.FirstOrDefault();

            Status = PointLayers.Count == 0
                ? "No point feature layers found in the active map."
                : $"{PointLayers.Count} point layer(s) available.";
        }
        catch (Exception ex)
        {
            Status = $"Unable to read point layers: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadFieldsAsync(LayerOption? layerOption)
    {
        ClearFieldSelections();
        ClearLoadedSites();

        if (layerOption is null)
        {
            Status = "Select a point layer.";
            return;
        }

        SetBusy(true);

        try
        {
            var fields = await QueuedTask.Run(() =>
                layerOption.Layer
                    .GetTable()
                    .GetDefinition()
                    .GetFields()
                    .Select(field => new FieldOption(
                        field.Name,
                        field.AliasName,
                        field.FieldType))
                    .ToList());

            foreach (var field in fields)
                AllFields.Add(field);

            foreach (var field in fields.Where(IsNumericField))
                NumericFields.Add(field);

            SelectedIdField =
                AllFields.FirstOrDefault(field =>
                    string.Equals(field.Name, "ID", StringComparison.OrdinalIgnoreCase))
                ?? AllFields.FirstOrDefault(field => field.FieldType == FieldType.OID)
                ?? AllFields.FirstOrDefault();

            SelectedWeightField = FindFieldByName("Weight");
            SelectedBearingField = FindFieldByName("Bearing");
            SelectedMajorScaleField = FindFieldByName("MajorScale");
            SelectedMinorScaleField = FindFieldByName("MinorScale");

            Status = $"{fields.Count} field(s) loaded from '{layerOption.Name}'.";
        }
        catch (Exception ex)
        {
            Status = $"Unable to read fields from '{layerOption.Name}': {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadSitesAsync()
    {
        if (IsBusy)
            return;

        if (SelectedLayer is null ||
            SelectedIdField is null ||
            SelectedWeightField is null ||
            SelectedBearingField is null ||
            SelectedMajorScaleField is null ||
            SelectedMinorScaleField is null)
        {
            Status = "Select a point layer and map all required fields before loading sites.";
            return;
        }

        SetBusy(true);

        try
        {
            var result = await QueuedTask.Run(() =>
                InfluenceSiteReader.Read(
                    SelectedLayer.Layer,
                    SelectedIdField,
                    SelectedWeightField,
                    SelectedBearingField,
                    SelectedMajorScaleField,
                    SelectedMinorScaleField));

            LoadedSites.Clear();
            foreach (var site in result.Sites)
                LoadedSites.Add(site);

            _workingSpatialReference = result.WorkingSpatialReference;
            NotifyActionStateChanged();

            var sourceText = result.UsedSelection
                ? $"{LoadedSites.Count} selected site(s) loaded."
                : $"{LoadedSites.Count} site(s) loaded from the full layer.";

            var spatialReferenceText = result.AutoProjected
                ? $" Working SR: EPSG:{result.WorkingSpatialReference.Wkid} (automatic UTM projection)."
                : $" Working SR: EPSG:{result.WorkingSpatialReference.Wkid}.";

            LoadScope = sourceText + spatialReferenceText;

            Status = LoadedSites.Count == 0
                ? "No point features were available to load."
                : "Influence sites validated and loaded into GeoInfluence.Core using projected working coordinates.";
        }
        catch (Exception ex)
        {
            ClearLoadedSites();
            Status = $"Unable to load influence sites: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CalculatePreviewAsync()
    {
        if (IsBusy)
            return;

        if (LoadedSites.Count == 0 || _workingSpatialReference is null)
        {
            Status = "Load at least one influence site before calculating the preview.";
            return;
        }

        if (PreviewResolution < 10 || PreviewResolution > 200)
        {
            Status = "Preview resolution must be between 10 and 200.";
            return;
        }

        if (!double.IsFinite(ExtentMarginPercent) ||
            ExtentMarginPercent < 0 ||
            ExtentMarginPercent > 200)
        {
            Status = "Extent margin must be between 0 and 200 percent.";
            return;
        }

        SetBusy(true);

        try
        {
            var sites = LoadedSites.ToList();

            var minX = sites.Min(site => site.X);
            var maxX = sites.Max(site => site.X);
            var minY = sites.Min(site => site.Y);
            var maxY = sites.Max(site => site.Y);

            var spanX = maxX - minX;
            var spanY = maxY - minY;
            var referenceSpan = Math.Max(spanX, spanY);

            if (!double.IsFinite(referenceSpan) || referenceSpan <= 0)
                referenceSpan = 1.0;

            var margin = referenceSpan * (ExtentMarginPercent / 100.0);

            if (spanX <= 0)
            {
                minX -= referenceSpan * 0.5;
                maxX += referenceSpan * 0.5;
            }

            if (spanY <= 0)
            {
                minY -= referenceSpan * 0.5;
                maxY += referenceSpan * 0.5;
            }

            minX -= margin;
            minY -= margin;
            maxX += margin;
            maxY += margin;

            var resolution = PreviewResolution;

            var grid = new AnisotropicGridAllocator().Allocate(
                sites,
                minX,
                minY,
                maxX,
                maxY,
                columns: resolution,
                rows: resolution);

            _lastAllocationGrid = grid;
            NotifyActionStateChanged();

            var workingSpatialReference = _workingSpatialReference;
            var showConfidence = ShowConfidence;

            await QueuedTask.Run(() =>
                PreviewOverlayManager.Render(
                    grid,
                    workingSpatialReference,
                    showConfidence));

            Status = ShowConfidence
                ? $"Confidence preview rendered: {resolution} x {resolution} cells. More opaque cells have a stronger winner margin."
                : $"Preview rendered: {resolution} x {resolution} cells, {sites.Count} influence site(s).";
        }
        catch (Exception ex)
        {
            Status = $"Unable to calculate preview: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ClearPreviewAsync()
    {
        await QueuedTask.Run(PreviewOverlayManager.Clear);
        _lastAllocationGrid = null;
        NotifyActionStateChanged();
        Status = "Allocation preview cleared.";
    }

    private async Task ExportPolygonsAsync()
    {
        if (IsBusy)
            return;

        if (_lastAllocationGrid is null || _workingSpatialReference is null)
        {
            Status = "Calculate a preview before exporting the polygon output.";
            return;
        }

        SetBusy(true);

        try
        {
            var grid = _lastAllocationGrid;
            var workingSpatialReference = _workingSpatialReference;

            var result = await QueuedTask.Run(() =>
                AllocationOutputWriter.WriteToDefaultGeodatabase(
                    grid,
                    workingSpatialReference));

            Status =
                $"Created '{result.FeatureClassName}' with {result.FeatureCount} polygon cell(s) in the project default geodatabase.";
        }
        catch (Exception ex)
        {
            Status = $"Unable to export polygon output: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExportRegionsAsync()
    {
        if (IsBusy)
            return;

        if (_lastAllocationGrid is null || _workingSpatialReference is null)
        {
            Status = "Calculate a preview before exporting influence regions.";
            return;
        }

        SetBusy(true);

        try
        {
            var grid = _lastAllocationGrid;
            var workingSpatialReference = _workingSpatialReference;

            var result = await QueuedTask.Run(() =>
                InfluenceRegionOutputWriter.WriteToDefaultGeodatabase(
                    grid,
                    workingSpatialReference));

            Status =
                $"Created '{result.FeatureClassName}' with {result.FeatureCount} dissolved influence region(s).";
        }
        catch (Exception ex)
        {
            Status = $"Unable to export influence regions: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExportRasterAsync()
    {
        if (IsBusy)
            return;

        if (_lastAllocationGrid is null || _workingSpatialReference is null)
        {
            Status = "Calculate a preview before exporting the raster output.";
            return;
        }

        SetBusy(true);

        string? temporaryFeatureClassPath = null;

        try
        {
            var grid = _lastAllocationGrid;
            var workingSpatialReference = _workingSpatialReference;

            var preparation = await QueuedTask.Run(() =>
                RasterOutputWriter.Prepare(
                    grid,
                    workingSpatialReference));

            temporaryFeatureClassPath = preparation.TemporaryFeatureClassPath;

            var parameters = Geoprocessing.MakeValueArray(
                preparation.TemporaryFeatureClassPath,
                "SiteIndex",
                preparation.OutputRasterPath,
                preparation.CellSize);

            var environments = Geoprocessing.MakeEnvironmentArray(
                overwriteoutput: true);

            var result = await Geoprocessing.ExecuteToolAsync(
                "conversion.FeatureToRaster",
                parameters,
                environments,
                null,
                null,
                GPExecuteToolFlags.AddOutputsToMap |
                GPExecuteToolFlags.GPThread |
                GPExecuteToolFlags.AddToHistory);

            if (result.IsFailed)
            {
                throw new InvalidOperationException(
                    "Feature To Raster failed. Check the geoprocessing messages for details.");
            }

            var rasterName = Path.GetFileName(preparation.OutputRasterPath);

            var symbologyApplied = await QueuedTask.Run(() =>
                RasterSymbologyManager.ApplySiteIdColorizer(
                    grid,
                    rasterName));

            Status = symbologyApplied
                ? $"Created raster '{rasterName}' with cell size {preparation.CellSize:F3} map unit(s) and SiteId symbology."
                : $"Created raster '{rasterName}' with cell size {preparation.CellSize:F3} map unit(s). The raster was created, but its SiteId symbology could not be applied automatically.";
        }
        catch (Exception ex)
        {
            Status = $"Unable to export raster output: {ex.Message}";
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryFeatureClassPath))
            {
                try
                {
                    var deleteParameters =
                        Geoprocessing.MakeValueArray(temporaryFeatureClassPath);

                    await Geoprocessing.ExecuteToolAsync(
                        "management.Delete",
                        deleteParameters,
                        null,
                        null,
                        null,
                        GPExecuteToolFlags.GPThread);
                }
                catch
                {
                    // Best-effort cleanup. The raster result remains valid.
                }
            }

            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        if (_isBusy == value)
            return;

        _isBusy = value;
        NotifyPropertyChanged(nameof(IsBusy));
        NotifyActionStateChanged();
    }

    private void ClearFieldSelections()
    {
        AllFields.Clear();
        NumericFields.Clear();

        SelectedIdField = null;
        SelectedWeightField = null;
        SelectedBearingField = null;
        SelectedMajorScaleField = null;
        SelectedMinorScaleField = null;
    }

    private void ClearLoadedSites()
    {
        LoadedSites.Clear();
        _workingSpatialReference = null;
        _lastAllocationGrid = null;
        LoadScope = "No sites loaded.";
        NotifyActionStateChanged();
    }

    private void NotifyActionStateChanged()
    {
        NotifyPropertyChanged(nameof(CanLoadSites));
        NotifyPropertyChanged(nameof(CanCalculatePreview));
        NotifyPropertyChanged(nameof(CanExport));
        NotifyPropertyChanged(nameof(CanClearPreview));
    }

    private FieldOption? FindFieldByName(string name)
    {
        return NumericFields.FirstOrDefault(field =>
            string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNumericField(FieldOption field)
    {
        return field.FieldType is
            FieldType.SmallInteger or
            FieldType.Integer or
            FieldType.Single or
            FieldType.Double;
    }
}

internal sealed class ShowGeoInfluenceDockPaneButton : Button
{
    protected override void OnClick()
    {
        GeoInfluenceDockPaneViewModel.Show();
    }
}
