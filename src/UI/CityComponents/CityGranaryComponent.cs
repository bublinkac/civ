using System;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI.CityComponents;

/// <summary>
/// Civ3-authentic Food Storage & Granary visualization component.
/// Displays accumulated food, growth thresholds, granary retention thresholds, and growth/starvation ETA.
/// </summary>
public partial class CityGranaryComponent : PanelContainer
{
    public CityGranaryComponent(City city) : this(city, null) { }

    public CityGranaryComponent(City city, GameSimulation? sim)
    {
        // Parchment styled container
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.96f, 0.93f, 0.84f, 0.95f),
            BorderColor = new Color(0.55f, 0.45f, 0.3f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        AddThemeStyleboxOverride("panel", style);

        CustomMinimumSize = new Vector2(500, 95);
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        // --- 1. TOP HEADER STRIP ---
        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(headerHBox);

        var titleLabel = new Label { Text = "🌾 FOOD STORAGE & GRANARY" };
        titleLabel.AddThemeFontSizeOverride("font_size", 12);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.05f));
        headerHBox.AddChild(titleLabel);

        int neededFood = city.FoodNeededForGrowth;
        int storedFood = Math.Clamp(city.StoredFood, 0, neededFood);
        int netFood = city.LastTurnNetFood;

        // Check if city has a granary or Pyramids (which gives Granary effect to all cities)
        bool hasGranary = city.Buildings.Any(b => b.Id == "granary") ||
                          (sim != null && sim.CompletedWonderIds.Contains("pyramids"));

        var granaryBadge = new Label();
        if (hasGranary)
        {
            granaryBadge.Text = $"✨ Granary Active (50% [{neededFood / 2} 🌾] Retained on Growth)";
            granaryBadge.AddThemeColorOverride("font_color", new Color(0.15f, 0.5f, 0.15f));
        }
        else
        {
            granaryBadge.Text = "▫️ No Granary (All Food Consumed on Growth)";
            granaryBadge.AddThemeColorOverride("font_color", new Color(0.55f, 0.45f, 0.35f));
        }
        granaryBadge.AddThemeFontSizeOverride("font_size", 11);
        granaryBadge.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        granaryBadge.HorizontalAlignment = HorizontalAlignment.Right;
        headerHBox.AddChild(granaryBadge);

        // --- 2. STATS & ETA ROW ---
        var statsHBox = new HBoxContainer();
        statsHBox.AddThemeConstantOverride("separation", 14);
        vbox.AddChild(statsHBox);

        // Stored Food Ratio
        var ratioLabel = new Label { Text = $"Stored: {city.StoredFood} / {neededFood} Food" };
        ratioLabel.AddThemeFontSizeOverride("font_size", 11);
        ratioLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.1f));
        statsHBox.AddChild(ratioLabel);

        // Net Food
        string netStr = netFood > 0 ? $"+{netFood} / turn" : netFood < 0 ? $"{netFood} / turn" : "0 / turn";
        Color netColor = netFood > 0 ? new Color(0.15f, 0.5f, 0.15f) : netFood < 0 ? new Color(0.8f, 0.15f, 0.15f) : new Color(0.5f, 0.5f, 0.2f);
        var netLabel = new Label { Text = $"Net: {netStr}" };
        netLabel.AddThemeFontSizeOverride("font_size", 11);
        netLabel.AddThemeColorOverride("font_color", netColor);
        statsHBox.AddChild(netLabel);

        // Growth / Starvation ETA
        string etaText;
        Color etaColor;
        if (netFood > 0)
        {
            int turnsToGrow = (int)Math.Ceiling((double)(neededFood - city.StoredFood) / netFood);
            if (turnsToGrow <= 0) turnsToGrow = 1;

            // Check growth caps (Fresh water / Aqueduct at size 6, Hospital at size 12)
            bool isCoastalOrFloodplain = false;
            if (sim != null)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var t = sim.Map.GetTile(city.X + dx, city.Y + dy);
                        if (t != null && (t.Terrain.Id == "coast" || t.Terrain.Id == "floodplains" || t.HasRiver))
                        {
                            isCoastalOrFloodplain = true;
                            break;
                        }
                    }
                    if (isCoastalOrFloodplain) break;
                }
            }

            bool cappedAt6 = city.Population >= 6 && !city.Buildings.Any(b => b.Id == "aqueduct") && !isCoastalOrFloodplain;
            bool cappedAt12 = city.Population >= 12 && !city.Buildings.Any(b => b.Id == "hospital");

            if (cappedAt6)
            {
                etaText = "🚫 Growth Capped at Pop 6 (Requires Aqueduct / River)";
                etaColor = new Color(0.75f, 0.35f, 0.05f);
            }
            else if (cappedAt12)
            {
                etaText = "🚫 Growth Capped at Pop 12 (Requires Hospital)";
                etaColor = new Color(0.75f, 0.35f, 0.05f);
            }
            else
            {
                etaText = $"⏱️ Growth in: {turnsToGrow} turn{(turnsToGrow == 1 ? "" : "s")}";
                etaColor = new Color(0.1f, 0.4f, 0.15f);
            }
        }
        else if (netFood < 0)
        {
            int deficit = -netFood;
            int turnsToStarve = Math.Max(1, (city.StoredFood / deficit) + 1);
            etaText = $"⚠️ STARVATION! Population loss in {turnsToStarve} turn{(turnsToStarve == 1 ? "" : "s")}";
            etaColor = new Color(0.85f, 0.1f, 0.1f);
        }
        else
        {
            etaText = "⚖️ Stagnant (Zero Growth)";
            etaColor = new Color(0.45f, 0.45f, 0.45f);
        }

        var etaLabel = new Label { Text = etaText };
        etaLabel.AddThemeFontSizeOverride("font_size", 11);
        etaLabel.AddThemeColorOverride("font_color", etaColor);
        etaLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        etaLabel.HorizontalAlignment = HorizontalAlignment.Right;
        statsHBox.AddChild(etaLabel);

        // --- 3. CIV3 FOOD STORAGE BOX (BINS GRID) ---
        var foodBoxContainer = new PanelContainer();
        var boxStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.88f, 0.85f, 0.76f, 0.9f),
            BorderColor = new Color(0.6f, 0.5f, 0.4f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        foodBoxContainer.AddThemeStyleboxOverride("panel", boxStyle);
        vbox.AddChild(foodBoxContainer);

        var boxMargin = new MarginContainer();
        boxMargin.AddThemeConstantOverride("margin_left", 6);
        boxMargin.AddThemeConstantOverride("margin_right", 6);
        boxMargin.AddThemeConstantOverride("margin_top", 4);
        boxMargin.AddThemeConstantOverride("margin_bottom", 4);
        foodBoxContainer.AddChild(boxMargin);

        var binsHBox = new HBoxContainer();
        binsHBox.AddThemeConstantOverride("separation", 3);
        boxMargin.AddChild(binsHBox);

        // Render discrete food bins (up to neededFood, scaled or 1:1)
        int totalBins = Math.Min(30, neededFood);
        int filledBins = (int)Math.Round((double)storedFood / neededFood * totalBins);
        int granaryThresholdBin = hasGranary ? totalBins / 2 : -1;

        for (int i = 0; i < totalBins; i++)
        {
            // Insert divider marker for Granary 50% retention
            if (i == granaryThresholdBin)
            {
                var divider = new ColorRect
                {
                    CustomMinimumSize = new Vector2(3, 20),
                    Color = new Color(0.8f, 0.2f, 0.2f, 0.85f) // Red granary line
                };
                divider.TooltipText = $"Granary Threshold ({neededFood / 2} Food preserved on growth)";
                binsHBox.AddChild(divider);
            }

            var bin = new PanelContainer();
            bin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            bin.CustomMinimumSize = new Vector2(10, 18);

            bool isFilled = i < filledBins;
            bool inPreservedZone = hasGranary && i < granaryThresholdBin;

            Color binBg = isFilled
                ? (inPreservedZone ? new Color(0.95f, 0.78f, 0.2f) : new Color(0.35f, 0.75f, 0.25f))
                : (inPreservedZone ? new Color(0.92f, 0.86f, 0.7f, 0.5f) : new Color(0.8f, 0.77f, 0.7f, 0.4f));

            var binStyle = new StyleBoxFlat
            {
                BgColor = binBg,
                BorderColor = isFilled ? new Color(0.45f, 0.35f, 0.15f) : new Color(0.65f, 0.6f, 0.55f),
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2
            };
            bin.AddThemeStyleboxOverride("panel", binStyle);

            var binLabel = new Label
            {
                Text = isFilled ? "🌾" : "·",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            binLabel.AddThemeFontSizeOverride("font_size", 9);
            bin.AddChild(binLabel);

            binsHBox.AddChild(bin);
        }
    }
}
