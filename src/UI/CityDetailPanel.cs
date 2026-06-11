using Godot;
using CivGame.Core;
using CivGame.UI.CityComponents;

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
            BgColor = new Color(0.15f, 0.12f, 0.08f, 0.98f)
        };
        AddThemeStyleboxOverride("panel", style);
    }

    private void CreateLayout()
    {
        var mainVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        AddChild(mainVBox);

        // --- MASTER HEADER STRIP ---
        var headerStrip = new PanelContainer();
        var stripStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.85f, 0.78f, 0.65f, 0.95f),
            BorderWidthBottom = 2,
            BorderColor = new Color(0.5f, 0.4f, 0.3f)
        };
        headerStrip.AddThemeStyleboxOverride("panel", stripStyle);
        
        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 20);
        headerStrip.AddChild(headerHBox);
        
        mainVBox.AddChild(headerStrip);

        // Header Component
        headerHBox.AddChild(new CityHeaderComponent(_city));

        // --- CONTENT AREA (Interactive Map and Dashboard) ---
        var contentHBox = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        mainVBox.AddChild(contentHBox);

        // 1. Buildings & Stats (Left) - Fixed width
        var leftPanel = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) };
        leftPanel.AddChild(new CityBuildingsListComponent(_city));
        contentHBox.AddChild(leftPanel);

        // 2. Interactive Map (Center) - Expand
        var centerPanel = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        centerPanel.AddChild(new CityInteractiveMapComponent(_city, _sim));
        contentHBox.AddChild(centerPanel);

        // 3. Production & Economy (Right) - Fixed width
        var rightPanel = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) };
        rightPanel.AddChild(new CityProductionQueueComponent(_city));
        rightPanel.AddChild(new CityEconomyModuleComponent(_city));
        contentHBox.AddChild(rightPanel);
    }


}
