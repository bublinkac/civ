using Godot;
using CivGame.Core;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityEconomyModuleComponent : PanelContainer
{
    public CityEconomyModuleComponent(City city, GameSimulation sim)
    {
        // Parchment styled box
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.82f, 0.95f), // Parchment
            BorderWidthRight = 2,
            BorderColor = new Color(0.5f, 0.4f, 0.3f),
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(240, 280);
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        // --- 1. LUXURIES SECTION ---
        var luxVBox = new VBoxContainer();
        luxVBox.AddThemeConstantOverride("separation", 2);
        vbox.AddChild(luxVBox);

        var luxTitle = new Label { Text = "LUXURIES" };
        luxTitle.AddThemeFontSizeOverride("font_size", 11);
        luxTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.1f));
        luxVBox.AddChild(luxTitle);

        var luxGrid = new GridContainer { Columns = 3 };
        luxGrid.AddThemeConstantOverride("h_separation", 12);
        luxGrid.AddThemeConstantOverride("v_separation", 2);
        luxVBox.AddChild(luxGrid);

        // List of luxuries connected
        var luxuries = new (string name, string icon, string effect)[]
        {
            ("Wine", "🍷", "😊"),
            ("Gems", "💎", "😊😊"),
            ("Fur", "🧥", "😊"),
            ("Spices", "🧂", "😊"),
        };

        foreach (var lux in luxuries)
        {
            // Assume player always has some luxuries connected or based on courthouse/market
            bool hasLux = city.Buildings.Any(b => b.Id == "marketplace") || city.Population > 2;

            var iconLabel = new Label { Text = lux.icon };
            iconLabel.AddThemeFontSizeOverride("font_size", 14);
            if (!hasLux) iconLabel.Modulate = new Color(1, 1, 1, 0.25f);
            luxGrid.AddChild(iconLabel);

            var nameLabel = new Label { Text = lux.name };
            nameLabel.AddThemeFontSizeOverride("font_size", 10);
            nameLabel.AddThemeColorOverride("font_color", hasAccessColor(hasLux));
            luxGrid.AddChild(nameLabel);

            var effLabel = new Label { Text = hasLux ? lux.effect : "" };
            effLabel.AddThemeFontSizeOverride("font_size", 10);
            luxGrid.AddChild(effLabel);
        }

        vbox.AddChild(new HSeparator());

        // --- 2. POLLUTION SECTION ---
        var pollVBox = new VBoxContainer();
        pollVBox.AddThemeConstantOverride("separation", 2);
        vbox.AddChild(pollVBox);

        var pollTitle = new Label { Text = "POLLUTION" };
        pollTitle.AddThemeFontSizeOverride("font_size", 11);
        pollTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.1f));
        pollVBox.AddChild(pollTitle);

        bool hasIndustry = city.Buildings.Any(b => b.Id == "factory" || b.Id == "coal_plant");
        var pollStatus = new HBoxContainer();
        pollStatus.AddThemeConstantOverride("separation", 6);
        pollVBox.AddChild(pollStatus);

        if (hasIndustry)
        {
            var warnIcon = new Label { Text = "⚠️ ☣️" };
            warnIcon.AddThemeFontSizeOverride("font_size", 14);
            pollStatus.AddChild(warnIcon);

            var warnText = new Label { Text = "Industrial Pollution Active!" };
            warnText.AddThemeFontSizeOverride("font_size", 10);
            warnText.AddThemeColorOverride("font_color", new Color(0.65f, 0.15f, 0.15f));
            pollStatus.AddChild(warnText);
        }
        else
        {
            var clearText = new Label { Text = "None (Environment Clear)" };
            clearText.AddThemeFontSizeOverride("font_size", 10);
            clearText.AddThemeColorOverride("font_color", new Color(0.15f, 0.45f, 0.15f));
            pollStatus.AddChild(clearText);
        }

        vbox.AddChild(new HSeparator());

        // --- 3. GARRISON SECTION ---
        var garVBox = new VBoxContainer();
        garVBox.AddThemeConstantOverride("separation", 2);
        garVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(garVBox);

        var garTitle = new Label { Text = "GARRISON" };
        garTitle.AddThemeFontSizeOverride("font_size", 11);
        garTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.1f));
        garVBox.AddChild(garTitle);

        var garScroll = new ScrollContainer();
        garScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        garScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        garVBox.AddChild(garScroll);

        var garList = new VBoxContainer();
        garScroll.AddChild(garList);

        // Find units in this city
        var stationedUnits = sim.Units.Where(u => u.X == city.X && u.Y == city.Y).ToList();
        if (stationedUnits.Count == 0)
        {
            var emptyLabel = new Label { Text = "No units stationed" };
            emptyLabel.AddThemeFontSizeOverride("font_size", 10);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.45f, 0.4f));
            garList.AddChild(emptyLabel);
        }
        else
        {
            foreach (var unit in stationedUnits)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 6);

                string icon = unit.Type switch
                {
                    UnitType.Settler => "🧑‍🤝‍🧑",
                    UnitType.Worker => "⚒️",
                    UnitType.Explorer => "🧭",
                    UnitType.Warrior => "⚔️",
                    UnitType.Archer => "🏹",
                    _ => "💂"
                };

                var iconLabel = new Label { Text = icon };
                iconLabel.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(iconLabel);

                var unitName = new Label { Text = $"{unit.Type} ({unit.Health}/{unit.MaxHealth} HP)" };
                unitName.AddThemeFontSizeOverride("font_size", 10);
                unitName.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.12f));
                row.AddChild(unitName);

                garList.AddChild(row);
            }
        }
    }

    private Color hasAccessColor(bool has)
    {
        return has ? new Color(0.12f, 0.1f, 0.05f) : new Color(0.5f, 0.45f, 0.4f, 0.4f);
    }
}
