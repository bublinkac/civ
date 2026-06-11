using Godot;
using CivGame.Core;
using System;
using System.Linq;

namespace CivGame.UI.CityComponents;

public partial class CityProductionQueueComponent : PanelContainer
{
    public CityProductionQueueComponent(City city, GameSimulation sim)
    {
        // Parchment styled box
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.82f, 0.95f), // Parchment
            BorderColor = new Color(0.5f, 0.4f, 0.3f),
            CornerRadiusBottomRight = 10
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(490, 280);
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var hsplit = new HBoxContainer();
        hsplit.AddThemeConstantOverride("separation", 15);
        margin.AddChild(hsplit);

        // ==========================================
        // LEFT COLUMN: Yield Bars (Food, Prod, Comm, Granary)
        // ==========================================
        var leftCol = new VBoxContainer();
        leftCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftCol.AddThemeConstantOverride("separation", 8);
        hsplit.AddChild(leftCol);

        // Gather base yields (similar to GameSimulation.CollectCityYields)
        int baseFood = 0, baseProd = 0, baseComm = 0;

        // City Center
        var centerTile = sim.Map.GetTile(city.X, city.Y);
        if (centerTile != null)
        {
            baseFood += centerTile.TotalYield.Food;
            baseProd += centerTile.TotalYield.Production;
            baseComm += centerTile.TotalYield.Commerce;
        }

        // Surrounding worked tiles
        foreach (var tilePos in city.WorkedTiles)
        {
            var t = sim.Map.GetTile(tilePos.X, tilePos.Y);
            if (t != null && t.OwnerCityId == city.Id)
            {
                baseFood += t.TotalYield.Food;
                baseProd += t.TotalYield.Production;
                baseComm += t.TotalYield.Commerce;
            }
        }

        // Apply Wonder/Small Wonder multipliers
        if (city.Buildings.Any(b => b.Id == "forbidden_palace")) baseComm = (int)Math.Round(baseComm * 1.5f);
        if (city.Buildings.Any(b => b.Id == "iron_works")) baseProd *= 2;

        int foodConsumption = city.Population * 2;
        int netFood = baseFood - foodConsumption;

        // --- 1. PRODUCTION BAR ---
        var prodBox = new VBoxContainer();
        prodBox.AddThemeConstantOverride("separation", 1);
        leftCol.AddChild(prodBox);

        var prodTitle = new Label { Text = $"PRODUCTION  {baseProd} per turn" };
        prodTitle.AddThemeFontSizeOverride("font_size", 11);
        prodTitle.AddThemeColorOverride("font_color", new Color(0.12f, 0.45f, 0.75f)); // Blue for production
        prodBox.AddChild(prodTitle);

        var prodIcons = new Label { Text = string.Concat(Enumerable.Repeat("🛡️", Math.Min(15, baseProd))) + (baseProd > 15 ? $" (+{baseProd - 15})" : "") };
        prodIcons.AddThemeFontSizeOverride("font_size", 12);
        prodBox.AddChild(prodIcons);

        // --- 2. FOOD BAR ---
        var foodBox = new VBoxContainer();
        foodBox.AddThemeConstantOverride("separation", 1);
        leftCol.AddChild(foodBox);

        string growthStatus = netFood > 0 ? $"+{netFood}/turn" : netFood < 0 ? $"{netFood}/turn" : "Zero Growth";
        var foodTitle = new Label { Text = $"FOOD  {baseFood} per turn ({growthStatus})" };
        foodTitle.AddThemeFontSizeOverride("font_size", 11);
        foodTitle.AddThemeColorOverride("font_color", new Color(0.65f, 0.45f, 0.05f)); // Golden brown for food
        foodBox.AddChild(foodTitle);

        // Draw green wheat for net food, red apples/icons for eaten food, or simple splits
        string foodText = string.Concat(Enumerable.Repeat("🌾", Math.Min(12, baseFood)));
        if (foodConsumption > 0)
        {
            int consumed = Math.Min(10, foodConsumption);
            foodText = string.Concat(Enumerable.Repeat("🟢", Math.Max(0, baseFood - foodConsumption))) + " " + string.Concat(Enumerable.Repeat("🔴", consumed));
        }
        var foodIcons = new Label { Text = foodText };
        foodIcons.AddThemeFontSizeOverride("font_size", 10);
        foodBox.AddChild(foodIcons);

        // --- 3. COMMERCE BAR ---
        var commBox = new VBoxContainer();
        commBox.AddThemeConstantOverride("separation", 1);
        leftCol.AddChild(commBox);

        var commTitle = new Label { Text = $"COMMERCE  {baseComm} per turn" };
        commTitle.AddThemeFontSizeOverride("font_size", 11);
        commTitle.AddThemeColorOverride("font_color", new Color(0.15f, 0.45f, 0.15f)); // Green for commerce
        commBox.AddChild(commTitle);

        // Split gold and science according to tax rate
        int goldPart = (baseComm * sim.PlayerTaxRate) / 100;
        int sciPart = baseComm - goldPart;

        var commSplit = new HBoxContainer();
        commBox.AddChild(commSplit);

