using Godot;
using CivGame.Core;
using System.Collections.Generic;
using System.Linq;

namespace CivGame.UI;

/// <summary>
/// Civ3-authentic Cultural Advisor (F5).
/// Layout mirrors the original:
///   - Header bar with title
///   - Advisor portrait + speech bubble (top-left)
///   - City culture table (center) with Civ3 culture levels
///   - Top 5 Cities ranking (right panel)
///   - National totals bar + victory progress (bottom)
/// </summary>
public partial class CulturalAdvisorPanel : PanelContainer
{
    private GameSimulation _sim;

    // Civ3 culture thresholds for city "level" names
    private static readonly (int Threshold, string Name)[] CultureLevels =
    {
        (50000, "Legendary"),
        (10000, "Distinguished"),
        (5000,  "Influential"),
        (1000,  "Refined"),
        (100,   "Developing"),
        (10,    "Fledgling"),
        (0,     "Unknown")
    };

    private static string GetCultureLevel(int culture)
    {
        foreach (var (threshold, name) in CultureLevels)
        {
            if (culture >= threshold) return name;
        }
        return "Unknown";
    }

    private static Color GetCultureLevelColor(string level) => level switch
    {
        "Legendary"     => new Color(0.85f, 0.65f, 0.0f),
        "Distinguished" => new Color(0.6f, 0.2f, 0.75f),
        "Influential"   => new Color(0.2f, 0.4f, 0.8f),
        "Refined"       => new Color(0.2f, 0.6f, 0.3f),
        "Developing"    => new Color(0.4f, 0.4f, 0.4f),
        "Fledgling"     => new Color(0.5f, 0.45f, 0.35f),
        _               => new Color(0.3f, 0.3f, 0.3f)
    };

