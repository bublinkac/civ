using System;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Civ3-authentic modal dialog displayed when player forces conquer an enemy city.
/// Gives the commander the strategic choice to Install a Governor (Keep) or Raze the city to ashes.
/// </summary>
public partial class CityCaptureDialog : CanvasLayer
{
    private readonly GameSimulation _sim;
    private readonly City _city;
    private readonly Unit _capturingUnit;

    public event Action<CityCaptureChoice>? OnChoiceMade;

    public CityCaptureDialog(GameSimulation sim, City city, Unit capturingUnit)
    {
        _sim = sim;
        _city = city;
        _capturingUnit = capturingUnit;

        Layer = 115; // Above standard HUD and map overlays

        // 1. Fullscreen Dimmed Overlay
        var overlay = new ColorRect
        {
            Color = new Color(0.0f, 0.0f, 0.02f, 0.82f)
        };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        // 2. Center Panel Container
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(740, 520)
        };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.11f, 0.15f, 0.98f),
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderColor = new Color(0.85f, 0.68f, 0.22f), // Antique Gold
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ShadowColor = new Color(0, 0, 0, 0.7f),
            ShadowSize = 16
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        overlay.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        margin.AddChild(vbox);

        // Header Title
        var headerLabel = new Label
        {
            Text = "⚔️   C I T Y   C O N Q U E R E D   ⚔️",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        headerLabel.AddThemeFontSizeOverride("font_size", 22);
        headerLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.25f));
        vbox.AddChild(headerLabel);

        // Subtitle with unit & city name
        var subtitleLabel = new Label
        {
            Text = $"Our triumphant forces led by {_capturingUnit.Type} have captured the city of {_city.Name}!",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitleLabel.AddThemeFontSizeOverride("font_size", 14);
        subtitleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        vbox.AddChild(subtitleLabel);

        // City Details Dossier Box
        var dossier = CreateCityDossier();
        vbox.AddChild(dossier);

        // Prompt question
        var promptLabel = new Label
        {
            Text = "What is your decree regarding the fate of this city?",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        promptLabel.AddThemeFontSizeOverride("font_size", 15);
        promptLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.3f));
        vbox.AddChild(promptLabel);

        // Choices Container (Keep vs Raze)
        var choicesHBox = new HBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        choicesHBox.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(choicesHBox);

        // Option 1: Keep City Card
        var keepBtn = CreateChoiceButton(
            title: "🏰 INSTALL GOVERNOR\n(Keep City)",
            desc: $"Annex {_city.Name} into our realm.\n• Population becomes {Math.Max(1, _city.Population - 1)}.\n• All Great Wonders and infrastructure remain intact.\n• Citizens will resist for {Math.Max(1, _city.Population / 2)} turn(s).\n• Plunders +{Math.Min(35, 10 + _city.Population * 5)} 💰 gold.",
            bgColor: new Color(0.12f, 0.28f, 0.44f),
            hoverColor: new Color(0.18f, 0.38f, 0.60f),
            borderColor: new Color(0.4f, 0.7f, 1.0f),
            onPressed: () => SelectChoice(CityCaptureChoice.Keep)
        );
        keepBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        choicesHBox.AddChild(keepBtn);

        // Option 2: Raze City Card
        int captiveWorkers = Math.Clamp(_city.Population / 2, 1, 2);
        int razeGold = Math.Min(60, 15 + _city.Population * 10);
        var razeBtn = CreateChoiceButton(
            title: "🔥 RAZE TO THE GROUND\n(Burn City)",
            desc: $"Burn {_city.Name} to ashes!\n• City and territory borders are wiped from map.\n• Plunders +{razeGold} 💰 gold from the ruins.\n• Enslaves {captiveWorkers} captive Worker unit(s).\n• Warning: Destroys all wonders & infrastructure.",
            bgColor: new Color(0.48f, 0.12f, 0.12f),
            hoverColor: new Color(0.65f, 0.18f, 0.18f),
            borderColor: new Color(1.0f, 0.35f, 0.35f),
            onPressed: () => SelectChoice(CityCaptureChoice.Raze)
        );
        razeBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        choicesHBox.AddChild(razeBtn);
    }

    private Control CreateCityDossier()
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.11f, 0.85f),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.45f, 0.6f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        panel.AddChild(margin);

        var grid = new GridContainer
        {
            Columns = 2
        };
        grid.AddThemeConstantOverride("h_separation", 24);
        grid.AddThemeConstantOverride("v_separation", 6);
        margin.AddChild(grid);

        string oldCiv = _city.Faction == Faction.AiRival ? _sim.AiCiv.Name : "Hostile Power";
        string oldLeader = _city.Faction == Faction.AiRival ? _sim.AiCiv.LeaderName : "Unknown";

        AddDossierRow(grid, "Former Empire:", $"{oldCiv} ({oldLeader})");
        AddDossierRow(grid, "Population:", $"{_city.Population} citizen(s)");

        var wonders = _city.Buildings.OfType<Wonder>().Where(w => !w.IsNationalWonder).ToList();
        string wonderText = wonders.Count > 0 ? string.Join(", ", wonders.Select(w => w.Name)) : "None";
        AddDossierRow(grid, "Great Wonders:", wonderText);

        AddDossierRow(grid, "Buildings:", $"{_city.Buildings.Count} structure(s)");
        AddDossierRow(grid, "Location:", $"({_city.X}, {_city.Y})");

        return panel;
    }

    private static void AddDossierRow(GridContainer grid, string label, string val)
    {
        var lbl = new Label
        {
            Text = label
        };
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        grid.AddChild(lbl);

        var v = new Label
        {
            Text = val
        };
        v.AddThemeFontSizeOverride("font_size", 13);
        v.AddThemeColorOverride("font_color", Colors.White);
        grid.AddChild(v);
    }

    private static Button CreateChoiceButton(string title, string desc, Color bgColor, Color hoverColor, Color borderColor, Action onPressed)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(320, 150),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand
        };

        var normalStyle = new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderColor = borderColor,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = hoverColor,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderColor = Colors.White,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        btn.AddThemeStyleboxOverride("normal", normalStyle);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);

        var vbox = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 6);

        var margin = new MarginContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddChild(vbox);
        btn.AddChild(margin);

        var titleLbl = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLbl.AddThemeFontSizeOverride("font_size", 14);
        titleLbl.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(titleLbl);

        var descLbl = new Label
        {
            Text = desc,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLbl.AddThemeFontSizeOverride("font_size", 12);
        descLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        vbox.AddChild(descLbl);

        btn.Pressed += onPressed;
        return btn;
    }

    private void SelectChoice(CityCaptureChoice choice)
    {
        OnChoiceMade?.Invoke(choice);
        QueueFree();
    }
}
