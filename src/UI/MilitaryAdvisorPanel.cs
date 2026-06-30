using System;
using Godot;
using CivGame.Core;
using System.Linq;
using System.Collections.Generic;

namespace CivGame.UI;

/// <summary>
/// Civ3-authentic Military Advisor (F3).
/// Two view modes: "View by Unit Type" and "View by City".
/// Shows all individual units with HP, position, status, attack/defense.
/// </summary>
public partial class MilitaryAdvisorPanel : PanelContainer
{
    private GameSimulation _sim;
    private VBoxContainer _contentContainer;
    private Button _viewByUnitBtn;
    private Button _viewByCityBtn;
    private bool _viewByCity = false;

    public MilitaryAdvisorPanel(GameSimulation sim)
    {
        _sim = sim;
        Name = "MilitaryAdvisorPanel";

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        // Civ3 parchment background
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.91f, 0.87f, 0.78f, 1.0f),
            BorderWidthTop = 5, BorderWidthBottom = 5, BorderWidthLeft = 5, BorderWidthRight = 5,
            BorderColor = new Color(0.55f, 0.4f, 0.25f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        AddThemeStyleboxOverride("panel", styleBox);

        var outerMargin = new MarginContainer();
        outerMargin.AddThemeConstantOverride("margin_top", 12);
        outerMargin.AddThemeConstantOverride("margin_bottom", 12);
        outerMargin.AddThemeConstantOverride("margin_left", 16);
        outerMargin.AddThemeConstantOverride("margin_right", 16);
        outerMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        outerMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 8);