    public CulturalAdvisorPanel(GameSimulation sim)
    {
        _sim = sim;
        Name = "CulturalAdvisorPanel";

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        // Civ3 parchment background
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.91f, 0.87f, 0.78f, 1.0f),
            BorderWidthTop = 5, BorderWidthBottom = 5, BorderWidthLeft = 5, BorderWidthRight = 5,
            BorderColor = new Color(0.55f, 0.45f, 0.3f),
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
            BgColor = new Color(0.35f, 0.25f, 0.45f),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6
        };
        headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
        var headerHBox = new HBoxContainer();
        var titleLabel = new Label
        {
            Text = "C U L T U R A L   A D V I S O R",
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
        // ADVISOR ROW: portrait + speech bubble + national stats
        // ═══════════════════════════════════════════════
        var playerCities = _sim.Cities.Where(c => c.Faction == Faction.Player).ToList();
        int totalCulture = playerCities.Sum(c => c.AccumulatedCulture);
        int totalCulturePerTurn = playerCities.Sum(c => c.CultureOutput);
        int maxCityCulture = playerCities.Any() ? playerCities.Max(c => c.AccumulatedCulture) : 0;
        string bestCityName = playerCities.Any() ? playerCities.OrderByDescending(c => c.AccumulatedCulture).First().Name : "—";
        int threshold = GameSimulation.CulturalVictoryThreshold;

        var advisorRow = new HBoxContainer();
        advisorRow.AddThemeConstantOverride("separation", 12);

        // Advisor portrait
        var portraitPanel = new PanelContainer { CustomMinimumSize = new Vector2(80, 90) };
        var portraitStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.2f, 0.4f, 0.9f), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };
        portraitPanel.AddThemeStyleboxOverride("panel", portraitStyle);
        var portraitLabel = new Label { Text = "\U0001f3ad", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        portraitLabel.AddThemeFontSizeOverride("font_size", 48);
        portraitPanel.AddChild(portraitLabel);
        advisorRow.AddChild(portraitPanel);

        // Speech bubble
        var speechPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var speechStyle = new StyleBoxFlat
        {
            BgColor = new Color(1.0f, 1.0f, 0.95f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.4f, 0.3f, 0.5f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        speechPanel.AddThemeStyleboxOverride("panel", speechStyle);

        string advice = GenerateAdvisorAdvice(playerCities, totalCulture, maxCityCulture, threshold);
        var speechLabel = new Label { Text = advice, AutowrapMode = TextServer.AutowrapMode.Word };
        speechLabel.AddThemeFontSizeOverride("font_size", 13);
        speechLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        speechPanel.AddChild(speechLabel);
        advisorRow.AddChild(speechPanel);

        // National Culture Stats box
        var statsPanel = new PanelContainer { CustomMinimumSize = new Vector2(200, 0) };
        var statsStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.88f, 0.95f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.5f, 0.4f, 0.6f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        statsPanel.AddThemeStyleboxOverride("panel", statsStyle);
        var statsVBox = new VBoxContainer();
        statsVBox.AddThemeConstantOverride("separation", 2);
        AddStatLine(statsVBox, "National Culture:", totalCulture.ToString("N0"), new Color(0.3f, 0.15f, 0.5f));
        AddStatLine(statsVBox, "Per Turn:", $"+{totalCulturePerTurn}", new Color(0.2f, 0.5f, 0.3f));
        AddStatLine(statsVBox, "Best City:", bestCityName, new Color(0.1f, 0.1f, 0.1f));
        AddStatLine(statsVBox, "Victory:", $"{maxCityCulture:N0}/{threshold:N0}", new Color(0.5f, 0.3f, 0.1f));
        statsPanel.AddChild(statsVBox);
        advisorRow.AddChild(statsPanel);

        mainVBox.AddChild(advisorRow);

        // ═══════════════════════════════════════════════
        // MAIN CONTENT: Map + City Table + Top 5
        // ═══════════════════════════════════════════════
        var contentHBox = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        contentHBox.AddThemeConstantOverride("separation", 10);

        // --- MAP: Culture overlay ---
        var cultureMapOptions = new AdvisorMapOptions
        {
            MinSize = new Vector2(220, 160),
            ShowCities = true,
            ShowUnits = false,
            ShowTerritory = true,
            ShowCultureRadius = true,
            ShowCityNames = true,
            ShowFogOfWar = false,
            CityDotScale = 2.0f
        };
        var cultureMap = new AdvisorMapPanel(_sim, cultureMapOptions);
        cultureMap.SizeFlagsVertical = SizeFlags.ExpandFill;
        contentHBox.AddChild(cultureMap);

        // --- City Culture Table ---
        var tablePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var tablePanelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.92f, 0.85f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.6f, 0.5f, 0.4f),
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6
        };
        tablePanel.AddThemeStyleboxOverride("panel", tablePanelStyle);

        var tableVBox = new VBoxContainer();
        tableVBox.AddThemeConstantOverride("separation", 0);
        tableVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tableVBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        var tableTitle = new Label { Text = "City Cultural Status", HorizontalAlignment = HorizontalAlignment.Center };
        tableTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        tableTitle.AddThemeFontSizeOverride("font_size", 16);
        tableVBox.AddChild(tableTitle);
        tableVBox.AddChild(CreateDivider(new Color(0.6f, 0.5f, 0.4f)));

        // Header row
        tableVBox.AddChild(CreateCityRow("City", "Culture/Turn", "Total", "Level", true));
        tableVBox.AddChild(CreateDivider(new Color(0.7f, 0.65f, 0.55f)));

        var scrollContainer = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var scrollVBox = new VBoxContainer();
        scrollVBox.AddThemeConstantOverride("separation", 0);
        scrollVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var sortedCities = playerCities.OrderByDescending(c => c.AccumulatedCulture).ToList();
        bool alt = false;
        foreach (var city in sortedCities)
        {
            string level = GetCultureLevel(city.AccumulatedCulture);
            var row = CreateCityRow(city.Name, $"+{city.CultureOutput}", city.AccumulatedCulture.ToString("N0"), level, false, alt);
            scrollVBox.AddChild(row);
            alt = !alt;
        }

        scrollContainer.AddChild(scrollVBox);
        tableVBox.AddChild(scrollContainer);
        tablePanel.AddChild(tableVBox);
        contentHBox.AddChild(tablePanel);

        // --- RIGHT: Top 5 Cities + Wonders ---
        var rightPanel = new VBoxContainer { CustomMinimumSize = new Vector2(240, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        rightPanel.AddThemeConstantOverride("separation", 10);

        // Top 5 Cities Ranking
        var top5Panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var top5Style = new StyleBoxFlat
        {
            BgColor = new Color(0.93f, 0.9f, 0.82f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.55f, 0.45f, 0.3f),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        top5Panel.AddThemeStyleboxOverride("panel", top5Style);

        var top5VBox = new VBoxContainer();
        top5VBox.AddThemeConstantOverride("separation", 4);
        var top5Title = new Label { Text = "Top 5 Cities", HorizontalAlignment = HorizontalAlignment.Center };
        top5Title.AddThemeFontSizeOverride("font_size", 15);
        top5Title.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        top5VBox.AddChild(top5Title);
        top5VBox.AddChild(CreateDivider(new Color(0.6f, 0.5f, 0.35f)));

        // Combine player + AI cities and rank
        var allCities = _sim.Cities.OrderByDescending(c => c.AccumulatedCulture).Take(5).ToList();
        int rank = 1;
        foreach (var city in allCities)
        {
            bool isPlayer = city.Faction == Faction.Player;
            string civName = isPlayer ? _sim.PlayerCiv.Name : _sim.AiCiv.Name;
            string label = $"{rank}. {city.Name} ({civName})";
            string cultureStr = city.AccumulatedCulture.ToString("N0");
            var rankRow = new HBoxContainer();
            var rankLabel = new Label { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rankLabel.AddThemeFontSizeOverride("font_size", 12);
            rankLabel.AddThemeColorOverride("font_color", isPlayer ? new Color(0.1f, 0.3f, 0.1f) : new Color(0.5f, 0.15f, 0.15f));
            var cultureLabel = new Label { Text = cultureStr };
            cultureLabel.AddThemeFontSizeOverride("font_size", 12);
            cultureLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.4f));
            rankRow.AddChild(rankLabel);
            rankRow.AddChild(cultureLabel);
            top5VBox.AddChild(rankRow);
            rank++;
        }

        top5Panel.AddChild(top5VBox);
        rightPanel.AddChild(top5Panel);

        // Wonders list
        var wondersPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var wondersStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.93f, 0.88f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderColor = new Color(0.55f, 0.45f, 0.3f),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8
        };
        wondersPanel.AddThemeStyleboxOverride("panel", wondersStyle);

        var wondersVBox = new VBoxContainer();
        wondersVBox.AddThemeConstantOverride("separation", 3);
        var wondersTitle = new Label { Text = "Our Wonders", HorizontalAlignment = HorizontalAlignment.Center };
        wondersTitle.AddThemeFontSizeOverride("font_size", 14);
        wondersTitle.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        wondersVBox.AddChild(wondersTitle);
        wondersVBox.AddChild(CreateDivider(new Color(0.6f, 0.5f, 0.35f)));

        var wonderScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        var wonderListVBox = new VBoxContainer();
        wonderListVBox.AddThemeConstantOverride("separation", 2);
        int wonderCount = 0;
        foreach (var city in playerCities)
        {
            foreach (var b in city.Buildings)
            {
                if (b is Wonder w)
                {
                    var wRow = new HBoxContainer();
                    var wName = new Label { Text = w.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                    wName.AddThemeFontSizeOverride("font_size", 11);
                    wName.AddThemeColorOverride("font_color", new Color(0.25f, 0.15f, 0.4f));
                    var wCity = new Label { Text = $"({city.Name})" };
                    wCity.AddThemeFontSizeOverride("font_size", 11);
                    wCity.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
                    wRow.AddChild(wName);
                    wRow.AddChild(wCity);
                    wonderListVBox.AddChild(wRow);
                    wonderCount++;
                }
            }
        }
        if (wonderCount == 0)
        {
            var noWonders = new Label { Text = "No wonders yet.", HorizontalAlignment = HorizontalAlignment.Center };
            noWonders.AddThemeFontSizeOverride("font_size", 11);
            noWonders.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            wonderListVBox.AddChild(noWonders);
        }
        wonderScroll.AddChild(wonderListVBox);
        wondersVBox.AddChild(wonderScroll);
        wondersPanel.AddChild(wondersVBox);
        rightPanel.AddChild(wondersPanel);

        contentHBox.AddChild(rightPanel);
        mainVBox.AddChild(contentHBox);

        // ═══════════════════════════════════════════════
        // BOTTOM: Victory Progress Bar
        // ═══════════════════════════════════════════════
        var bottomPanel = new PanelContainer();
        var bottomStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.35f, 0.25f, 0.45f, 0.9f),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        bottomPanel.AddThemeStyleboxOverride("panel", bottomStyle);

        var bottomHBox = new HBoxContainer();
        bottomHBox.AddThemeConstantOverride("separation", 15);

        var victoryLabel = new Label { Text = "Cultural Victory:", VerticalAlignment = VerticalAlignment.Center };
        victoryLabel.AddThemeFontSizeOverride("font_size", 14);
        victoryLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        bottomHBox.AddChild(victoryLabel);

        var progressBar = new ProgressBar { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 22) };
        progressBar.MinValue = 0;
        progressBar.MaxValue = 100;
        float progress = threshold > 0 ? (float)maxCityCulture / threshold * 100f : 0;
        progressBar.Value = System.Math.Min(100, progress);
        bottomHBox.AddChild(progressBar);

        var progressLabel = new Label { Text = $"{maxCityCulture:N0} / {threshold:N0} ({progress:F1}%)", VerticalAlignment = VerticalAlignment.Center };
        progressLabel.AddThemeFontSizeOverride("font_size", 13);
        progressLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.8f));
        bottomHBox.AddChild(progressLabel);

        var footerCloseBtn = new Button { Text = "Close", CustomMinimumSize = new Vector2(80, 28) };
        var closeBtnStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.3f, 0.15f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        footerCloseBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        footerCloseBtn.Pressed += () => QueueFree();
        bottomHBox.AddChild(footerCloseBtn);

        bottomPanel.AddChild(bottomHBox);
        mainVBox.AddChild(bottomPanel);

        outerMargin.AddChild(mainVBox);
        AddChild(outerMargin);
    }

    private string GenerateAdvisorAdvice(List<City> cities, int totalCulture, int maxCityCulture, int threshold)
    {
        if (cities.Count == 0)
            return "We have no cities! Found a city and build cultural buildings to begin generating culture.";

        if (maxCityCulture >= threshold)
            return "One of our cities has achieved LEGENDARY cultural status! Cultural Victory is within reach!";

        if (maxCityCulture >= threshold / 2)
            return $"Our culture is flourishing magnificently! We are over halfway to Cultural Victory. Keep building temples, cathedrals, and wonders!";

        // Check which culture building is missing
        bool allHaveTemples = cities.All(c => c.Buildings.Any(b => b.Id == "temple"));
        bool allHaveLibraries = cities.All(c => c.Buildings.Any(b => b.Id == "library"));
        bool allHaveMonuments = cities.All(c => c.Buildings.Any(b => b.Id == "monument"));
        bool allHaveCathedrals = cities.All(c => c.Buildings.Any(b => b.Id == "cathedral"));

        if (!allHaveMonuments)
            return "I recommend building Monuments in all cities. They provide a steady stream of culture and expand our borders quickly.";
        if (!allHaveTemples)
            return "Build Temples in all cities to increase our cultural output significantly. Temples also make citizens happier.";
        if (!allHaveLibraries)
            return "Libraries would boost our cultural production in every city. Knowledge and culture go hand in hand!";
        if (!allHaveCathedrals)
            return "Cathedrals are powerful cultural engines. Build them in all cities and watch our borders expand!";

        return $"Our civilization generates +{cities.Sum(c => c.CultureOutput)} culture per turn. Continue building wonders to accelerate our cultural dominance!";
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

    private ColorRect CreateDivider(Color color)
    {
        return new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = color };
    }

    private HBoxContainer CreateCityRow(string name, string culturePerTurn, string totalCulture, string level, bool isHeader, bool altBg = false)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);

        if (!isHeader && altBg)
        {
            // Zebra striping via modulate
            row.Modulate = new Color(0.96f, 0.94f, 0.9f);
        }

        var nameLabel = new Label { Text = name, CustomMinimumSize = new Vector2(160, 26), HorizontalAlignment = HorizontalAlignment.Left };
        var cptLabel = new Label { Text = culturePerTurn, CustomMinimumSize = new Vector2(100, 26), HorizontalAlignment = HorizontalAlignment.Center };
        var totalLabel = new Label { Text = totalCulture, CustomMinimumSize = new Vector2(110, 26), HorizontalAlignment = HorizontalAlignment.Right };
        var levelLabel = new Label { Text = level, CustomMinimumSize = new Vector2(120, 26), HorizontalAlignment = HorizontalAlignment.Center };

        int fontSize = isHeader ? 12 : 13;
        var color = isHeader ? new Color(0.4f, 0.35f, 0.25f) : new Color(0.1f, 0.08f, 0.05f);

        nameLabel.AddThemeFontSizeOverride("font_size", fontSize);
        nameLabel.AddThemeColorOverride("font_color", color);
        cptLabel.AddThemeFontSizeOverride("font_size", fontSize);
        cptLabel.AddThemeColorOverride("font_color", isHeader ? color : new Color(0.2f, 0.5f, 0.2f));
        totalLabel.AddThemeFontSizeOverride("font_size", fontSize);
        totalLabel.AddThemeColorOverride("font_color", isHeader ? color : new Color(0.3f, 0.2f, 0.4f));
        levelLabel.AddThemeFontSizeOverride("font_size", fontSize);
        levelLabel.AddThemeColorOverride("font_color", isHeader ? color : GetCultureLevelColor(level));

        row.AddChild(nameLabel);
        row.AddChild(cptLabel);
        row.AddChild(totalLabel);
        row.AddChild(levelLabel);
        return row;
    }
}
