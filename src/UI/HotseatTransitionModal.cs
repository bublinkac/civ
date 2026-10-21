using System;
using Godot;
using CivGame.Core;
using CivGame.Audio;

namespace CivGame.UI;

public partial class HotseatTransitionModal : PanelContainer
{
    public HotseatTransitionModal(GameSimulation sim, Faction nextFaction, Action onStartTurnClicked)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop; // Blocks input to map

        // Backdrop: Deep dark classic Civ3 midnight vignette style
        var backdropStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.04f, 0.06f, 0.98f),
            BorderWidthLeft = 6,
            BorderWidthTop = 6,
            BorderWidthRight = 6,
            BorderWidthBottom = 6,
            BorderColor = new Color(0.75f, 0.58f, 0.22f) // Antique Roman Gold
        };
        AddThemeStyleboxOverride("panel", backdropStyle);

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(center);

        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(580, 420)
        };
        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.08f, 0.12f, 0.95f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            BorderColor = new Color(0.95f, 0.72f, 0.12f, 0.9f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);
        center.AddChild(card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 36);
        margin.AddThemeConstantOverride("margin_right", 36);
        margin.AddThemeConstantOverride("margin_top", 32);
        margin.AddThemeConstantOverride("margin_bottom", 32);
        card.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        margin.AddChild(vbox);

        // Header Title
        var title = new Label
        {
            Text = "🏛️  SID MEIER'S CIVILIZATION III  🏛️",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.75f, 0.3f));
        vbox.AddChild(title);

        var subHeader = new Label
        {
            Text = "⚔️   H O T S E A T   M U L T I P L A Y E R   ⚔️",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subHeader.AddThemeFontSizeOverride("font_size", 14);
        subHeader.AddThemeColorOverride("font_color", new Color(0.85f, 0.25f, 0.25f));
        vbox.AddChild(subHeader);

        vbox.AddChild(new HSeparator());

        var noticeLabel = new Label
        {
            Text = $"Koniec ťahu! (Turn {sim.TurnNumber} - {TurnTimeline.GetFormattedYear(sim.TurnNumber)})\nProsím, odovzdajte počítač protihráčovi:",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        noticeLabel.AddThemeFontSizeOverride("font_size", 15);
        noticeLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.82f, 0.85f));
        vbox.AddChild(noticeLabel);

        // Active Player Highlight Box
        var activeCiv = nextFaction == Faction.AiRival ? sim.AiCiv : sim.PlayerCiv;
        string playerName = nextFaction == Faction.AiRival ? sim.Player2Name : sim.Player1Name;
        Color civThemeColor = nextFaction == Faction.AiRival ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.2f, 0.6f, 0.95f);

        var playerPanel = new PanelContainer();
        var playerPanelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.14f, 0.13f, 0.18f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = civThemeColor,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        playerPanel.AddThemeStyleboxOverride("panel", playerPanelStyle);

        var pMargin = new MarginContainer();
        pMargin.AddThemeConstantOverride("margin_top", 12);
        pMargin.AddThemeConstantOverride("margin_bottom", 12);
        pMargin.AddThemeConstantOverride("margin_left", 16);
        pMargin.AddThemeConstantOverride("margin_right", 16);
        playerPanel.AddChild(pMargin);

        var pVBox = new VBoxContainer();
        pVBox.AddThemeConstantOverride("separation", 6);
        pMargin.AddChild(pVBox);

        var nameLabel = new Label
        {
            Text = $"👤 {playerName.ToUpper()}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 22);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        pVBox.AddChild(nameLabel);

        var civLabel = new Label
        {
            Text = $"Civilizácia: {activeCiv.Name}  |  Vodca: {activeCiv.LeaderName}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        civLabel.AddThemeFontSizeOverride("font_size", 14);
        civLabel.AddThemeColorOverride("font_color", civThemeColor);
        pVBox.AddChild(civLabel);

        vbox.AddChild(playerPanel);

        var secretLabel = new Label
        {
            Text = "🔒 Vaša mapa, mestá a jednotky sú chránené hmlou (Fog of War).",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        secretLabel.AddThemeFontSizeOverride("font_size", 12);
        secretLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        vbox.AddChild(secretLabel);

        // Start Turn Button
        var startBtn = new Button
        {
            Text = "🛡️  ZAČAŤ ŤAH (START TURN)  🛡️",
            CustomMinimumSize = new Vector2(300, 50),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        startBtn.AddThemeFontSizeOverride("font_size", 16);
        var btnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.55f, 0.22f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.95f, 0.72f, 0.12f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        startBtn.AddThemeStyleboxOverride("normal", btnStyle);

        var btnHover = new StyleBoxFlat
        {
            BgColor = new Color(0.16f, 0.68f, 0.28f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = Colors.White,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        startBtn.AddThemeStyleboxOverride("hover", btnHover);

        startBtn.Pressed += () =>
        {
            AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/click.wav");
            QueueFree();
            onStartTurnClicked?.Invoke();
        };
        vbox.AddChild(startBtn);
    }
}
