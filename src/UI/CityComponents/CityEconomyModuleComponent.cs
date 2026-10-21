using System;
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

        var connectedLuxs = sim.GetCityConnectedLuxuries(city);
        bool hasMarketplace = city.Buildings.Any(b => b.Id == "marketplace");

        // Dictionary to map standard lux IDs to icons
        var luxIcons = new System.Collections.Generic.Dictionary<string, string>
        {
            { "wine", "🍷" }, { "dyes", "🎨" }, { "furs", "🧥" },
            { "spices", "🧂" }, { "ivory", "🐘" }, { "silks", "👘" },
            { "incense", "🕯️" }, { "gems", "💎" }
        };

        if (connectedLuxs.Count == 0)
        {
            var noneLabel = new Label { Text = "No luxuries connected to Trade Network." };
            noneLabel.AddThemeFontSizeOverride("font_size", 10);
            noneLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            luxVBox.AddChild(noneLabel);
        }
        else
        {
            int luxIndex = 1;
            foreach (var luxId in connectedLuxs)
            {
                var luxDef = ResourceRegistry.Get(luxId);
                if (luxDef == null) continue;

                string icon = luxIcons.ContainsKey(luxId) ? luxIcons[luxId] : "📦";
                
                // Calculate happy effect based on marketplace
                int happyFaces = hasMarketplace ? ((luxIndex + 1) / 2) : 1;
                string effectStr = string.Concat(System.Linq.Enumerable.Repeat("😊", happyFaces));

                var iconLabel = new Label { Text = icon };
                iconLabel.AddThemeFontSizeOverride("font_size", 14);
                luxGrid.AddChild(iconLabel);

                var nameLabel = new Label { Text = luxDef.Name };
                nameLabel.AddThemeFontSizeOverride("font_size", 10);
                nameLabel.AddThemeColorOverride("font_color", hasAccessColor(true));
                luxGrid.AddChild(nameLabel);

                var effLabel = new Label { Text = effectStr };
                effLabel.AddThemeFontSizeOverride("font_size", 10);
                luxGrid.AddChild(effLabel);

                luxIndex++;
            }
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

        // Gather base production
        int baseProd = 0;
        var centerTile = sim.Map.GetTile(city.X, city.Y);
        if (centerTile != null) baseProd += Math.Max(1, centerTile.TotalYield.Production);
        foreach (var tilePos in city.WorkedTiles)
        {
            var t = sim.Map.GetTile(tilePos.X, tilePos.Y);
            if (t != null && t.OwnerCityId == city.Id) baseProd += t.TotalYield.Production;
        }

        var pollStats = PollutionSystem.GetCityPollutionStats(city, baseProd);
        var pollStatus = new HBoxContainer();
        pollStatus.AddThemeConstantOverride("separation", 6);
        pollVBox.AddChild(pollStatus);

        if (pollStats.TotalPoints > 0)
        {
            var warnIcon = new Label { Text = "⚠️ ☣️" };
            warnIcon.AddThemeFontSizeOverride("font_size", 14);
            pollStatus.AddChild(warnIcon);

            var warnText = new Label { Text = $"{pollStats.TotalPoints} pts ({pollStats.Probability:P0}/turn chance)" };
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
        var garrisonComp = new CityGarrisonComponent(city, sim);
        garrisonComp.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(garrisonComp);
    }

    private Color hasAccessColor(bool has)
    {
        return has ? new Color(0.12f, 0.1f, 0.05f) : new Color(0.5f, 0.45f, 0.4f, 0.4f);
    }
}
