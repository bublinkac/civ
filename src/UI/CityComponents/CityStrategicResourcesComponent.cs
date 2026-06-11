using Godot;
using CivGame.Core;
using System.Collections.Generic;

namespace CivGame.UI.CityComponents;

public partial class CityStrategicResourcesComponent : PanelContainer
{
    public CityStrategicResourcesComponent(City city, GameSimulation sim)
    {
        // Parchment panel styling
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.88f, 0.78f, 0.0f), // Transparent background, let parent strip handle it
            BorderColor = new Color(0.45f, 0.38f, 0.28f, 0.3f),
            BorderWidthRight = 2
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(280, 0);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        AddChild(vbox);

        var title = new Label { Text = "STRATEGIC RESOURCES" };
        title.AddThemeFontSizeOverride("font_size", 11);
        title.AddThemeColorOverride("font_color", new Color(0.25f, 0.2f, 0.12f));
        title.AddThemeConstantOverride("outline_size", 0);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var grid = new GridContainer { Columns = 4 };
        grid.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 2);
        vbox.AddChild(grid);

        // List of resources: id, name, icon
        var resources = new (string id, string name, string icon)[]
        {
            ("iron", "Iron", "⚙️"),
            ("coal", "Coal", "🪨"),
            ("horses", "Horses", "🐎"),
            ("rubber", "Rubber", "🛞")
        };

        foreach (var res in resources)
        {
            var itemBox = new VBoxContainer();
            itemBox.Alignment = BoxContainer.AlignmentMode.Center;

            var hasAccess = sim.CityHasResourceAccess(city, res.id);

            // Icon Label
            var iconLabel = new Label { Text = res.icon };
            iconLabel.AddThemeFontSizeOverride("font_size", 20);
            iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
            if (!hasAccess)
            {
                iconLabel.Modulate = new Color(1f, 1f, 1f, 0.3f); // Translucent if no access
            }
            itemBox.AddChild(iconLabel);

            // Quantity Label
            var qtyLabel = new Label { Text = hasAccess ? "1" : "0" };
            qtyLabel.AddThemeFontSizeOverride("font_size", 10);
            qtyLabel.AddThemeColorOverride("font_color", hasAccess ? new Color(0.1f, 0.45f, 0.15f) : new Color(0.55f, 0.15f, 0.15f));
            qtyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            itemBox.AddChild(qtyLabel);

            // Tooltip
            itemBox.TooltipText = $"{res.name}: {(hasAccess ? "Connected to City Network" : "No Access")}";

            grid.AddChild(itemBox);
        }
    }
}
