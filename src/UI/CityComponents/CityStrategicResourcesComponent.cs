using Godot;
using CivGame.Core;
using System.Collections.Generic;

namespace CivGame.UI.CityComponents;

public partial class CityStrategicResourcesComponent : HBoxContainer
{
    public CityStrategicResourcesComponent(City city, GameSimulation sim)
    {
        AddThemeConstantOverride("separation", 10);

        var title = new Label { Text = "STRATEGIC RESOURCES" };
        title.AddThemeColorOverride("font_color", Colors.Black);
        AddChild(title);

        var resources = new List<string> { "iron", "horses", "coal", "rubber", "oil" };
        foreach (var resId in resources)
        {
            if (sim.CityHasResourceAccess(city, resId))
            {
                var resLabel = new Label { Text = $"🔗 {resId.ToUpper()}" };
                resLabel.AddThemeColorOverride("font_color", Colors.Black);
                AddChild(resLabel);
            }
        }
    }
}
