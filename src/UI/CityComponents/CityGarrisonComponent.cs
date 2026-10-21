using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI.CityComponents;

/// <summary>
/// Civ3-authentic Garrison & Stationed Units component.
/// Displays defensive garrison, unit veterancy, health bars, fortify states, and Military Police pacification.
/// </summary>
public partial class CityGarrisonComponent : PanelContainer
{
    public CityGarrisonComponent(City city) : this(city, null) { }

    public CityGarrisonComponent(City city, GameSimulation? sim)
    {
        // Parchment styled container
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.82f, 0.95f),
            BorderColor = new Color(0.5f, 0.4f, 0.3f),
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

        CustomMinimumSize = new Vector2(250, 200);
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
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

        // --- 1. HEADER & MILITARY POLICE STRIP ---
        var headerVBox = new VBoxContainer();
        headerVBox.AddThemeConstantOverride("separation", 2);
        vbox.AddChild(headerVBox);

        var headerHBox = new HBoxContainer();
        headerVBox.AddChild(headerHBox);

        var titleLabel = new Label { Text = "🛡️ GARRISON & DEFENSE" };
        titleLabel.AddThemeFontSizeOverride("font_size", 11);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.15f, 0.1f));
        headerHBox.AddChild(titleLabel);

        // Find stationed units
        List<Unit> stationedUnits = sim != null
            ? sim.Units.Where(u => u.X == city.X && u.Y == city.Y && u.Faction == city.Faction).ToList()
            : new List<Unit>();

        // Military Police calculation
        int maxMp = 2; // Default Despotism
        if (sim != null)
        {
            var govType = city.Faction == Faction.Player ? sim.PlayerGovernment : sim.AiGovernment;
            var gov = Government.Get(govType);
            maxMp = gov.MaxMilitaryPolice;
        }

        int combatUnitsCount = stationedUnits.Count(u => u.AttackStrength > 0 || u.DefenseStrength > 0);
        int activeMp = Math.Min(maxMp, combatUnitsCount);

        var mpLabel = new Label
        {
            Text = $"👮 MP: {activeMp}/{maxMp} active",
            HorizontalAlignment = HorizontalAlignment.Right,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        mpLabel.AddThemeFontSizeOverride("font_size", 10);
        mpLabel.AddThemeColorOverride("font_color", activeMp > 0 ? new Color(0.15f, 0.45f, 0.15f) : new Color(0.55f, 0.45f, 0.35f));
        headerHBox.AddChild(mpLabel);

        // Defense bonuses strip (Walls, Coastal Fortress, Civil Defense)
        var bonuses = new List<string>();
        if (city.Buildings.Any(b => b.Id == "walls")) bonuses.Add("Walls (+50% Def)");
        if (city.Buildings.Any(b => b.Id == "coastal_fortress")) bonuses.Add("Coastal Fort (+50% Sea)");
        if (city.Buildings.Any(b => b.Id == "civil_defense")) bonuses.Add("Civil Def (+50%)");
        if (city.Buildings.Any(b => b.Id == "sam_missile_battery")) bonuses.Add("SAM Battery");

        if (bonuses.Count > 0)
        {
            var bonusLabel = new Label
            {
                Text = string.Join(" • ", bonuses),
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            bonusLabel.AddThemeFontSizeOverride("font_size", 9);
            bonusLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.35f, 0.55f));
            headerVBox.AddChild(bonusLabel);
        }

        vbox.AddChild(new HSeparator());

        // --- 2. STATIONED UNITS LIST ---
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        vbox.AddChild(scroll);

        var listVBox = new VBoxContainer();
        listVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        listVBox.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(listVBox);

        if (stationedUnits.Count == 0)
        {
            var undefendedPanel = new PanelContainer();
            var undefendedStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.95f, 0.85f, 0.85f, 0.8f),
                BorderColor = new Color(0.8f, 0.3f, 0.3f),
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
            };
            undefendedPanel.AddThemeStyleboxOverride("panel", undefendedStyle);

            var uMargin = new MarginContainer();
            uMargin.AddThemeConstantOverride("margin_left", 6);
            uMargin.AddThemeConstantOverride("margin_right", 6);
            uMargin.AddThemeConstantOverride("margin_top", 6);
            uMargin.AddThemeConstantOverride("margin_bottom", 6);
            undefendedPanel.AddChild(uMargin);

            var warnText = new Label
            {
                Text = "⚠️ UNDEFENDED CITY!\nNo military units stationed.\nVulnerable to immediate capture.",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            warnText.AddThemeFontSizeOverride("font_size", 10);
            warnText.AddThemeColorOverride("font_color", new Color(0.75f, 0.15f, 0.15f));
            uMargin.AddChild(warnText);

            listVBox.AddChild(undefendedPanel);
        }
        else
        {
            int mpCounted = 0;
            foreach (var unit in stationedUnits)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 6);

                string icon = unit.Type switch
                {
                    UnitType.Warrior or UnitType.Swordsman or UnitType.MedievalInfantry or UnitType.Legionary or UnitType.Immortals => "⚔️",
                    UnitType.Spearman or UnitType.Pikeman or UnitType.Musketman or UnitType.Rifleman or UnitType.Infantry or UnitType.MechInfantry or UnitType.Hoplite => "🛡️",
                    UnitType.Archer or UnitType.Bowman or UnitType.Longbowman => "🏹",
                    UnitType.Horseman or UnitType.Knight or UnitType.Cavalry or UnitType.AnsarWarrior or UnitType.Sipahi or UnitType.Cossack => "🐎",
                    UnitType.Tank or UnitType.Panzer or UnitType.ModernArmor => "🚜",
                    UnitType.Catapult or UnitType.Trebuchet or UnitType.Cannon or UnitType.Artillery or UnitType.RadarArtillery => "💥",
                    UnitType.Worker => "⚒️",
                    UnitType.Settler => "🧑‍🤝‍🧑",
                    UnitType.Explorer or UnitType.ChasquiScout => "🧭",
                    UnitType.GreatLeader => "👑",
                    _ => "💂"
                };

                var iconLabel = new Label { Text = icon };
                iconLabel.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(iconLabel);

                var infoVBox = new VBoxContainer();
                infoVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                infoVBox.AddThemeConstantOverride("separation", 1);
                row.AddChild(infoVBox);

                // Line 1: Unit Name + Veterancy Badge + Status
                var line1 = new HBoxContainer();
                line1.AddThemeConstantOverride("separation", 4);
                infoVBox.AddChild(line1);

                var nameLabel = new Label { Text = unit.Type.ToString() };
                nameLabel.AddThemeFontSizeOverride("font_size", 10);
                nameLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.1f, 0.08f));
                line1.AddChild(nameLabel);

                // Veterancy badge
                string expBadge = unit.Experience switch
                {
                    ExperienceLevel.Conscript => "[Conscript]",
                    ExperienceLevel.Regular => "[Regular]",
                    ExperienceLevel.Veteran => "[Veteran]",
                    ExperienceLevel.Elite => "[Elite ⭐]",
                    _ => ""
                };
                Color expColor = unit.Experience switch
                {
                    ExperienceLevel.Conscript => new Color(0.6f, 0.45f, 0.3f),
                    ExperienceLevel.Regular => new Color(0.2f, 0.45f, 0.7f),
                    ExperienceLevel.Veteran => new Color(0.45f, 0.2f, 0.65f),
                    ExperienceLevel.Elite => new Color(0.85f, 0.65f, 0.1f),
                    _ => Colors.White
                };
                var badgeLabel = new Label { Text = expBadge };
                badgeLabel.AddThemeFontSizeOverride("font_size", 9);
                badgeLabel.AddThemeColorOverride("font_color", expColor);
                line1.AddChild(badgeLabel);

                // Status tag
                if (unit.IsFortified)
                {
                    var fortLabel = new Label { Text = "[🛡️ Fortified]" };
                    fortLabel.AddThemeFontSizeOverride("font_size", 9);
                    fortLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.5f, 0.2f));
                    line1.AddChild(fortLabel);
                }
                else if (unit.IsSentry)
                {
                    var sentryLabel = new Label { Text = "[👁️ Sentry]" };
                    sentryLabel.AddThemeFontSizeOverride("font_size", 9);
                    sentryLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.4f, 0.7f));
                    line1.AddChild(sentryLabel);
                }
                else if (unit.IsSleeping)
                {
                    var sleepLabel = new Label { Text = "[💤 Sleep]" };
                    sleepLabel.AddThemeFontSizeOverride("font_size", 9);
                    sleepLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                    line1.AddChild(sleepLabel);
                }

                // MP Badge
                if (unit.AttackStrength > 0 || unit.DefenseStrength > 0)
                {
                    if (mpCounted < maxMp)
                    {
                        mpCounted++;
                        var mpTag = new Label { Text = "[👮 MP]" };
                        mpTag.AddThemeFontSizeOverride("font_size", 9);
                        mpTag.AddThemeColorOverride("font_color", new Color(0.15f, 0.55f, 0.15f));
                        line1.AddChild(mpTag);
                    }
                }

                // Line 2: Health representation
                var line2 = new HBoxContainer();
                line2.AddThemeConstantOverride("separation", 6);
                infoVBox.AddChild(line2);

                float healthPct = (float)unit.Health / Math.Max(1, unit.MaxHealth);
                Color hpColor = healthPct > 0.6f ? new Color(0.2f, 0.7f, 0.2f) : healthPct > 0.3f ? new Color(0.8f, 0.7f, 0.15f) : new Color(0.85f, 0.2f, 0.2f);

                var hpLabel = new Label { Text = $"{unit.Health} / {unit.MaxHealth} HP" };
                hpLabel.AddThemeFontSizeOverride("font_size", 9);
                hpLabel.AddThemeColorOverride("font_color", hpColor);
                line2.AddChild(hpLabel);

                var hpBar = new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = unit.MaxHealth,
                    Value = unit.Health,
                    ShowPercentage = false,
                    CustomMinimumSize = new Vector2(70, 8),
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
                };
                line2.AddChild(hpBar);

                listVBox.AddChild(row);
            }
        }
    }
}