        // ═══════════════════════════════════════════════
        // HEADER BAR
        // ═══════════════════════════════════════════════
        var headerPanel = new PanelContainer();
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.45f, 0.2f, 0.15f),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6
        };
        headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
        var headerHBox = new HBoxContainer();
        var titleLabel = new Label
        {
            Text = "M I L I T A R Y   A D V I S O R",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 0.75f));

        var closeBtn = new Button { Text = "\u2716", Flat = true, CustomMinimumSize = new Vector2(36, 36) };
        closeBtn.AddThemeColorOverride("font_color", new Color(0.95f, 0.8f, 0.6f));
        closeBtn.AddThemeFontSizeOverride("font_size", 20);
        closeBtn.Pressed += () => QueueFree();

        headerHBox.AddChild(titleLabel);
        headerHBox.AddChild(closeBtn);
        headerPanel.AddChild(headerHBox);
        mainVBox.AddChild(headerPanel);

        // ═══════════════════════════════════════════════
        // ADVISOR ROW: portrait + speech + stats
        // ═══════════════════════════════════════════════
        int playerUnitCount = _sim.Units.Count(u => u.Faction == Faction.Player);
        int aiUnitCount = _sim.Units.Count(u => u.Faction == Faction.AiRival);
        int playerMilitary = _sim.Units.Count(u => u.Faction == Faction.Player && u.AttackStrength > 0);
        int totalAttack = _sim.Units.Where(u => u.Faction == Faction.Player).Sum(u => u.AttackStrength);
        int totalDefense = _sim.Units.Where(u => u.Faction == Faction.Player).Sum(u => u.DefenseStrength);

        var advisorRow = new HBoxContainer();
        advisorRow.AddThemeConstantOverride("separation", 12);

        // Portrait
        var portraitPanel = new PanelContainer { CustomMinimumSize = new Vector2(80, 90) };
        var portraitStyle = new StyleBoxFlat { BgColor = new Color(0.4f, 0.2f, 0.15f, 0.9f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };
        portraitPanel.AddThemeStyleboxOverride("panel", portraitStyle);
        var portraitLabel = new Label { Text = "\u2694\ufe0f", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        portraitLabel.AddThemeFontSizeOverride("font_size", 48);
        portraitPanel.AddChild(portraitLabel);
        advisorRow.AddChild(portraitPanel);

        // Speech bubble
        var speechPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var speechStyle = new StyleBoxFlat
        {
            BgColor = new Color(1.0f, 1.0f, 0.95f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.5f, 0.3f, 0.2f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        speechPanel.AddThemeStyleboxOverride("panel", speechStyle);

        string message = GenerateMilitaryAdvice(playerUnitCount, aiUnitCount, playerMilitary);
        var speechLabel = new Label { Text = message, AutowrapMode = TextServer.AutowrapMode.Word };
        speechLabel.AddThemeFontSizeOverride("font_size", 13);
        speechLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        speechPanel.AddChild(speechLabel);
        advisorRow.AddChild(speechPanel);

        // Stats column
        var statsVBox = new VBoxContainer { CustomMinimumSize = new Vector2(160, 0) };
        statsVBox.AddThemeConstantOverride("separation", 2);
        AddStatLine(statsVBox, "Total Units:", playerUnitCount.ToString(), new Color(0.1f, 0.1f, 0.5f));
        AddStatLine(statsVBox, "Military:", playerMilitary.ToString(), new Color(0.5f, 0.15f, 0.15f));
        AddStatLine(statsVBox, "Total ATK:", totalAttack.ToString(), new Color(0.6f, 0.2f, 0.1f));
        AddStatLine(statsVBox, "Total DEF:", totalDefense.ToString(), new Color(0.1f, 0.3f, 0.6f));
        AddStatLine(statsVBox, "War Status:", _sim.IsAtWarWithAi ? "AT WAR" : "Peace",
            _sim.IsAtWarWithAi ? new Color(0.7f, 0.1f, 0.1f) : new Color(0.1f, 0.5f, 0.1f));
        advisorRow.AddChild(statsVBox);

        mainVBox.AddChild(advisorRow);

        // ═══════════════════════════════════════════════
        // VIEW TOGGLE BUTTONS
        // ═══════════════════════════════════════════════
        var toggleRow = new HBoxContainer();
        toggleRow.AddThemeConstantOverride("separation", 10);
        toggleRow.Alignment = BoxContainer.AlignmentMode.Center;

        _viewByUnitBtn = CreateToggleButton("View by Unit Type", true);
        _viewByUnitBtn.Pressed += () => SwitchView(false);
        toggleRow.AddChild(_viewByUnitBtn);

        _viewByCityBtn = CreateToggleButton("View by City", false);
        _viewByCityBtn.Pressed += () => SwitchView(true);
        toggleRow.AddChild(_viewByCityBtn);

        // Enemy info label
        toggleRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var enemyLabel = new Label { Text = $"Enemy: {_sim.AiCiv.Name} ({aiUnitCount} units est.)" };
        enemyLabel.AddThemeFontSizeOverride("font_size", 12);
        enemyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.2f, 0.2f));
        toggleRow.AddChild(enemyLabel);

        mainVBox.AddChild(toggleRow);

        // ═══════════════════════════════════════════════
        // CONTENT AREA: Map (left) + Unit List (right)
        // ═══════════════════════════════════════════════
        var contentSplit = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        contentSplit.AddThemeConstantOverride("separation", 10);

        // Advisor Map showing all units
        var mapOptions = new AdvisorMapOptions
        {
            MinSize = new Vector2(260, 180),
            ShowUnits = true,
            ShowCities = true,
            ShowTerritory = true,
            ShowCityNames = true,
            UnitFactionFilter = null,
            CityDotScale = 1.8f,
            UnitDotScale = 1.2f
        };
        var advisorMap = new AdvisorMapPanel(_sim, mapOptions);
        advisorMap.SizeFlagsVertical = SizeFlags.ExpandFill;
        contentSplit.AddChild(advisorMap);

        _contentContainer = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        contentSplit.AddChild(_contentContainer);
        mainVBox.AddChild(contentSplit);

        BuildUnitTypeView();

        // ═══════════════════════════════════════════════
        // BOTTOM BAR
        // ═══════════════════════════════════════════════
        var bottomPanel = new PanelContainer();
        var bottomStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.45f, 0.2f, 0.15f, 0.9f),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        bottomPanel.AddThemeStyleboxOverride("panel", bottomStyle);

        var bottomHBox = new HBoxContainer();
        bottomHBox.AddThemeConstantOverride("separation", 20);

        var armyStr = new Label { Text = $"Army Strength: {totalAttack + totalDefense}", VerticalAlignment = VerticalAlignment.Center };
        armyStr.AddThemeFontSizeOverride("font_size", 13);
        armyStr.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        bottomHBox.AddChild(armyStr);

        var supportLabel = new Label { Text = $"Support: {playerUnitCount} gold/turn", VerticalAlignment = VerticalAlignment.Center };
        supportLabel.AddThemeFontSizeOverride("font_size", 13);
        supportLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.7f, 0.7f));
        bottomHBox.AddChild(supportLabel);

        bottomHBox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var footerCloseBtn = new Button { Text = "Close", CustomMinimumSize = new Vector2(80, 28) };
        var closeBtnStyle2 = new StyleBoxFlat { BgColor = new Color(0.6f, 0.2f, 0.1f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        footerCloseBtn.AddThemeStyleboxOverride("normal", closeBtnStyle2);
        footerCloseBtn.Pressed += () => QueueFree();
        bottomHBox.AddChild(footerCloseBtn);

        bottomPanel.AddChild(bottomHBox);
        mainVBox.AddChild(bottomPanel);

        outerMargin.AddChild(mainVBox);
        AddChild(outerMargin);
    }

    private void SwitchView(bool byCity)
    {
        _viewByCity = byCity;
        UpdateToggleStyles();

        // Clear content
        foreach (var child in _contentContainer.GetChildren())
            child.QueueFree();

        if (_viewByCity)
            BuildCityView();
        else
            BuildUnitTypeView();
    }

    private void UpdateToggleStyles()
    {
        var activeStyle = new StyleBoxFlat { BgColor = new Color(0.55f, 0.25f, 0.15f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        var inactiveStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.25f, 0.2f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        _viewByUnitBtn.AddThemeStyleboxOverride("normal", _viewByCity ? inactiveStyle : activeStyle);
        _viewByCityBtn.AddThemeStyleboxOverride("normal", _viewByCity ? activeStyle : inactiveStyle);
    }

    private void BuildUnitTypeView()
    {
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var listVBox = new VBoxContainer();
        listVBox.AddThemeConstantOverride("separation", 4);
        listVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var playerUnits = _sim.Units.Where(u => u.Faction == Faction.Player).ToList();
        var grouped = playerUnits.GroupBy(u => u.Type).OrderBy(g => g.Key.ToString());

        foreach (var group in grouped)
        {
            // Group header
            var groupHeader = new PanelContainer();
            var ghStyle = new StyleBoxFlat { BgColor = new Color(0.8f, 0.75f, 0.65f), ContentMarginLeft = 10, ContentMarginTop = 4, ContentMarginBottom = 4 };
            groupHeader.AddThemeStyleboxOverride("panel", ghStyle);
            var ghLabel = new Label { Text = $"{group.Key} ({group.Count()})" };
            ghLabel.AddThemeFontSizeOverride("font_size", 15);
            ghLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.1f, 0.05f));
            groupHeader.AddChild(ghLabel);
            listVBox.AddChild(groupHeader);

            // Individual units
            bool alt = false;
            foreach (var unit in group.OrderByDescending(u => u.Health))
            {
                listVBox.AddChild(CreateUnitDetailRow(unit, alt));
                alt = !alt;
            }
        }

        scroll.AddChild(listVBox);
        _contentContainer.AddChild(scroll);
    }

    private void BuildCityView()
    {
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var listVBox = new VBoxContainer();
        listVBox.AddThemeConstantOverride("separation", 4);
        listVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var playerUnits = _sim.Units.Where(u => u.Faction == Faction.Player).ToList();
        var playerCities = _sim.Cities.Where(c => c.Faction == Faction.Player).ToList();

        // Assign units to nearest city
        var cityUnitMap = new Dictionary<string, List<Unit>>();
        var fieldUnits = new List<Unit>();

        foreach (var unit in playerUnits)
        {
            string? closestCityId = null;
            int closestDist = int.MaxValue;
            foreach (var city in playerCities)
            {
                int dist = Math.Abs(unit.X - city.X) + Math.Abs(unit.Y - city.Y);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestCityId = city.Id;
                }
            }

            if (closestCityId != null && closestDist <= 3)
            {
                if (!cityUnitMap.ContainsKey(closestCityId))
                    cityUnitMap[closestCityId] = new List<Unit>();
                cityUnitMap[closestCityId].Add(unit);
            }
            else
            {
                fieldUnits.Add(unit);
            }
        }

        // Show units per city
        foreach (var city in playerCities.OrderBy(c => c.Name))
        {
            var units = cityUnitMap.ContainsKey(city.Id) ? cityUnitMap[city.Id] : new List<Unit>();

            var cityHeader = new PanelContainer();
            var chStyle = new StyleBoxFlat { BgColor = new Color(0.75f, 0.7f, 0.6f), ContentMarginLeft = 10, ContentMarginTop = 4, ContentMarginBottom = 4 };
            cityHeader.AddThemeStyleboxOverride("panel", chStyle);
            var chHBox = new HBoxContainer();
            var chLabel = new Label { Text = $"{city.Name} ({city.X},{city.Y})", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            chLabel.AddThemeFontSizeOverride("font_size", 15);
            chLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.1f, 0.05f));
            var chCount = new Label { Text = $"{units.Count} units" };
            chCount.AddThemeFontSizeOverride("font_size", 13);
            chCount.AddThemeColorOverride("font_color", new Color(0.4f, 0.25f, 0.1f));
            chHBox.AddChild(chLabel);
            chHBox.AddChild(chCount);
            cityHeader.AddChild(chHBox);
            listVBox.AddChild(cityHeader);

            if (units.Count == 0)
            {
                var noUnits = new Label { Text = "    (no garrison)" };
                noUnits.AddThemeFontSizeOverride("font_size", 12);
                noUnits.AddThemeColorOverride("font_color", new Color(0.5f, 0.4f, 0.3f));
                listVBox.AddChild(noUnits);
            }
            else
            {
                bool alt = false;
                foreach (var unit in units.OrderBy(u => u.Type.ToString()))
                {
                    listVBox.AddChild(CreateUnitDetailRow(unit, alt));
                    alt = !alt;
                }
            }
        }

        // Field units (not near any city)
        if (fieldUnits.Count > 0)
        {
            var fieldHeader = new PanelContainer();
            var fhStyle = new StyleBoxFlat { BgColor = new Color(0.7f, 0.65f, 0.55f), ContentMarginLeft = 10, ContentMarginTop = 4, ContentMarginBottom = 4 };
            fieldHeader.AddThemeStyleboxOverride("panel", fhStyle);
            var fhLabel = new Label { Text = $"In the Field ({fieldUnits.Count} units)" };
            fhLabel.AddThemeFontSizeOverride("font_size", 15);
            fhLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.15f, 0.05f));
            fieldHeader.AddChild(fhLabel);
            listVBox.AddChild(fieldHeader);

            bool alt = false;
            foreach (var unit in fieldUnits.OrderBy(u => u.Type.ToString()))
            {
                listVBox.AddChild(CreateUnitDetailRow(unit, alt));
                alt = !alt;
            }
        }

        scroll.AddChild(listVBox);
        _contentContainer.AddChild(scroll);
    }

    private HBoxContainer CreateUnitDetailRow(Unit unit, bool altBg)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.CustomMinimumSize = new Vector2(0, 28);

        if (altBg)
            row.Modulate = new Color(0.97f, 0.95f, 0.9f);

        // Unit type name
        var nameLabel = new Label { Text = unit.Type.ToString(), CustomMinimumSize = new Vector2(100, 0) };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.08f, 0.05f));
        row.AddChild(nameLabel);

        // ATK/DEF
        var atkLabel = new Label { Text = $"A:{unit.AttackStrength}", CustomMinimumSize = new Vector2(40, 0) };
        atkLabel.AddThemeFontSizeOverride("font_size", 12);
        atkLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.2f, 0.1f));
        row.AddChild(atkLabel);

        var defLabel = new Label { Text = $"D:{unit.DefenseStrength}", CustomMinimumSize = new Vector2(40, 0) };
        defLabel.AddThemeFontSizeOverride("font_size", 12);
        defLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.3f, 0.6f));
        row.AddChild(defLabel);

        // HP Bar
        var hpContainer = new HBoxContainer { CustomMinimumSize = new Vector2(100, 0) };
        hpContainer.AddThemeConstantOverride("separation", 4);
        var hpBar = new ProgressBar { CustomMinimumSize = new Vector2(60, 14), MinValue = 0, MaxValue = unit.MaxHealth, Value = unit.Health };
        hpContainer.AddChild(hpBar);
        var hpText = new Label { Text = $"{unit.Health}%" };
        hpText.AddThemeFontSizeOverride("font_size", 11);
        hpText.AddThemeColorOverride("font_color", unit.Health >= 70 ? new Color(0.1f, 0.5f, 0.1f) : unit.Health >= 40 ? new Color(0.6f, 0.5f, 0.1f) : new Color(0.7f, 0.1f, 0.1f));
        hpContainer.AddChild(hpText);
        row.AddChild(hpContainer);

        // Movement
        var moveLabel = new Label { Text = $"M:{unit.RemainingMovement:F0}/{unit.MaxMovement:F0}", CustomMinimumSize = new Vector2(60, 0) };
        moveLabel.AddThemeFontSizeOverride("font_size", 11);
        moveLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.3f));
        row.AddChild(moveLabel);

        // Position
        var posLabel = new Label { Text = $"({unit.X},{unit.Y})", CustomMinimumSize = new Vector2(60, 0) };
        posLabel.AddThemeFontSizeOverride("font_size", 11);
        posLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.35f, 0.25f));
        row.AddChild(posLabel);

        // Status
        string status = "Active";
        Color statusColor = new Color(0.2f, 0.5f, 0.2f);
        if (unit.IsFortified) { status = "Fortified"; statusColor = new Color(0.2f, 0.3f, 0.6f); }
        else if (unit.IsSleeping) { status = "Sleeping"; statusColor = new Color(0.4f, 0.4f, 0.4f); }
        else if (unit.IsWorkerBuilding()) { status = "Building"; statusColor = new Color(0.5f, 0.4f, 0.1f); }
        else if (unit.RemainingMovement <= 0) { status = "Exhausted"; statusColor = new Color(0.5f, 0.3f, 0.3f); }

        var statusLabel = new Label { Text = status, CustomMinimumSize = new Vector2(80, 0) };
        statusLabel.AddThemeFontSizeOverride("font_size", 11);
        statusLabel.AddThemeColorOverride("font_color", statusColor);
        row.AddChild(statusLabel);

        return row;
    }

    private Button CreateToggleButton(string text, bool active)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(140, 30) };
        btn.AddThemeFontSizeOverride("font_size", 13);
        var style = active
            ? new StyleBoxFlat { BgColor = new Color(0.55f, 0.25f, 0.15f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 }
            : new StyleBoxFlat { BgColor = new Color(0.3f, 0.25f, 0.2f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        btn.AddThemeStyleboxOverride("normal", style);
        return btn;
    }

    private string GenerateMilitaryAdvice(int playerUnits, int aiUnits, int military)
    {
        if (playerUnits == 0)
            return "We have no military forces! Build Warriors immediately to defend our cities!";
        if (_sim.IsAtWarWithAi && playerUnits < aiUnits)
            return $"We are at WAR and outnumbered! The enemy has approximately {aiUnits} units. Build more troops immediately or seek peace!";
        if (_sim.IsAtWarWithAi && playerUnits >= aiUnits)
            return "We are at war but our forces are strong. Press the attack or fortify our borders!";
        if (playerUnits > aiUnits * 1.5f)
            return "Our military dominance is overwhelming. We could crush our rivals if we wish, or maintain this superiority as a deterrent.";
        if (playerUnits > aiUnits)
            return "We have a military advantage. Our forces are well-positioned to defend the empire.";
        if (playerUnits < aiUnits * 0.5f)
            return "We are dangerously outnumbered! The enemy could attack at any time. Prioritize military production immediately!";
        if (military == 0)
            return "We have no combat units! Our empire is defenseless. Build Warriors or Archers to protect our cities.";
        return "Our military is adequate. Consider building more units if expansion or war is planned.";
    }

    private void AddStatLine(VBoxContainer parent, string label, string value, Color valueColor)
    {
        var hbox = new HBoxContainer();
        var lbl = new Label { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        lbl.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.3f));
        var val = new Label { Text = value };
        val.AddThemeFontSizeOverride("font_size", 13);
        val.AddThemeColorOverride("font_color", valueColor);
        hbox.AddChild(lbl);
        hbox.AddChild(val);
        parent.AddChild(hbox);
    }
}

// Extension to cleanly chain AddChild returning the child
public static class NodeExtensions
{
    public static T AddChildWithReturn<T>(this Node parent, T child) where T : Node
    {
        parent.AddChild(child);
        return child;
    }
}
