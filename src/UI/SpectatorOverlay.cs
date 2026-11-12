using System;
using System.Linq;
using Godot;
using CivGame.Core;
using CivGame.Render;

namespace CivGame.UI;

public partial class SpectatorOverlay : Control
{
    private SpectatorController? _controller;
    private GameSimulation? _sim;

    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _factionsLabel = null!;
    private Button _playPauseBtn = null!;

    public void Initialize(SpectatorController controller, GameSimulation sim)
    {
        _controller = controller;
        _sim = sim;

        _controller.OnSpectatorStateChanged += HandleSpectatorStateChanged;
        _controller.OnTurnAdvanced += Refresh;

        Visible = _controller.IsActive;
        Refresh();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.TopWide);
        CustomMinimumSize = new Vector2(0, 140);
        ZIndex = 50;

        var centerContainer = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        centerContainer.SetAnchorsPreset(LayoutPreset.TopWide);
        centerContainer.OffsetTop = 50;
        AddChild(centerContainer);

        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(820, 130),
            MouseFilter = MouseFilterEnum.Stop
        };

        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.07f, 0.11f, 0.94f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.92f, 0.75f, 0.25f, 0.95f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);
        centerContainer.AddChild(card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        card.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        // Header Title
        _titleLabel = new Label
        {
            Text = "👁️  SPECTATOR MODE (AI VS AI)  —  AUTOMATED SIMULATION",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 15);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.8f, 0.3f));
        vbox.AddChild(_titleLabel);

        // Turn & Speed Status
        _statusLabel = new Label
        {
            Text = "Turn: 1 (4000 BC) | Status: Running (0.2s/turn)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.9f));
        vbox.AddChild(_statusLabel);

        // Factions Summary
        _factionsLabel = new Label
        {
            Text = "Civilizations: Loading...",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _factionsLabel.AddThemeFontSizeOverride("font_size", 11);
        _factionsLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.95f));
        vbox.AddChild(_factionsLabel);

        // Controls Row
        var controlsHBox = new HBoxContainer();
        controlsHBox.AddThemeConstantOverride("separation", 8);
        controlsHBox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(controlsHBox);

        _playPauseBtn = CreateButton("⏸ Pause", new Color(0.2f, 0.35f, 0.5f));
        _playPauseBtn.Pressed += OnPlayPausePressed;
        controlsHBox.AddChild(_playPauseBtn);

        var stepBtn = CreateButton("⏭ Step 1 Turn", new Color(0.25f, 0.4f, 0.25f));
        stepBtn.Pressed += OnStepPressed;
        controlsHBox.AddChild(stepBtn);

        var sep1 = new VSeparator();
        controlsHBox.AddChild(sep1);

        // Speed presets
        var speed05Btn = CreateButton("0.5s", new Color(0.18f, 0.22f, 0.28f));
        speed05Btn.Pressed += () => SetSpeed(0.5f);
        controlsHBox.AddChild(speed05Btn);

        var speed02Btn = CreateButton("0.2s", new Color(0.2f, 0.28f, 0.38f));
        speed02Btn.Pressed += () => SetSpeed(0.2f);
        controlsHBox.AddChild(speed02Btn);

        var speed005Btn = CreateButton("0.05s", new Color(0.25f, 0.35f, 0.48f));
        speed005Btn.Pressed += () => SetSpeed(0.05f);
        controlsHBox.AddChild(speed005Btn);

        var turboBtn = CreateButton("⚡ Turbo (0s)", new Color(0.45f, 0.25f, 0.55f));
        turboBtn.Pressed += () => SetSpeed(0.0f);
        controlsHBox.AddChild(turboBtn);

        var sep2 = new VSeparator();
        controlsHBox.AddChild(sep2);

        // Fast forward buttons
        var ff10Btn = CreateButton("+10", new Color(0.35f, 0.3f, 0.15f));
        ff10Btn.Pressed += () => _controller?.FastForward(10);
        controlsHBox.AddChild(ff10Btn);

        var ff50Btn = CreateButton("+50", new Color(0.45f, 0.35f, 0.15f));
        ff50Btn.Pressed += () => _controller?.FastForward(50);
        controlsHBox.AddChild(ff50Btn);

        var ff100Btn = CreateButton("+100", new Color(0.55f, 0.4f, 0.15f));
        ff100Btn.Pressed += () => _controller?.FastForward(100);
        controlsHBox.AddChild(ff100Btn);

        var sep3 = new VSeparator();
        controlsHBox.AddChild(sep3);

        var exitBtn = CreateButton("❌ Exit (F10)", new Color(0.5f, 0.18f, 0.18f));
        exitBtn.Pressed += OnExitPressed;
        controlsHBox.AddChild(exitBtn);

        Visible = _controller?.IsActive ?? false;
    }

    private void HandleSpectatorStateChanged(bool active)
    {
        Visible = active;
        if (active)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (_controller == null || _sim == null) return;

        string pauseText = _controller.IsPaused ? "⏸ PAUSED" : $"▶ RUNNING ({_controller.TurnDelaySeconds:F2}s/turn)";
        string yearFormatted = TurnTimeline.GetFormattedYear(_sim.TurnNumber);
        _statusLabel.Text = $"Turn: {_sim.TurnNumber} ({yearFormatted}) | Status: {pauseText} | Total Automated: {_controller.TurnsSimulatedInSession} turns";
        _playPauseBtn.Text = _controller.IsPaused ? "▶ Play" : "⏸ Pause";

        // Build brief summary of factions
        var summaries = _sim.ActiveFactions
            .Where(f => !f.IsBarbarian())
            .Select(f =>
            {
                var civ = _sim.GetCivilization(f);
                int cities = _sim.Cities.Count(c => c.Faction == f);
                int units = _sim.Units.Count(u => u.Faction == f);
                string tag = f.IsPlayer() ? "Player AI" : "Rival AI";
                return $"{civ.Name} ({tag}): {cities} cities, {units} units";
            });

        _factionsLabel.Text = string.Join("  |  ", summaries);
    }

    private void OnPlayPausePressed()
    {
        if (_controller == null) return;
        _controller.TogglePause();
        Refresh();
    }

    private void OnStepPressed()
    {
        if (_controller == null) return;
        _controller.StepTurn();
        Refresh();
    }

    private void SetSpeed(float seconds)
    {
        if (_controller == null) return;
        _controller.SetSpeed(seconds);
        Refresh();
    }

    private void OnExitPressed()
    {
        _controller?.DisableSpectatorMode();
    }

    private static Button CreateButton(string text, Color baseColor)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(0, 26)
        };
        btn.AddThemeFontSizeOverride("font_size", 11);

        var normal = new StyleBoxFlat
        {
            BgColor = baseColor,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        normal.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat
        {
            BgColor = baseColor.Lightened(0.2f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        hover.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat
        {
            BgColor = baseColor.Darkened(0.2f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        pressed.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        return btn;
    }
}
