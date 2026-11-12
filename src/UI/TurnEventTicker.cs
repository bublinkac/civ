using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Compact turn event ticker positioned at the top of the HUD.
/// Displays cycling alerts, advisor updates, and provides 1-click camera zoom or modal expansion.
/// </summary>
public partial class TurnEventTicker : PanelContainer
{
    private readonly GameSimulation _sim;
    private List<TurnEvent> _currentEvents = new();
    private int _currentIndex = 0;
    private double _cycleTimer = 0.0;
    private const double CycleInterval = 6.0;

    // UI Nodes
    private Button _badgeBtn = null!;
    private Button _headlineBtn = null!;
    private Label _indexLabel = null!;
    private Button _prevBtn = null!;
    private Button _nextBtn = null!;
    private Button _expandBtn = null!;
    private Button _closeBtn = null!;

    public event Action? OnOpenAllNotifications;
    public event Action<TurnEvent>? OnExecuteEventAction;

    public TurnEventTicker(GameSimulation sim)
    {
        _sim = sim;
        Name = "TurnEventTicker";

        // Center at top below main top bar
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        AnchorTop = 0f;
        AnchorBottom = 0f;
        OffsetLeft = -320;
        OffsetRight = 320;
        OffsetTop = 50;
        OffsetBottom = 88;
        CustomMinimumSize = new Vector2(640, 38);

        SetupLayout();
        RefreshEvents();
    }

