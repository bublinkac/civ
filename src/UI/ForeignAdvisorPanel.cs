using Godot;
using CivGame.Core;
using System.Linq;

namespace CivGame.UI;

/// <summary>
/// Civ3 Foreign Advisor (F4): Displays diplomatic relations, active treaties, and rival civilization status.
/// Currently a functional placeholder showing war/peace status and known rival info.
/// TODO: Add embassy system, trade agreements, map exchange, tech trading, alliance proposals.
/// </summary>
public partial class ForeignAdvisorPanel : PanelContainer
{
    private GameSimulation _sim;

    public ForeignAdvisorPanel(GameSimulation sim, bool embedded = false)
    {
        _sim = sim;
        Name = "ForeignAdvisorPanel";

        if (!embedded)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            var styleBox = new StyleBoxFlat
            {
                BgColor = new Color(0.9f, 0.88f, 0.8f, 0.95f),
                BorderWidthTop = 4, BorderWidthBottom = 4, BorderWidthLeft = 4, BorderWidthRight = 4,
                BorderColor = new Color(0.7f, 0.65f, 0.4f),
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
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

        var canvasBorder = new PanelContainer();
        canvasBorder.SizeFlagsVertical = SizeFlags.ExpandFill;
        canvasBorder.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        canvasBorder.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.AddThemeConstantOverride("margin_left", 30);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 20);

        if (!embedded)
        {
            var headerHBox = new HBoxContainer();
            var titleLabel = new Label
            {
                Text = "F O R E I G N   A D V I S O R",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 28);
            titleLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));

            var closeBtn = new Button { Text = "\u2716", Flat = true, CustomMinimumSize = new Vector2(40, 40) };
            closeBtn.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            closeBtn.AddThemeFontSizeOverride("font_size", 24);
            closeBtn.Pressed += () => QueueFree();

