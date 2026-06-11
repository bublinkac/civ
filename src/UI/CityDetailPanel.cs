using Godot;
using CivGame.Core;
using CivGame.UI.CityComponents;
using System.Linq;

namespace CivGame.UI;

public partial class CityDetailPanel : PanelContainer
{
    private City _city;
    private GameSimulation _sim;

    public CityDetailPanel(City city, GameSimulation sim)
    {
        _city = city;
        _sim = sim;
        ConfigurePanel();
        CreateLayout();
    }

    private void ConfigurePanel()
    {
        SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.1f, 0.08f, 0.98f) // Deep dark brown classic background
        };
        AddThemeStyleboxOverride("panel", style);
    }

    private void CreateLayout()
    {
        // Clear any existing elements first (useful for cycling cities)
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        var mainVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        mainVBox.AddThemeConstantOverride("separation", 10);
        AddChild(mainVBox);

        // ==========================================
        // 1. TOP MASTER HEADER STRIP (3-column layout)
        // ==========================================
        var headerStrip = new PanelContainer();
        var stripStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.88f, 0.78f, 1.0f), // Beautiful full parchment top bar
            BorderWidthBottom = 3,
            BorderColor = new Color(0.45f, 0.35f, 0.25f)
        };
        headerStrip.AddThemeStyleboxOverride("panel", stripStyle);
        mainVBox.AddChild(headerStrip);
        
        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 0);
        headerStrip.AddChild(headerHBox);

        // Column A (Left): Strategic Resources
        var resourcesComp = new CityStrategicResourcesComponent(_city, _sim);
        headerHBox.AddChild(resourcesComp);

        // Column B (Center): Core City Header & Navigation
        var headerComp = new CityHeaderComponent(_city, _sim, () => CycleCity(-1), () => CycleCity(1));
        headerHBox.AddChild(headerComp);

        // Column C (Right): Culture Metrics & Close Button
        var cultureComp = new CityCultureHeaderComponent(_city, _sim, () => QueueFree());
        headerHBox.AddChild(cultureComp);

        // ==========================================
        // 2. MIDDLE AREA (Interactive Map & Citizen heads)
        // ==========================================
        var middleVBox = new VBoxContainer();
        middleVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        middleVBox.AddThemeConstantOverride("separation", 8);
        mainVBox.AddChild(middleVBox);

        // Map View (Centered)
        var mapComp = new CityInteractiveMapComponent(_city, _sim);
        middleVBox.AddChild(mapComp);

        // Citizens Row (Beneath the Map)
        var citizensComp = new CityCitizensComponent(_city);
        middleVBox.AddChild(citizensComp);

        // Divider
        mainVBox.AddChild(new HSeparator());

        // ==========================================
        // 3. BOTTOM SECTION (Horizontal dashboard columns)
        // ==========================================
        var bottomHBox = new HBoxContainer();
        bottomHBox.Alignment = BoxContainer.AlignmentMode.Center;
        bottomHBox.AddThemeConstantOverride("separation", 12);
        mainVBox.AddChild(bottomHBox);

        // Left Panel: Improvements list
        var buildingsComp = new CityBuildingsListComponent(_city);
        bottomHBox.AddChild(buildingsComp);

        // Middle Panel: Economy, Luxuries, Pollution, Garrison
        var economyComp = new CityEconomyModuleComponent(_city, _sim);
        bottomHBox.AddChild(economyComp);

        // Right Panel: Production queue & yield splits
        var productionComp = new CityProductionQueueComponent(_city, _sim);
        bottomHBox.AddChild(productionComp);
    }

    private void CycleCity(int direction)
    {
        var playerCities = _sim.Cities.Where(c => c.Faction == Faction.Player).ToList();
        if (playerCities.Count <= 1) return;

        int currentIndex = playerCities.FindIndex(c => c.Id == _city.Id);
        if (currentIndex == -1) return;

        int nextIndex = (currentIndex + direction + playerCities.Count) % playerCities.Count;
        
        // Defer layout updates to prevent immediate redraw issues in the same frame
        CallDeferred(nameof(ApplyCycleCity), playerCities[nextIndex].Id);
    }

    private void ApplyCycleCity(string nextCityId)
    {
        var nextCity = _sim.Cities.FirstOrDefault(c => c.Id == nextCityId);
        if (nextCity != null)
        {
            _city = nextCity;
            CreateLayout();
            GD.Print($"[City Screen] Cycled to city: {_city.Name}");
        }
    }
}
