using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace GeoInfluence.Pro;

internal sealed class GeoInfluenceDockPaneViewModel : DockPane
{
    internal const string DockPaneId = "GeoInfluence_Pro_DockPane";

    private string _heading = "Spatial Influence Modeling";

    public string Heading
    {
        get => _heading;
        set => SetProperty(ref _heading, value);
    }

    internal static void Show()
    {
        FrameworkApplication.DockPaneManager.Find(DockPaneId)?.Activate();
    }
}

internal sealed class ShowGeoInfluenceDockPaneButton : Button
{
    protected override void OnClick()
    {
        GeoInfluenceDockPaneViewModel.Show();
    }
}