            headerHBox.AddChild(titleLabel);
            headerHBox.AddChild(closeBtn);
            mainVBox.AddChild(headerHBox);
        }

        // --- ADVISOR MESSAGE ---
        var advisorBox = new HBoxContainer();
        advisorBox.AddThemeConstantOverride("separation", 10);
        var advisorPortrait = new Label { Text = "\U0001f9d1\u200d\u2696\ufe0f" };
        advisorPortrait.AddThemeFontSizeOverride("font_size", 48);

        string warStatus = _sim.IsAtWarWithAi ? "We are at WAR" : "We are at PEACE";
        string advisorMessage = _sim.IsAtWarWithAi
            ? $"{warStatus} with the {_sim.AiCiv.Name}.\nThey are a dangerous foe. Consider building up our military before any offensive."
            : $"{warStatus} with the {_sim.AiCiv.Name}.\nDiplomatic relations are stable. We could propose trade or demand tribute.";

        var messagePanel = new PanelContainer();
        var msgStyle = new StyleBoxFlat { BgColor = Colors.White, BorderColor = new Color(0.5f, 0.5f, 0.3f), BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2 };
        messagePanel.AddThemeStyleboxOverride("panel", msgStyle);
        var msgMargin = new MarginContainer { CustomMinimumSize = new Vector2(400, 80) };
        msgMargin.AddThemeConstantOverride("margin_left", 12);
        msgMargin.AddThemeConstantOverride("margin_right", 12);
        msgMargin.AddThemeConstantOverride("margin_top", 10);
        var msgLabel = new Label { Text = advisorMessage, AutowrapMode = TextServer.AutowrapMode.Word };
        msgLabel.AddThemeColorOverride("font_color", Colors.Black);
        msgLabel.AddThemeFontSizeOverride("font_size", 14);
        msgMargin.AddChild(msgLabel);
        messagePanel.AddChild(msgMargin);

        advisorBox.AddChild(advisorPortrait);
        advisorBox.AddChild(messagePanel);
        mainVBox.AddChild(advisorBox);

        // --- DIVIDER ---
        mainVBox.AddChild(new ColorRect { CustomMinimumSize = new Vector2(0, 2), Color = new Color(0.5f, 0.4f, 0.3f) });

        // --- CONTENT: Map + Rival Table ---
        var contentSplit = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        contentSplit.AddThemeConstantOverride("separation", 12);

        // Territory map
        var foreignMapOptions = new AdvisorMapOptions
        {
            MinSize = new Vector2(240, 160),
            ShowCities = true,
            ShowUnits = false,
            ShowTerritory = true,
            ShowCityNames = true,
            ShowFogOfWar = true,
            CityDotScale = 2.0f
        };
        var foreignMap = new AdvisorMapPanel(_sim, foreignMapOptions);
        foreignMap.SizeFlagsVertical = SizeFlags.ExpandFill;
        contentSplit.AddChild(foreignMap);

        // --- RIVAL CIVILIZATIONS TABLE ---
        var tableVBox = new VBoxContainer();
        tableVBox.AddThemeConstantOverride("separation", 10);
        tableVBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        tableVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var tableTitle = new Label { Text = "Known Civilizations", HorizontalAlignment = HorizontalAlignment.Center };
        tableTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.2f));
        tableTitle.AddThemeFontSizeOverride("font_size", 20);
        tableVBox.AddChild(tableTitle);

        // Table header
        var headerRow = CreateTableRow("Civilization", "Leader", "Status", "Cities", "Military", true);
        tableVBox.AddChild(headerRow);

        // Player row
        int playerCities = _sim.Cities.Count(c => c.Faction == Faction.Player);
        int playerUnits = _sim.Units.Count(u => u.Faction == Faction.Player);
        var playerRow = CreateTableRow(_sim.PlayerCiv.Name, _sim.PlayerCiv.LeaderName, "YOU", playerCities.ToString(), playerUnits.ToString(), false);
        tableVBox.AddChild(playerRow);

        // AI Rival row
        int aiCities = _sim.Cities.Count(c => c.Faction == Faction.AiRival);
        int aiUnits = _sim.Units.Count(u => u.Faction == Faction.AiRival);
        string status = _sim.IsAtWarWithAi ? "AT WAR" : "Peace";
        var aiRow = CreateTableRow(_sim.AiCiv.Name, _sim.AiCiv.LeaderName, status, aiCities.ToString(), "?");
        if (_sim.IsAtWarWithAi)
        {
            aiRow.Modulate = new Color(1.0f, 0.85f, 0.85f);
        }
        tableVBox.AddChild(aiRow);

        // --- FUTURE: More civs will be listed here ---
        var placeholder = new Label { Text = "\n(Additional civilizations will appear here as the game expands to multi-civ.)", HorizontalAlignment = HorizontalAlignment.Center };
        placeholder.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        placeholder.AddThemeFontSizeOverride("font_size", 12);
        tableVBox.AddChild(placeholder);

        contentSplit.AddChild(tableVBox);
        mainVBox.AddChild(contentSplit);

        if (!embedded)
        {
            var footerMargin = new MarginContainer();
            footerMargin.AddThemeConstantOverride("margin_top", 10);
            footerMargin.AddThemeConstantOverride("margin_bottom", 10);
            footerMargin.AddThemeConstantOverride("margin_right", 20);
            var footerCloseBtn = new Button { Text = "Close Advisor", CustomMinimumSize = new Vector2(150, 40) };
            footerCloseBtn.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            var btnStyle2 = new StyleBoxFlat { BgColor = new Color(0.7f, 0.2f, 0.2f, 1.0f), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 };
            footerCloseBtn.AddThemeStyleboxOverride("normal", btnStyle2);
            footerCloseBtn.Pressed += () => QueueFree();
            footerMargin.AddChild(footerCloseBtn);
            mainVBox.AddChild(footerMargin);
        }

        margin.AddChild(mainVBox);
        canvasBorder.AddChild(margin);
        AddChild(canvasBorder);
    }

    private HBoxContainer CreateTableRow(string civ, string leader, string status, string cities, string military, bool isHeader = false)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);

        var civLabel = new Label { Text = civ, CustomMinimumSize = new Vector2(180, 30), HorizontalAlignment = HorizontalAlignment.Left };
        var leaderLabel = new Label { Text = leader, CustomMinimumSize = new Vector2(140, 30), HorizontalAlignment = HorizontalAlignment.Left };
        var statusLabel = new Label { Text = status, CustomMinimumSize = new Vector2(100, 30), HorizontalAlignment = HorizontalAlignment.Center };
        var citiesLabel = new Label { Text = cities, CustomMinimumSize = new Vector2(60, 30), HorizontalAlignment = HorizontalAlignment.Center };
        var milLabel = new Label { Text = military, CustomMinimumSize = new Vector2(60, 30), HorizontalAlignment = HorizontalAlignment.Center };

        int fontSize = isHeader ? 14 : 15;
        var color = isHeader ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.1f, 0.1f, 0.1f);

        foreach (var label in new[] { civLabel, leaderLabel, statusLabel, citiesLabel, milLabel })
        {
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
        }

        if (status == "AT WAR") statusLabel.AddThemeColorOverride("font_color", Colors.Red);

        row.AddChild(civLabel);
        row.AddChild(leaderLabel);
        row.AddChild(statusLabel);
        row.AddChild(citiesLabel);
        row.AddChild(milLabel);
        return row;
    }
}