    private void SetupLayout()
    {
        AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.08f, 0.10f, 0.15f, 0.95f),
            new Color(0.85f, 0.68f, 0.22f, 0.95f),
            borderWidth: 1.5f,
            cornerRadius: 6
        ));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(hbox);

        // 1. Badge Button (e.g. "🔔 3 udalosti")
        _badgeBtn = new Button
        {
            CustomMinimumSize = new Vector2(130, 28)
        };
        _badgeBtn.AddThemeFontSizeOverride("font_size", 11);
        _badgeBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.18f, 0.22f, 0.32f), new Color(0.35f, 0.45f, 0.65f), 1, 4));
        _badgeBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.24f, 0.30f, 0.45f), Colors.White, 1.5f, 4));
        _badgeBtn.Pressed += () => OnOpenAllNotifications?.Invoke();
        hbox.AddChild(_badgeBtn);

        // 2. Headline Button (Current Event Title)
        _headlineBtn = new Button
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = HorizontalAlignment.Left,
            ClipText = true
        };
        _headlineBtn.AddThemeFontSizeOverride("font_size", 12);
        _headlineBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.10f, 0.12f, 0.18f, 0.7f), Colors.Transparent, 0, 4));
        _headlineBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.16f, 0.20f, 0.28f), new Color(0.95f, 0.75f, 0.25f), 1, 4));
        _headlineBtn.Pressed += OnHeadlineClicked;
        hbox.AddChild(_headlineBtn);

        // 3. Navigation Controls
        _prevBtn = new Button
        {
            Text = "◀",
            CustomMinimumSize = new Vector2(24, 26)
        };
        _prevBtn.AddThemeFontSizeOverride("font_size", 10);
        _prevBtn.Pressed += PrevEvent;
        hbox.AddChild(_prevBtn);

        _indexLabel = new Label
        {
            Text = "1/1",
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(32, 26)
        };
        _indexLabel.AddThemeFontSizeOverride("font_size", 10);
        _indexLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.75f, 0.85f));
        hbox.AddChild(_indexLabel);

        _nextBtn = new Button
        {
            Text = "▶",
            CustomMinimumSize = new Vector2(24, 26)
        };
        _nextBtn.AddThemeFontSizeOverride("font_size", 10);
        _nextBtn.Pressed += NextEvent;
        hbox.AddChild(_nextBtn);

        // 4. Expand All Button
        _expandBtn = new Button
        {
            Text = "📜 Správy [N]",
            CustomMinimumSize = new Vector2(95, 26)
        };
        _expandBtn.AddThemeFontSizeOverride("font_size", 11);
        _expandBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.16f, 0.28f, 0.20f), new Color(0.35f, 0.65f, 0.45f), 1, 4));
        _expandBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.22f, 0.38f, 0.28f), Colors.White, 1.5f, 4));
        _expandBtn.Pressed += () => OnOpenAllNotifications?.Invoke();
        hbox.AddChild(_expandBtn);

        // 5. Dismiss Button
        _closeBtn = new Button
        {
            Text = "✕",
            CustomMinimumSize = new Vector2(24, 26)
        };
        _closeBtn.AddThemeFontSizeOverride("font_size", 10);
        _closeBtn.Pressed += () => Visible = false;
        hbox.AddChild(_closeBtn);
    }

    public override void _Process(double delta)
    {
        if (!Visible || _currentEvents.Count <= 1) return;

        _cycleTimer += delta;
        if (_cycleTimer >= CycleInterval)
        {
            _cycleTimer = 0.0;
            NextEvent();
        }
    }

    public void RefreshEvents()
    {
        _currentEvents = _sim.TurnEvents.GetTurnEvents(_sim.TurnNumber, _sim.ActiveTurnFaction);
        
        // If current turn has no events yet, show latest events from empire log
        if (_currentEvents.Count == 0)
        {
            _currentEvents = _sim.TurnEvents.GetLatestEvents(_sim.ActiveTurnFaction, 10);
        }

        if (_currentEvents.Count == 0)
        {
            Visible = false;
            return;
        }

        Visible = true;
        _currentIndex = Math.Clamp(_currentIndex, 0, _currentEvents.Count - 1);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_currentEvents.Count == 0)
        {
            Visible = false;
            return;
        }

        var ev = _currentEvents[_currentIndex];
        int unreadCount = _currentEvents.Count(e => !e.IsRead);

        bool hasCritical = _currentEvents.Any(e => e.Severity == TurnEventSeverity.Critical);
        bool hasWarning = _currentEvents.Any(e => e.Severity == TurnEventSeverity.Warning);

        // Border styling based on urgency
        Color borderColor = hasCritical ? new Color(0.95f, 0.25f, 0.25f) :
                            (hasWarning ? new Color(0.95f, 0.65f, 0.20f) : new Color(0.85f, 0.68f, 0.22f));

        AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.08f, 0.10f, 0.15f, 0.95f),
            borderColor,
            borderWidth: hasCritical ? 2f : 1.5f,
            cornerRadius: 6
        ));

        _badgeBtn.Text = unreadCount > 0 ? $"🔔 {unreadCount} nových" : $"📰 {_currentEvents.Count} správ";
        _indexLabel.Text = $"{_currentIndex + 1}/{_currentEvents.Count}";

        string actionHint = ev.ActionType switch
        {
            TurnEventActionType.ZoomToTile => " [🔍 Zamerať]",
            TurnEventActionType.OpenCity => " [🏛️ Mesto]",
            TurnEventActionType.OpenTechTree => " [🔬 Veda]",
            TurnEventActionType.OpenAdvisor => " [👥 Poradca]",
            _ => ""
        };

        _headlineBtn.Text = $"{ev.Icon} {ev.Title}{actionHint}";

        // Text color by severity
        Color textColor = ev.Severity switch
        {
            TurnEventSeverity.Critical => new Color(1.0f, 0.45f, 0.45f),
            TurnEventSeverity.Warning => new Color(1.0f, 0.85f, 0.40f),
            TurnEventSeverity.Success => new Color(0.55f, 0.95f, 0.65f),
            _ => Colors.White
        };
        _headlineBtn.AddThemeColorOverride("font_color", textColor);
    }

    private void OnHeadlineClicked()
    {
        if (_currentEvents.Count == 0) return;
        var ev = _currentEvents[_currentIndex];
        ev.IsRead = true;
        CivGame.Audio.AudioManager.Instance?.PlaySfx("res://assets/sounds/ui_click.wav");
        OnExecuteEventAction?.Invoke(ev);
        UpdateDisplay();
    }

    public void NextEvent()
    {
        if (_currentEvents.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % _currentEvents.Count;
        _cycleTimer = 0.0;
        UpdateDisplay();
    }

    public void PrevEvent()
    {
        if (_currentEvents.Count == 0) return;
        _currentIndex = (_currentIndex - 1 + _currentEvents.Count) % _currentEvents.Count;
        _cycleTimer = 0.0;
        UpdateDisplay();
    }

    private static StyleBoxFlat CreatePanelStyle(Color bg, Color border, float borderWidth = 1.5f, float cornerRadius = 6)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = (int)borderWidth,
            BorderWidthTop = (int)borderWidth,
            BorderWidthRight = (int)borderWidth,
            BorderWidthBottom = (int)borderWidth,
            CornerRadiusTopLeft = (int)cornerRadius,
            CornerRadiusTopRight = (int)cornerRadius,
            CornerRadiusBottomLeft = (int)cornerRadius,
            CornerRadiusBottomRight = (int)cornerRadius
        };
    }
}