        var goldPartLabel = new Label { Text = $"💰 {goldPart} Gold ({sim.PlayerTaxRate}%)  " };
        goldPartLabel.AddThemeFontSizeOverride("font_size", 10);
        goldPartLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.45f, 0.05f));
        commSplit.AddChild(goldPartLabel);

        var sciPartLabel = new Label { Text = $"🧪 {sciPart} Science ({100 - sim.PlayerTaxRate}%)" };
        sciPartLabel.AddThemeFontSizeOverride("font_size", 10);
        sciPartLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.45f, 0.55f));
        commSplit.AddChild(sciPartLabel);

        // --- 4. GRANARY PREVIEW ---
        var granBox = new VBoxContainer();
        granBox.AddThemeConstantOverride("separation", 2);
        leftCol.AddChild(granBox);

        var granTitle = new Label { Text = $"GRANARY  ({city.StoredFood}/{city.FoodNeededForGrowth})" };
        granTitle.AddThemeFontSizeOverride("font_size", 10);
        granTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.25f, 0.15f));
        granBox.AddChild(granTitle);

        // Draw a row of boxes or wheat representing food stored
        var granHBox = new HBoxContainer();
        granBox.AddChild(granHBox);

        for (int i = 0; i < city.FoodNeededForGrowth; i += 2)
        {
            string symbol = (i < city.StoredFood) ? "🌾" : "░";
            var item = new Label { Text = symbol };
            item.AddThemeFontSizeOverride("font_size", 11);
            granHBox.AddChild(item);
        }

        // ==========================================
        // RIGHT COLUMN: Active Project Display
        // ==========================================
        var rightCol = new VBoxContainer();
        rightCol.CustomMinimumSize = new Vector2(170, 0);
        rightCol.Alignment = BoxContainer.AlignmentMode.Center;
        rightCol.AddThemeConstantOverride("separation", 4);
        hsplit.AddChild(rightCol);

        var projFrameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.9f, 0.85f, 0.75f, 0.8f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            BorderColor = new Color(0.45f, 0.38f, 0.28f, 0.5f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        var frame = new PanelContainer();
        frame.AddThemeStyleboxOverride("panel", projFrameStyle);
        rightCol.AddChild(frame);

        var frameVBox = new VBoxContainer();
        frameVBox.AddThemeConstantOverride("separation", 4);
        frameVBox.Alignment = BoxContainer.AlignmentMode.Center;
        frame.AddChild(frameVBox);

        var activeTitle = new Label { Text = "BUILDING PROJECT" };
        activeTitle.AddThemeFontSizeOverride("font_size", 10);
        activeTitle.AddThemeColorOverride("font_color", new Color(0.4f, 0.35f, 0.25f));
        activeTitle.HorizontalAlignment = HorizontalAlignment.Center;
        frameVBox.AddChild(activeTitle);

        if (city.CurrentProject == ProductionProject.None)
        {
            var idleLabel = new Label { Text = "IDLE" };
            idleLabel.AddThemeFontSizeOverride("font_size", 16);
            idleLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
            idleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            frameVBox.AddChild(idleLabel);

            var idleDesc = new Label { Text = "Select Project to Begin" };
            idleDesc.AddThemeFontSizeOverride("font_size", 9);
            idleDesc.HorizontalAlignment = HorizontalAlignment.Center;
            frameVBox.AddChild(idleDesc);
        }
        else
        {
            var projName = new Label { Text = city.CurrentProject.ToString().ToUpper() };
            projName.AddThemeFontSizeOverride("font_size", 13);
            projName.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.12f));
            projName.HorizontalAlignment = HorizontalAlignment.Center;
            frameVBox.AddChild(projName);

            // Project Icon/Graphic
            var graphic = new Label { Text = getProjectEmoji(city.CurrentProject) };
            graphic.AddThemeFontSizeOverride("font_size", 34);
            graphic.HorizontalAlignment = HorizontalAlignment.Center;
            frameVBox.AddChild(graphic);

            int cost = city.GetProjectCost(city.CurrentProject);
            int progress = city.CurrentProductionProgress;
            int turnsLeft = baseProd > 0 ? (int)Math.Ceiling((double)(cost - progress) / baseProd) : 9999;

            var timeLabel = new Label { Text = turnsLeft == 9999 ? "Never Completes" : $"Complete in {turnsLeft} turn{(turnsLeft == 1 ? "" : "s")}" };
            timeLabel.AddThemeFontSizeOverride("font_size", 10);
            timeLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.4f, 0.15f));
            timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            frameVBox.AddChild(timeLabel);

            // Progress bar
            var bar = new ProgressBar();
            bar.MinValue = 0;
            bar.MaxValue = cost;
            bar.Value = progress;
            bar.CustomMinimumSize = new Vector2(0, 14);
            bar.ShowPercentage = true;
            frameVBox.AddChild(bar);

            var progressLabel = new Label { Text = $"{progress}/{cost} Shields" };
            progressLabel.AddThemeFontSizeOverride("font_size", 9);
            progressLabel.HorizontalAlignment = HorizontalAlignment.Center;
            frameVBox.AddChild(progressLabel);
        }
    }

    private string getProjectEmoji(ProductionProject proj)
    {
        string name = proj.ToString().ToLower();
        if (name.Contains("settler")) return "🧑‍🤝‍🧑";
        if (name.Contains("worker")) return "⚒️";
        if (name.Contains("warrior")) return "⚔️";
        if (name.Contains("archer")) return "🏹";
        if (name.Contains("explorer")) return "🧭";
        if (name.Contains("granary")) return "🌾";
        if (name.Contains("barracks")) return "⚔️";
        if (name.Contains("temple")) return "🏛️";
        if (name.Contains("library")) return "📖";
        if (name.Contains("colosseum")) return "🏟️";
        if (name.Contains("aqueduct")) return "🚰";
        if (name.Contains("cathedral")) return "⛪";
        if (name.Contains("university")) return "🎓";
        if (name.Contains("palace")) return "🏰";
        if (name.Contains("epic")) return "🛡️";
        if (name.Contains("academy")) return "🎖️";
        if (name.Contains("pentagon")) return "🛑";
        return "🏢";
    }
}
