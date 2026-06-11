using Godot;
using CivGame.Core;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityBuildingsListComponent : PanelContainer
{
    public CityBuildingsListComponent(City city)
    {
        // Parchment styled box
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.82f, 0.95f), // Parchment
            BorderWidthRight = 2,
            BorderColor = new Color(0.5f, 0.4f, 0.3f),
            CornerRadiusBottomLeft = 10
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(250, 280);
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        var title = new Label { Text = "IMPROVEMENTS" };
        title.AddThemeFontSizeOverride("font_size", 12);
        title.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.1f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        vbox.AddChild(scroll);

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(list);

        if (city.Buildings.Count == 0)
        {
            var noneLabel = new Label { Text = "(none)" };
            noneLabel.AddThemeFontSizeOverride("font_size", 11);
            noneLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.45f, 0.4f));
            noneLabel.HorizontalAlignment = HorizontalAlignment.Center;
            list.AddChild(noneLabel);
        }
        else
        {
            foreach (var b in city.Buildings.OrderBy(b => b.Name))
            {
                var row = new HBoxContainer();
                row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddThemeConstantOverride("separation", 6);

                // 1. Icon
                string icon = getBuildingIcon(b.Id);
                var iconLabel = new Label { Text = icon };
                iconLabel.AddThemeFontSizeOverride("font_size", 14);
                row.AddChild(iconLabel);

                // 2. Name
                var nameLabel = new Label { Text = b.Name };
                nameLabel.AddThemeFontSizeOverride("font_size", 11);
                nameLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.1f, 0.05f));
                nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(nameLabel);

                // 3. Modifiers (Music Notes / Smileys)
                string modifiers = getBuildingModifiers(b.Id);
                if (!string.IsNullOrEmpty(modifiers))
                {
                    var modLabel = new Label { Text = modifiers };
                    modLabel.AddThemeFontSizeOverride("font_size", 11);
                    row.AddChild(modLabel);
                }

                list.AddChild(row);
            }
        }
    }

    private string getBuildingIcon(string id)
    {
        return id switch
        {
            "palace" => "🏰",
            "granary" => "🌾",
            "barracks" => "⚔️",
            "temple" => "🏛️",
            "cathedral" => "⛪",
            "colosseum" => "🏟️",
            "library" => "📖",
            "university" => "🎓",
            "bank" => "🏦",
            "marketplace" => "⚖️",
            "aqueduct" => "🚰",
            "hospital" => "🏥",
            "factory" => "⚙️",
            "coal_plant" => "🪨",
            _ => "🏢"
        };
    }

    private string getBuildingModifiers(string id)
    {
        return id switch
        {
            "temple" => "🎵🎵 😊",
            "cathedral" => "🎵🎵🎵🎵 😊😊",
            "colosseum" => "😊😊",
            "library" => "🎵🎵🎵",
            "university" => "🎵🎵🎵🎵",
            "monument" => "🎵",
            "research_lab" => "🎵🎵",
            "barracks" => "🎖️",
            "airport" => "✈️",
            _ => ""
        };
    }
}
