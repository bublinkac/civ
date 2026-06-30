using System;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Government selection panel. Shows current government, available governments with stats,
/// and allows revolution (with anarchy warning).
/// </summary>
public partial class GovernmentPanel : PanelContainer
{
    private GameSimulation _sim;
    private VBoxContainer _govList;
    public event Action? OnClosed;

    public GovernmentPanel(GameSimulation sim)
    {
        _sim = sim;

        SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.9f, 0.86f, 0.75f, 1.0f),
            BorderWidthLeft = 4, BorderWidthTop = 4, BorderWidthRight = 4, BorderWidthBottom = 4,
            BorderColor = new Color(0.6f, 0.55f, 0.4f, 1.0f)
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        var mainVBox = new VBoxContainer();

        // --- HEADER ---
        var headerLabel = new Label { Text = "G O V E R N M E N T" };
        headerLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f));
        headerLabel.AddThemeFontSizeOverride("font_size", 32);
        headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_top", 10);
        headerMargin.AddThemeConstantOverride("margin_bottom", 5);
        headerMargin.AddChild(headerLabel);
        mainVBox.AddChild(headerMargin);

        // --- CURRENT STATUS ---
        var statusPanel = new PanelContainer();
        var statusStyle = new StyleBoxFlat
        {
            BgColor = _sim.IsInAnarchy
                ? new Color(0.5f, 0.2f, 0.2f, 0.9f)
                : new Color(0.2f, 0.3f, 0.5f, 0.9f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.4f, 0.4f, 0.4f, 1.0f),
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4
        };
        statusPanel.AddThemeStyleboxOverride("panel", statusStyle);
        var statusMargin = new MarginContainer();
        statusMargin.AddThemeConstantOverride("margin_left", 20);
        statusMargin.AddThemeConstantOverride("margin_right", 20);
        statusMargin.AddThemeConstantOverride("margin_top", 8);
        statusMargin.AddThemeConstantOverride("margin_bottom", 8);

        string statusText;
        if (_sim.IsInAnarchy)
        {
            string pendingName = _sim.PendingGovernment.HasValue
                ? Government.Get(_sim.PendingGovernment.Value).Name
                : "Unknown";
            statusText = $"⚠ ANARCHY — Revolution in progress! ({_sim.AnarchyTurnsRemaining} turns remaining)\n" +
                         $"Transitioning to: {pendingName}\n" +
                         "All cities produce NO commerce, NO production. Citizens survive on subsistence food.";
        }
        else
        {
            var currentGov = Government.Get(_sim.PlayerGovernment);
            statusText = $"Current Government: {currentGov.Name}\n" +
                         $"Worker Efficiency: {currentGov.WorkerEfficiency}%  |  " +
                         $"Military Police: {currentGov.MaxMilitaryPolice}  |  " +
                         $"War Weariness: {(currentGov.HasWarWeariness ? (currentGov.WarWearinessSeverity == 2 ? "High" : "Low") : "None")}";
        }

        var statusLabel = new Label { Text = statusText };
        statusLabel.AddThemeColorOverride("font_color", Colors.White);
        statusLabel.AddThemeFontSizeOverride("font_size", 15);
        statusMargin.AddChild(statusLabel);
        statusPanel.AddChild(statusMargin);

        var statusOuterMargin = new MarginContainer();
        statusOuterMargin.AddThemeConstantOverride("margin_left", 20);
        statusOuterMargin.AddThemeConstantOverride("margin_right", 20);
        statusOuterMargin.AddThemeConstantOverride("margin_bottom", 10);
        statusOuterMargin.AddChild(statusPanel);
        mainVBox.AddChild(statusOuterMargin);

        // --- GOVERNMENT LIST ---
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        _govList = new VBoxContainer();
        _govList.AddThemeConstantOverride("separation", 8);

        var outerMargin = new MarginContainer();
        outerMargin.AddThemeConstantOverride("margin_left", 20);
        outerMargin.AddThemeConstantOverride("margin_right", 20);
        outerMargin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        foreach (GovernmentType govType in Government.All)
        {
            var gov = Government.Get(govType);
            bool isCurrent = govType == _sim.PlayerGovernment;
            bool isAvailable = _sim.CanChangeGovernment(govType);
            bool techUnlocked = gov.RequiredTechId == null || _sim.Research.IsResearched(gov.RequiredTechId);

            var card = CreateGovernmentCard(gov, isCurrent, isAvailable, techUnlocked);
            _govList.AddChild(card);
        }

        outerMargin.AddChild(_govList);
        scroll.AddChild(outerMargin);
        mainVBox.AddChild(scroll);

        // --- CLOSE BUTTON ---
        var closeMargin = new MarginContainer();
        closeMargin.AddThemeConstantOverride("margin_top", 8);
        closeMargin.AddThemeConstantOverride("margin_bottom", 10);
        closeMargin.AddThemeConstantOverride("margin_left", 20);
        closeMargin.AddThemeConstantOverride("margin_right", 20);

        var closeBtn = new Button { Text = "Close" };
        closeBtn.CustomMinimumSize = new Vector2(120, 36);
        closeBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        closeBtn.AddThemeColorOverride("font_color", Colors.White);
        closeBtn.AddThemeFontSizeOverride("font_size", 16);
        var closeBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.4f, 0.25f, 0.15f, 1.0f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = new Color(0.6f, 0.5f, 0.3f, 1.0f),
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4
        };
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        closeBtn.Pressed += () => { OnClosed?.Invoke(); QueueFree(); };
        closeMargin.AddChild(closeBtn);
        mainVBox.AddChild(closeMargin);

        AddChild(mainVBox);
    }

    private PanelContainer CreateGovernmentCard(Government gov, bool isCurrent, bool isAvailable, bool techUnlocked)
    {
        var card = new PanelContainer();
        Color bgColor;
        if (isCurrent)
            bgColor = new Color(0.3f, 0.5f, 0.3f, 0.9f);
        else if (!techUnlocked)
            bgColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        else if (isAvailable)
            bgColor = new Color(0.25f, 0.25f, 0.35f, 0.85f);
        else
            bgColor = new Color(0.35f, 0.3f, 0.25f, 0.7f);

        var cardStyle = new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            BorderColor = isCurrent ? new Color(0.4f, 0.8f, 0.4f, 1.0f) : new Color(0.5f, 0.5f, 0.5f, 0.8f),
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 15);

        // Left: Info
        var infoVBox = new VBoxContainer();
        infoVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        // Title
        string titleSuffix = isCurrent ? " (CURRENT)" : (!techUnlocked ? " (LOCKED)" : "");
        var titleLabel = new Label { Text = $"{gov.Name}{titleSuffix}" };
        titleLabel.AddThemeColorOverride("font_color", isCurrent ? new Color(0.7f, 1.0f, 0.7f) : Colors.White);
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        infoVBox.AddChild(titleLabel);

        // Required tech
        if (gov.RequiredTechId != null)
        {
            var techLabel = new Label { Text = $"Requires: {gov.RequiredTechId.Replace("_", " ")}" };
            techLabel.AddThemeColorOverride("font_color", techUnlocked ? new Color(0.6f, 0.8f, 0.6f) : new Color(1.0f, 0.5f, 0.5f));
            techLabel.AddThemeFontSizeOverride("font_size", 13);
            infoVBox.AddChild(techLabel);
        }

        // Stats line 1
        string wwText = gov.HasWarWeariness ? (gov.WarWearinessSeverity == 2 ? "High" : "Low") : "None";
        string hurryText = gov.Hurry switch
        {
            HurryMethod.ForcedLabor => "Forced Labor",
            HurryMethod.PayCitizens => "Pay Citizens",
            _ => "None"
        };
        var stats1 = new Label
        {
            Text = $"Worker: {gov.WorkerEfficiency}%  |  Police: {gov.MaxMilitaryPolice}  |  " +
                   $"War Weariness: {wwText}  |  Draft: {gov.DraftRate}"
        };
        stats1.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        stats1.AddThemeFontSizeOverride("font_size", 13);
        infoVBox.AddChild(stats1);

        // Stats line 2
        var stats2 = new Label
        {
            Text = $"Support T/C/M: {gov.UnitSupportPerTown}/{gov.UnitSupportPerCity}/{gov.UnitSupportPerMetropolis}" +
                   $"  |  Cost: {gov.UnitSupportCost}g  |  Corruption: {gov.Corruption}  |  Hurry: {hurryText}"
        };
        stats2.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
        stats2.AddThemeFontSizeOverride("font_size", 13);
        infoVBox.AddChild(stats2);

        // Bonuses/penalties
        var bonuses = new System.Collections.Generic.List<string>();
        if (gov.HasTilePenalty) bonuses.Add("⚠ Tile penalty: -1 on yields ≥ 3");
        if (gov.HasCommerceBonus) bonuses.Add("✦ Commerce bonus: +1 on tiles with ≥ 1 commerce");
        if (gov.Corruption == CorruptionLevel.Communal) bonuses.Add("★ Flat corruption (no distance factor)");

        if (bonuses.Count > 0)
        {
            var bonusLabel = new Label { Text = string.Join("  |  ", bonuses) };
            bonusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.5f));
            bonusLabel.AddThemeFontSizeOverride("font_size", 12);
            infoVBox.AddChild(bonusLabel);
        }

        hbox.AddChild(infoVBox);

        // Right: Revolution button
        if (isAvailable && !isCurrent)
        {
            var revolutionBtn = new Button { Text = "Revolution!" };
            revolutionBtn.CustomMinimumSize = new Vector2(130, 40);
            revolutionBtn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            revolutionBtn.AddThemeColorOverride("font_color", Colors.White);
            revolutionBtn.AddThemeFontSizeOverride("font_size", 15);

            var btnStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.6f, 0.2f, 0.15f, 1.0f),
                BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
                BorderColor = new Color(0.8f, 0.4f, 0.2f, 1.0f),
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4
            };
            revolutionBtn.AddThemeStyleboxOverride("normal", btnStyle);

            var govType = gov.Type;
            revolutionBtn.Pressed += () => OnRevolutionPressed(govType);
            hbox.AddChild(revolutionBtn);
        }

        margin.AddChild(hbox);
        card.AddChild(margin);
        return card;
    }

    private void OnRevolutionPressed(GovernmentType newType)
    {
        var targetGov = Government.Get(newType);
        bool isReligious = _sim.PlayerCiv.Trait1 == CivTrait.Religious || _sim.PlayerCiv.Trait2 == CivTrait.Religious;

        string warning;
        if (_sim.PlayerGovernment == GovernmentType.Despotism)
        {
            warning = $"Switch to {targetGov.Name} immediately (no anarchy from Despotism).";
        }
        else if (isReligious)
        {
            warning = $"Revolution to {targetGov.Name}!\nAs a Religious civilization, anarchy will last only 1 turn.";
        }
        else
        {
            warning = $"Revolution to {targetGov.Name}!\n" +
                      $"Anarchy will last {targetGov.AnarchyMinTurns}-{targetGov.AnarchyMaxTurns} turns.\n" +
                      "During anarchy: NO production, NO commerce, NO research.";
        }

        // Show confirmation dialog
        var confirmDialog = new AcceptDialog();
        confirmDialog.Title = "Start Revolution?";
        confirmDialog.DialogText = warning;
        confirmDialog.OkButtonText = "Revolt!";
        confirmDialog.AddCancelButton("Cancel");
        confirmDialog.Confirmed += () =>
        {
            _sim.ChangeGovernment(newType);
            OnClosed?.Invoke();
            QueueFree();
        };
        AddChild(confirmDialog);
        confirmDialog.PopupCentered(new Vector2I(400, 200));
    }
}
