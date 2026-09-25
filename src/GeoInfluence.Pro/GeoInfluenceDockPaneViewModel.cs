using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Pro.Models;

namespace GeoInfluence.Pro;

internal class GeoInfluenceDockPaneViewModel : DockPane
{
    internal const string DockPaneId = "GeoInfluence_Pro_DockPane";

    private readonly ObservableCollection<LayerOption> _pointLayers = [];
    private readonly ObservableCollection<FieldOption> _allFields = [];
    private readonly ObservableCollection<FieldOption> _numericFields = [];

    private LayerOption? _selectedLayer;
    private FieldOption? _selectedIdField;
    private FieldOption? _selectedWeightField;
    private FieldOption? _selectedBearingField;
    private FieldOption? _selectedMajorScaleField;
    private FieldOption? _selectedMinorScaleField;
    private string _heading = "Spatial Influence Modeling";
    private string _status = "Select or refresh a point layer from the active map.";
    private bool _isBusy;

    protected GeoInfluenceDockPaneViewModel()
    {
        RefreshLayersCommand = new RelayCommand(
            () => _ = RefreshLayersAsync());
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

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ObservableCollection<LayerOption> PointLayers => _pointLayers;

    public ObservableCollection<FieldOption> AllFields => _allFields;

    public ObservableCollection<FieldOption> NumericFields => _numericFields;

    public LayerOption? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (SetProperty(ref _selectedLayer, value))
                _ = LoadFieldsAsync(value);
        }
    }

    public FieldOption? SelectedIdField
    {
        get => _selectedIdField;
        set => SetProperty(ref _selectedIdField, value);
    }

    public FieldOption? SelectedWeightField
    {
        get => _selectedWeightField;
        set => SetProperty(ref _selectedWeightField, value);
    }

    public FieldOption? SelectedBearingField
    {
        get => _selectedBearingField;
        set => SetProperty(ref _selectedBearingField, value);
    }

    public FieldOption? SelectedMajorScaleField
    {
        get => _selectedMajorScaleField;
        set => SetProperty(ref _selectedMajorScaleField, value);
    }

    public FieldOption? SelectedMinorScaleField
    {
        get => _selectedMinorScaleField;
        set => SetProperty(ref _selectedMinorScaleField, value);
    }

    public ICommand RefreshLayersCommand { get; }

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

        IsBusy = true;

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
            IsBusy = false;
        }
    }

    private async Task LoadFieldsAsync(LayerOption? layerOption)
    {
        ClearFieldSelections();

        if (layerOption is null)
        {
            Status = "Select a point layer.";
            return;
        }

        IsBusy = true;

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
                AllFields.FirstOrDefault(field => field.FieldType == FieldType.OID)
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
            IsBusy = false;
        }
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
