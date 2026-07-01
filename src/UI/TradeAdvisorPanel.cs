using Godot;
using CivGame.Core;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.UI;

/// <summary>
/// Civ3-authentic Trade Advisor (F2).
/// Layout mirrors the original:
///   - Header bar with title
///   - Advisor portrait + speech bubble (top)
///   - Economy summary stat boxes
///   - Main content: Available Resources (left) + City Commerce Table (right)
///   - Bottom bar with totals + close
/// </summary>
public partial class TradeAdvisorPanel : PanelContainer
{
    private GameSimulation _sim;

    public TradeAdvisorPanel(GameSimulation sim, bool embedded = false)
    {
        _sim = sim;
        Name = "TradeAdvisorPanel";

        if (!embedded)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            var styleBox = new StyleBoxFlat
            {
                BgColor = new Color(0.91f, 0.87f, 0.78f, 1.0f),
                BorderWidthTop = 5, BorderWidthBottom = 5, BorderWidthLeft = 5, BorderWidthRight = 5,
                BorderColor = new Color(0.6f, 0.5f, 0.3f),
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
            };
            AddThemeStyleboxOverride("panel", styleBox);
        }
        else
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            var transparentStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
            AddThemeStyleboxOverride("panel", transparentStyle);
        }

        var outerMargin = new MarginContainer();
        outerMargin.AddThemeConstantOverride("margin_top", 12);
        outerMargin.AddThemeConstantOverride("margin_bottom", 12);
        outerMargin.AddThemeConstantOverride("margin_left", 16);
        outerMargin.AddThemeConstantOverride("margin_right", 16);
        outerMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        outerMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 8);

        if (!embedded)
        {
            // ═══════════════════════════════════════════════
            // HEADER BAR
            // ═══════════════════════════════════════════════
            var headerPanel = new PanelContainer();
            var headerStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.45f, 0.35f, 0.15f),
                ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6
            };
            headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
            var headerHBox = new HBoxContainer();
            var titleLabel = new Label
            {
                Text = "T R A D E   A D V I S O R",
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
        }

        // ═══════════════════════════════════════════════
        // ADVISOR ROW: portrait + speech bubble + economy stats
        // ═══════════════════════════════════════════════
        var advisorRow = new HBoxContainer();
        advisorRow.AddThemeConstantOverride("separation", 12);

        // Advisor portrait
        var portraitPanel = new PanelContainer { CustomMinimumSize = new Vector2(80, 90) };
        var portraitStyle = new StyleBoxFlat { BgColor = new Color(0.4f, 0.3f, 0.15f, 0.9f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };
        portraitPanel.AddThemeStyleboxOverride("panel", portraitStyle);
        var portraitLabel = new Label { Text = "\U0001f4b0", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        portraitLabel.AddThemeFontSizeOverride("font_size", 48);
        portraitPanel.AddChild(portraitLabel);
        advisorRow.AddChild(portraitPanel);

        // Speech bubble
        var speechPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var speechStyle = new StyleBoxFlat
        {
            BgColor = new Color(1.0f, 1.0f, 0.95f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.5f, 0.4f, 0.2f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        speechPanel.AddThemeStyleboxOverride("panel", speechStyle);

        string advice = GenerateTradeAdvice();
        var speechLabel = new Label { Text = advice, AutowrapMode = TextServer.AutowrapMode.Word };
        speechLabel.AddThemeFontSizeOverride("font_size", 13);
        speechLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        speechPanel.AddChild(speechLabel);
        advisorRow.AddChild(speechPanel);

        // Economy stat boxes (compact row)
        var statsVBox = new VBoxContainer { CustomMinimumSize = new Vector2(150, 0) };
        statsVBox.AddThemeConstantOverride("separation", 4);
        AddStatLine(statsVBox, "Treasury:", $"{_sim.PlayerTreasury} gold", new Color(0.6f, 0.5f, 0.1f));
        AddStatLine(statsVBox, "Income:", $"+{_sim.LastTurnIncome}/turn", new Color(0.15f, 0.5f, 0.15f));
        AddStatLine(statsVBox, "Expenses:", $"-{_sim.LastTurnMaintenance}/turn", new Color(0.6f, 0.15f, 0.15f));
        AddStatLine(statsVBox, "Science:", $"+{_sim.LastTurnScience}/turn", new Color(0.15f, 0.2f, 0.6f));
        AddStatLine(statsVBox, "Net Gold:", _sim.LastTurnNetGold >= 0 ? $"+{_sim.LastTurnNetGold}/turn" : $"{_sim.LastTurnNetGold}/turn",
            _sim.LastTurnNetGold >= 0 ? new Color(0.1f, 0.5f, 0.1f) : new Color(0.7f, 0.1f, 0.1f));
        advisorRow.AddChild(statsVBox);

        mainVBox.AddChild(advisorRow);

        // ═══════════════════════════════════════════════
        // MAIN CONTENT: Map + Resources + City Commerce
        // ═══════════════════════════════════════════════
        var contentHBox = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        contentHBox.AddThemeConstantOverride("separation", 10);

        // --- MAP: Territory and cities ---
        var tradeMapOptions = new AdvisorMapOptions
        {
            MinSize = new Vector2(200, 150),
            ShowCities = true,
            ShowUnits = false,
            ShowTerritory = true,
            ShowCityNames = true,
            ShowFogOfWar = false,
            CityFactionFilter = Faction.Player,
            CityDotScale = 2.0f
        };
        var tradeMap = new AdvisorMapPanel(_sim, tradeMapOptions);
        tradeMap.SizeFlagsVertical = SizeFlags.ExpandFill;
        contentHBox.AddChild(tradeMap);

        // --- LEFT: Available Resources ---
        var resourcesPanel = new PanelContainer { CustomMinimumSize = new Vector2(280, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        var resPanelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.85f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.6f, 0.5f, 0.35f),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        resourcesPanel.AddThemeStyleboxOverride("panel", resPanelStyle);

        var resVBox = new VBoxContainer();
        resVBox.AddThemeConstantOverride("separation", 6);

        var resTitle = new Label { Text = "Available Resources", HorizontalAlignment = HorizontalAlignment.Center };
        resTitle.AddThemeFontSizeOverride("font_size", 16);
        resTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        resVBox.AddChild(resTitle);
        resVBox.AddChild(CreateDivider(new Color(0.6f, 0.5f, 0.35f)));

        // Gather resources from player-owned tiles
        var playerLuxuries = new HashSet<string>();
        var playerStrategic = new HashSet<string>();
        var playerBonus = new HashSet<string>();

        foreach (var city in _sim.Cities.Where(c => c.Faction == Faction.Player))
        {
            // Check tiles around city for resources
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    var tile = _sim.Map.GetTile(city.X + dx, city.Y + dy);
                    if (tile?.Resource == null) continue;
                    if (!string.IsNullOrEmpty(tile.OwnerCityId) && tile.OwnerCityId == city.Id)
                    {
                        switch (tile.Resource.Category)
                        {
                            case ResourceCategory.Luxury: playerLuxuries.Add(tile.Resource.Id); break;
                            case ResourceCategory.Strategic: playerStrategic.Add(tile.Resource.Id); break;
                            case ResourceCategory.Bonus: playerBonus.Add(tile.Resource.Id); break;
                        }
                    }
                }
            }
        }

        // Luxury Resources section
        var luxTitle = new Label { Text = "Luxury Resources", HorizontalAlignment = HorizontalAlignment.Left };
        luxTitle.AddThemeFontSizeOverride("font_size", 13);
        luxTitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.3f, 0.6f));
        resVBox.AddChild(luxTitle);

        if (playerLuxuries.Count > 0)
        {
            var luxFlow = new HFlowContainer();
            luxFlow.AddThemeConstantOverride("h_separation", 8);
            luxFlow.AddThemeConstantOverride("v_separation", 4);
            foreach (var luxId in playerLuxuries.OrderBy(x => x))
            {
                var res = ResourceRegistry.Get(luxId);
                if (res == null) continue;
                luxFlow.AddChild(CreateResourceChip(res.Name, new Color(0.85f, 0.75f, 0.95f), new Color(0.4f, 0.2f, 0.55f)));
            }
            resVBox.AddChild(luxFlow);
        }
        else
        {
            var noLux = new Label { Text = "  None connected" };
            noLux.AddThemeFontSizeOverride("font_size", 12);
            noLux.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            resVBox.AddChild(noLux);
        }

        resVBox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        // Strategic Resources section
        var stratTitle = new Label { Text = "Strategic Resources", HorizontalAlignment = HorizontalAlignment.Left };
        stratTitle.AddThemeFontSizeOverride("font_size", 13);
        stratTitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.3f, 0.15f));
        resVBox.AddChild(stratTitle);

        if (playerStrategic.Count > 0)
        {
            var stratFlow = new HFlowContainer();
            stratFlow.AddThemeConstantOverride("h_separation", 8);
            stratFlow.AddThemeConstantOverride("v_separation", 4);
            foreach (var stratId in playerStrategic.OrderBy(x => x))
            {
                var res = ResourceRegistry.Get(stratId);
                if (res == null) continue;
                var strat = res as StrategicResource;
                bool revealed = strat == null || _sim.Research.IsResearched(strat.RequiredTechId);
                if (revealed)
                    stratFlow.AddChild(CreateResourceChip(res.Name, new Color(0.95f, 0.88f, 0.75f), new Color(0.5f, 0.3f, 0.1f)));
            }
            resVBox.AddChild(stratFlow);
        }
        else
        {
            var noStrat = new Label { Text = "  None connected" };
            noStrat.AddThemeFontSizeOverride("font_size", 12);
            noStrat.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            resVBox.AddChild(noStrat);
        }

        resVBox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        // Bonus Resources section
        var bonusTitle = new Label { Text = "Bonus Resources", HorizontalAlignment = HorizontalAlignment.Left };
        bonusTitle.AddThemeFontSizeOverride("font_size", 13);
        bonusTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.5f, 0.2f));
        resVBox.AddChild(bonusTitle);

        if (playerBonus.Count > 0)
        {
            var bonusFlow = new HFlowContainer();
            bonusFlow.AddThemeConstantOverride("h_separation", 8);
            bonusFlow.AddThemeConstantOverride("v_separation", 4);
            foreach (var bonusId in playerBonus.OrderBy(x => x))
            {
                var res = ResourceRegistry.Get(bonusId);
                if (res == null) continue;
                bonusFlow.AddChild(CreateResourceChip(res.Name, new Color(0.85f, 0.95f, 0.8f), new Color(0.2f, 0.45f, 0.15f)));
            }
            resVBox.AddChild(bonusFlow);
        }
        else
        {
            var noBonus = new Label { Text = "  None nearby" };
            noBonus.AddThemeFontSizeOverride("font_size", 12);
            noBonus.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            resVBox.AddChild(noBonus);
        }

        // Trade deals placeholder
        resVBox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        resVBox.AddChild(CreateDivider(new Color(0.6f, 0.5f, 0.35f)));
        var dealsTitle = new Label { Text = "Active Trade Deals", HorizontalAlignment = HorizontalAlignment.Center };
        dealsTitle.AddThemeFontSizeOverride("font_size", 14);
        dealsTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        resVBox.AddChild(dealsTitle);
        var noDeals = new Label { Text = "No active trade agreements.", HorizontalAlignment = HorizontalAlignment.Center };
        noDeals.AddThemeFontSizeOverride("font_size", 12);
        noDeals.AddThemeColorOverride("font_color", new Color(0.5f, 0.45f, 0.35f));
        resVBox.AddChild(noDeals);

        resourcesPanel.AddChild(resVBox);
        contentHBox.AddChild(resourcesPanel);

        // --- RIGHT: City Commerce Table ---
        var tablePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var tablePanelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.85f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.6f, 0.5f, 0.35f),
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6
        };
        tablePanel.AddThemeStyleboxOverride("panel", tablePanelStyle);

        var tableVBox = new VBoxContainer();
        tableVBox.AddThemeConstantOverride("separation", 0);
        tableVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tableVBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        var tableTitle = new Label { Text = "City Commerce", HorizontalAlignment = HorizontalAlignment.Center };
        tableTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        tableTitle.AddThemeFontSizeOverride("font_size", 16);
        tableVBox.AddChild(tableTitle);
        tableVBox.AddChild(CreateDivider(new Color(0.6f, 0.5f, 0.35f)));

        // Header row
        tableVBox.AddChild(CreateTradeRow("City", "Pop", "Commerce", "Maint.", "Net", true));
        tableVBox.AddChild(CreateDivider(new Color(0.7f, 0.6f, 0.45f)));

        var scrollContainer = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var scrollVBox = new VBoxContainer();
        scrollVBox.AddThemeConstantOverride("separation", 0);
        scrollVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var playerCities = _sim.Cities
            .Where(c => c.Faction == Faction.Player)
            .OrderByDescending(c => c.StoredCommerce)
            .ToList();

        bool alt = false;
        foreach (var city in playerCities)
        {
            int cityMaintenance = city.GetTotalMaintenance();
            int netCommerce = city.StoredCommerce - cityMaintenance;
            var row = CreateTradeRow(
                city.Name,
                city.Population.ToString(),
                city.StoredCommerce.ToString(),
                cityMaintenance.ToString(),
                netCommerce >= 0 ? $"+{netCommerce}" : $"{netCommerce}",
                false, alt
            );
            scrollVBox.AddChild(row);
            alt = !alt;
        }

        scrollContainer.AddChild(scrollVBox);
        tableVBox.AddChild(scrollContainer);
        tablePanel.AddChild(tableVBox);
        contentHBox.AddChild(tablePanel);

        mainVBox.AddChild(contentHBox);

        // ═══════════════════════════════════════════════
        // BOTTOM BAR
        // ═══════════════════════════════════════════════
        var bottomPanel = new PanelContainer();
        var bottomStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.45f, 0.35f, 0.15f, 0.9f),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        bottomPanel.AddThemeStyleboxOverride("panel", bottomStyle);

        var bottomHBox = new HBoxContainer();
        bottomHBox.AddThemeConstantOverride("separation", 20);

        int totalCommerce = playerCities.Sum(c => c.StoredCommerce);
        int totalMaint = playerCities.Sum(c => c.GetTotalMaintenance());

        var totComLabel = new Label { Text = $"Total Commerce: {totalCommerce}", VerticalAlignment = VerticalAlignment.Center };
        totComLabel.AddThemeFontSizeOverride("font_size", 13);
        totComLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        bottomHBox.AddChild(totComLabel);

        var totMaintLabel = new Label { Text = $"Total Maintenance: {totalMaint}", VerticalAlignment = VerticalAlignment.Center };
        totMaintLabel.AddThemeFontSizeOverride("font_size", 13);
        totMaintLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.7f, 0.7f));
        bottomHBox.AddChild(totMaintLabel);

        var luxCountLabel = new Label { Text = $"Luxuries: {playerLuxuries.Count}/8", VerticalAlignment = VerticalAlignment.Center };
        luxCountLabel.AddThemeFontSizeOverride("font_size", 13);
        luxCountLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.95f));
        bottomHBox.AddChild(luxCountLabel);

        var stratCountLabel = new Label { Text = $"Strategic: {playerStrategic.Count}/8", VerticalAlignment = VerticalAlignment.Center };
        stratCountLabel.AddThemeFontSizeOverride("font_size", 13);
        stratCountLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 0.75f));
        bottomHBox.AddChild(stratCountLabel);

        bottomHBox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        if (!embedded)
        {
            var footerCloseBtn = new Button { Text = "Close", CustomMinimumSize = new Vector2(80, 28) };
            var closeBtnStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.4f, 0.1f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
            footerCloseBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
            footerCloseBtn.Pressed += () => QueueFree();
            bottomHBox.AddChild(footerCloseBtn);
        }

        bottomPanel.AddChild(bottomHBox);
        mainVBox.AddChild(bottomPanel);

        outerMargin.AddChild(mainVBox);
        AddChild(outerMargin);
    }

    private string GenerateTradeAdvice()
    {
        var playerCities = _sim.Cities.Where(c => c.Faction == Faction.Player).ToList();
        if (playerCities.Count == 0)
            return "We have no cities! Found a city to begin collecting commerce and resources.";

        int net = _sim.LastTurnNetGold;
        if (net < -5)
            return $"Our treasury is bleeding {-net} gold per turn! We must reduce expenses or increase commerce immediately. Consider selling buildings or raising the tax rate.";
        if (net < 0)
            return $"We are spending more than we earn ({net} gold/turn). Build Marketplaces and connect luxury resources to boost commerce income.";
        if (_sim.PlayerTreasury < 10 && net <= 0)
            return "Our treasury is nearly empty! We risk going bankrupt. Raise taxes or reduce unit support costs.";
        if (_sim.LastTurnIncome > 30)
            return $"Trade is booming! We generate {_sim.LastTurnIncome} gold per turn. Consider investing more into science to accelerate research.";

        return $"Our economy is stable with a net income of +{net} gold per turn. Connect more luxury resources to keep citizens happy and boost commerce.";
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

    private PanelContainer CreateResourceChip(string name, Color bgColor, Color textColor)
    {
        var chip = new PanelContainer();
        var chipStyle = new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor = textColor.Lightened(0.3f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 3, ContentMarginBottom = 3
        };
        chip.AddThemeStyleboxOverride("panel", chipStyle);
        var chipLabel = new Label { Text = name };
        chipLabel.AddThemeFontSizeOverride("font_size", 12);
        chipLabel.AddThemeColorOverride("font_color", textColor);
        chip.AddChild(chipLabel);
        return chip;
    }

    private ColorRect CreateDivider(Color color)
    {
        return new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = color };
    }

    private HBoxContainer CreateTradeRow(string city, string pop, string commerce, string maintenance, string net, bool isHeader, bool altBg = false)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);

        if (!isHeader && altBg)
        {
            row.Modulate = new Color(0.96f, 0.94f, 0.9f);
        }

        var cityLabel = new Label { Text = city, CustomMinimumSize = new Vector2(140, 26), HorizontalAlignment = HorizontalAlignment.Left };
        var popLabel = new Label { Text = pop, CustomMinimumSize = new Vector2(50, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var comLabel = new Label { Text = commerce, CustomMinimumSize = new Vector2(80, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var maintLabel = new Label { Text = maintenance, CustomMinimumSize = new Vector2(70, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var netLabel = new Label { Text = net, CustomMinimumSize = new Vector2(70, 26), HorizontalAlignment = HorizontalAlignment.Center };

        int fontSize = isHeader ? 12 : 13;
        var color = isHeader ? new Color(0.4f, 0.35f, 0.25f) : new Color(0.1f, 0.08f, 0.05f);

        cityLabel.AddThemeFontSizeOverride("font_size", fontSize);
        cityLabel.AddThemeColorOverride("font_color", color);
        popLabel.AddThemeFontSizeOverride("font_size", fontSize);
        popLabel.AddThemeColorOverride("font_color", color);
        comLabel.AddThemeFontSizeOverride("font_size", fontSize);
        comLabel.AddThemeColorOverride("font_color", isHeader ? color : new Color(0.15f, 0.4f, 0.15f));
        maintLabel.AddThemeFontSizeOverride("font_size", fontSize);
        maintLabel.AddThemeColorOverride("font_color", isHeader ? color : new Color(0.5f, 0.2f, 0.2f));
        netLabel.AddThemeFontSizeOverride("font_size", fontSize);

        if (!isHeader)
        {
            bool negative = net.StartsWith("-");
            netLabel.AddThemeColorOverride("font_color", negative ? new Color(0.7f, 0.1f, 0.1f) : new Color(0.1f, 0.45f, 0.1f));
        }
        else
        {
            netLabel.AddThemeColorOverride("font_color", color);
        }

        row.AddChild(cityLabel);
        row.AddChild(popLabel);
        row.AddChild(comLabel);
        row.AddChild(maintLabel);
        row.AddChild(netLabel);
        return row;
    }
}
