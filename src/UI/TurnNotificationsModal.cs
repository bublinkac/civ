using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Full-screen Advisor Notifications and Turn Event Log modal.
/// Presents turn briefings, advisor warnings, city progress, and military intelligence
/// with 1-click camera zoom, city management, and tech tree shortcuts.
/// </summary>
public partial class TurnNotificationsModal : PanelContainer
{
    private readonly GameSimulation _sim;
    private string _currentCategoryFilter = "Všetky";
    private VBoxContainer _eventsList = null!;
    private HBoxContainer _categoryFilterBar = null!;
    private Label _subtitleLabel = null!;

    public event Action? OnClosed;
    public event Action<TurnEvent>? OnActionRequested;

    public TurnNotificationsModal(GameSimulation sim)
    {
        _sim = sim;
        Name = "TurnNotificationsModal";

        AnchorLeft = 0.08f;
        AnchorRight = 0.92f;
        AnchorTop = 0.06f;
        AnchorBottom = 0.94f;
        MouseFilter = MouseFilterEnum.Stop;

        // Dark obsidian backdrop with Civ gold border
        AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.06f, 0.08f, 0.12f, 0.98f),
            new Color(0.85f, 0.68f, 0.22f, 0.95f),
            borderWidth: 2,
            cornerRadius: 8
        ));

        SetupLayout();
        RefreshEvents();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Escape || keyEvent.Keycode == Key.N)
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Space)
            {
                // Spacebar steps to next unread event
                StepNextUnreadEvent();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void Close()
    {
        CivGame.Audio.AudioManager.Instance?.PlaySfx("res://assets/sounds/ui_click.wav");
        OnClosed?.Invoke();
        QueueFree();
    }

    private void SetupLayout()
    {
        var mainMargin = new MarginContainer();
        mainMargin.AddThemeConstantOverride("margin_left", 20);
        mainMargin.AddThemeConstantOverride("margin_right", 20);
        mainMargin.AddThemeConstantOverride("margin_top", 16);
        mainMargin.AddThemeConstantOverride("margin_bottom", 16);
        mainMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(mainMargin);

        var rootVBox = new VBoxContainer();
        rootVBox.AddThemeConstantOverride("separation", 12);
        rootVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rootVBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainMargin.AddChild(rootVBox);

        // ══════════════════════════════════════════════════════════
        // 1. TOP HEADER BAR
        // ══════════════════════════════════════════════════════════
        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 16);
        headerHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var titleVBox = new VBoxContainer();
        titleVBox.AddThemeConstantOverride("separation", 0);
        titleVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var titleLabel = new Label
        {
            Text = $"📰 SPRÁVY PORADCOV & UDALOSTI ŤAHU {_sim.TurnNumber} ({TurnTimeline.GetFormattedYear(_sim.TurnNumber)})",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.78f, 0.25f));
        titleVBox.AddChild(titleLabel);

        _subtitleLabel = new Label
        {
            Text = "Hlásenia ministrov a kľúčové udalosti vo vašej ríši",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
        _subtitleLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.70f, 0.80f));
        titleVBox.AddChild(_subtitleLabel);
        headerHBox.AddChild(titleVBox);

        // Close Button
        var closeBtn = new Button
        {
            Text = "✕ Zavrieť [Esc/N]",
            CustomMinimumSize = new Vector2(130, 36)
        };
        closeBtn.AddThemeFontSizeOverride("font_size", 13);
        closeBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.45f, 0.18f, 0.18f), new Color(0.75f, 0.30f, 0.30f), 1, 6));
        closeBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.60f, 0.22f, 0.22f), Colors.White, 1.5f, 6));
        closeBtn.Pressed += Close;
        headerHBox.AddChild(closeBtn);

        rootVBox.AddChild(headerHBox);

        // ══════════════════════════════════════════════════════════
        // 2. CATEGORY FILTER TABS
        // ══════════════════════════════════════════════════════════
        _categoryFilterBar = new HBoxContainer();
        _categoryFilterBar.AddThemeConstantOverride("separation", 8);
        rootVBox.AddChild(_categoryFilterBar);

        // Separator
        var sep = new HSeparator();
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat { BgColor = new Color(0.25f, 0.30f, 0.40f, 0.5f) });
        rootVBox.AddChild(sep);

        // ══════════════════════════════════════════════════════════
        // 3. SCROLLABLE EVENTS FEED
        // ══════════════════════════════════════════════════════════
        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

        _eventsList = new VBoxContainer();
        _eventsList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _eventsList.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_eventsList);
        rootVBox.AddChild(scroll);

        // ══════════════════════════════════════════════════════════
        // 4. BOTTOM ACTION FOOTER
        // ══════════════════════════════════════════════════════════
        var footerHBox = new HBoxContainer();
        footerHBox.AddThemeConstantOverride("separation", 12);
        footerHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var stepBtn = new Button
        {
            Text = "⏭️ Prejsť ďalšiu udalosť na mape [Medzerník]",
            CustomMinimumSize = new Vector2(280, 36)
        };
        stepBtn.AddThemeFontSizeOverride("font_size", 12);
        stepBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.18f, 0.32f, 0.22f), new Color(0.35f, 0.65f, 0.45f), 1, 6));
        stepBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.25f, 0.45f, 0.30f), Colors.White, 1.5f, 6));
        stepBtn.Pressed += StepNextUnreadEvent;
        footerHBox.AddChild(stepBtn);

        var markAllBtn = new Button
        {
            Text = "✓ Označiť všetky za prečítané",
            CustomMinimumSize = new Vector2(210, 36)
        };
        markAllBtn.AddThemeFontSizeOverride("font_size", 12);
        markAllBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.15f, 0.20f, 0.30f), new Color(0.30f, 0.40f, 0.60f), 1, 6));
        markAllBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.20f, 0.28f, 0.42f), Colors.White, 1.5f, 6));
        markAllBtn.Pressed += () =>
        {
            _sim.TurnEvents.MarkAllAsRead(_sim.ActiveTurnFaction);
            RefreshEvents();
        };
        footerHBox.AddChild(markAllBtn);

        rootVBox.AddChild(footerHBox);
    }

    private void BuildFilterTabs(List<TurnEvent> allEvents)
    {
        foreach (Node child in _categoryFilterBar.GetChildren()) child.QueueFree();

        int critCount = allEvents.Count(e => e.Severity == TurnEventSeverity.Critical || e.Severity == TurnEventSeverity.Warning);
        int prodCount = allEvents.Count(e => e.Category == TurnEventCategory.Production || e.Category == TurnEventCategory.Growth);
        int sciCount = allEvents.Count(e => e.Category == TurnEventCategory.Science || e.Category == TurnEventCategory.Wonder);
        int milCount = allEvents.Count(e => e.Category == TurnEventCategory.Military);
        int econCount = allEvents.Count(e => e.Category == TurnEventCategory.Economy || e.Category == TurnEventCategory.Trade || e.Category == TurnEventCategory.Disaster);

        var tabs = new[]
        {
            ("Všetky", $"Všetky ({allEvents.Count})"),
            ("Varovania", $"🚨 Krízy & Varovania ({critCount})"),
            ("Mestá", $"🔨 Mestá & Výroba ({prodCount})"),
            ("Veda", $"🔬 Veda & Objavy ({sciCount})"),
            ("Vojenské", $"⚔️ Vojenské ({milCount})"),
            ("Hospodárstvo", $"💰 Hospodárstvo ({econCount})")
        };

        foreach (var (key, label) in tabs)
        {
            var btn = new Button
            {
                Text = label,
                CustomMinimumSize = new Vector2(130, 30)
            };
            btn.AddThemeFontSizeOverride("font_size", 11);
            string capturedKey = key;
            btn.Pressed += () =>
            {
                _currentCategoryFilter = capturedKey;
                RefreshEvents();
            };

            bool active = _currentCategoryFilter == key;
            if (active)
            {
                btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.85f, 0.68f, 0.22f), Colors.White, 1.5f, 12));
                btn.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
            }
            else
            {
                btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.12f, 0.15f, 0.22f), new Color(0.25f, 0.30f, 0.40f), 1, 12));
                btn.AddThemeColorOverride("font_color", new Color(0.80f, 0.85f, 0.95f));
            }

            _categoryFilterBar.AddChild(btn);
        }
    }

    public void RefreshEvents()
    {
        var turnEvents = _sim.TurnEvents.GetTurnEvents(_sim.TurnNumber, _sim.ActiveTurnFaction);
        if (turnEvents.Count == 0)
        {
            turnEvents = _sim.TurnEvents.GetLatestEvents(_sim.ActiveTurnFaction, 20);
        }

        BuildFilterTabs(turnEvents);
        foreach (Node child in _eventsList.GetChildren()) child.QueueFree();

        var filtered = turnEvents.Where(e =>
        {
            return _currentCategoryFilter switch
            {
                "Varovania" => e.Severity == TurnEventSeverity.Critical || e.Severity == TurnEventSeverity.Warning,
                "Mestá" => e.Category == TurnEventCategory.Production || e.Category == TurnEventCategory.Growth,
                "Veda" => e.Category == TurnEventCategory.Science || e.Category == TurnEventCategory.Wonder,
                "Vojenské" => e.Category == TurnEventCategory.Military,
                "Hospodárstvo" => e.Category == TurnEventCategory.Economy || e.Category == TurnEventCategory.Trade || e.Category == TurnEventCategory.Disaster,
                _ => true
            };
        }).ToList();

        _subtitleLabel.Text = $"Zobrazených {filtered.Count} udalostí pre frakciu {_sim.PlayerCiv.Name}";

        if (filtered.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "Žiadne nové udalosti v tejto kategórii.",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 14);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.60f, 0.70f));
            _eventsList.AddChild(emptyLabel);
            return;
        }

        foreach (var ev in filtered)
        {
            _eventsList.AddChild(CreateEventCard(ev));
        }
    }

    private Control CreateEventCard(TurnEvent ev)
    {
        var card = new PanelContainer();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        Color borderColor = ev.Severity switch
        {
            TurnEventSeverity.Critical => new Color(0.95f, 0.25f, 0.25f),
            TurnEventSeverity.Warning => new Color(0.95f, 0.65f, 0.20f),
            TurnEventSeverity.Success => new Color(0.35f, 0.85f, 0.45f),
            _ => new Color(0.30f, 0.45f, 0.70f)
        };

        Color bgColor = ev.IsRead ? new Color(0.09f, 0.11f, 0.16f, 0.85f) : new Color(0.12f, 0.15f, 0.22f, 0.95f);
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(bgColor, borderColor, ev.IsRead ? 1 : 1.5f, 6));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        card.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        // Header Row: Advisor & Badges
        var topHBox = new HBoxContainer();
        topHBox.AddThemeConstantOverride("separation", 8);

        var advisorLabel = new Label { Text = ev.GetAdvisorDisplayName() };
        advisorLabel.AddThemeFontSizeOverride("font_size", 11);
        advisorLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.78f, 0.90f));
        topHBox.AddChild(advisorLabel);

        topHBox.AddChild(CreateBadge(ev.GetSeverityLabel(), borderColor));

        if (!string.IsNullOrEmpty(ev.TargetCityName))
        {
            topHBox.AddChild(CreateBadge($"🏙️ {ev.TargetCityName}", new Color(0.20f, 0.30f, 0.45f)));
        }

        vbox.AddChild(topHBox);

        // Title Row
        var titleHBox = new HBoxContainer();
        titleHBox.AddThemeConstantOverride("separation", 8);

        var iconLbl = new Label { Text = ev.Icon };
        iconLbl.AddThemeFontSizeOverride("font_size", 18);
        titleHBox.AddChild(iconLbl);

        var titleLbl = new Label { Text = ev.Title };
        titleLbl.AddThemeFontSizeOverride("font_size", 14);
        titleLbl.AddThemeColorOverride("font_color", Colors.White);
        titleHBox.AddChild(titleLbl);

        vbox.AddChild(titleHBox);

        // Message text
        var msgLabel = new RichTextLabel
        {
            Text = ev.Message,
            FitContent = true,
            SelectionEnabled = true
        };
        msgLabel.AddThemeFontSizeOverride("normal_font_size", 12);
        msgLabel.AddThemeColorOverride("default_color", new Color(0.85f, 0.88f, 0.95f));
        vbox.AddChild(msgLabel);

        // Actions Row
        var actionsHBox = new HBoxContainer();
        actionsHBox.AddThemeConstantOverride("separation", 10);

        if (ev.TileX.HasValue && ev.TileY.HasValue)
        {
            var zoomBtn = CreateActionButton("🔍 Zamerať na mape", () =>
            {
                ev.IsRead = true;
                OnActionRequested?.Invoke(ev);
                Close();
            });
            actionsHBox.AddChild(zoomBtn);
        }

        if (!string.IsNullOrEmpty(ev.TargetCityId))
        {
            var cityBtn = CreateActionButton("🏛️ Spravovať mesto", () =>
            {
                ev.IsRead = true;
                OnActionRequested?.Invoke(ev);
                Close();
            });
            actionsHBox.AddChild(cityBtn);
        }

        if (ev.ActionType == TurnEventActionType.OpenTechTree)
        {
            var techBtn = CreateActionButton("🔬 Strom technológií", () =>
            {
                ev.IsRead = true;
                OnActionRequested?.Invoke(ev);
                Close();
            });
            actionsHBox.AddChild(techBtn);
        }
        else if (ev.ActionType == TurnEventActionType.OpenAdvisor)
        {
            var advBtn = CreateActionButton("👥 Otvoriť poradcu", () =>
            {
                ev.IsRead = true;
                OnActionRequested?.Invoke(ev);
                Close();
            });
            actionsHBox.AddChild(advBtn);
        }

        if (!ev.IsRead)
        {
            var readBtn = CreateActionButton("✓ Prečítané", () =>
            {
                ev.IsRead = true;
                RefreshEvents();
            }, new Color(0.12f, 0.16f, 0.22f));
            actionsHBox.AddChild(readBtn);
        }

        vbox.AddChild(actionsHBox);
        return card;
    }

    private void StepNextUnreadEvent()
    {
        var unread = _sim.TurnEvents.GetUnreadEvents(_sim.ActiveTurnFaction);
        if (unread.Count > 0)
        {
            var target = unread[0];
            target.IsRead = true;
            OnActionRequested?.Invoke(target);
            CivGame.Audio.AudioManager.Instance?.PlaySfx("res://assets/sounds/ui_click.wav");
            Close();
        }
        else
        {
            Close();
        }
    }

    private static Button CreateActionButton(string text, Action action, Color? bg = null)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 26)
        };
        btn.AddThemeFontSizeOverride("font_size", 11);
        Color baseCol = bg ?? new Color(0.18f, 0.25f, 0.38f);
        btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(baseCol, baseCol.Lightened(0.2f), 1, 4));
        btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(baseCol.Lightened(0.15f), Colors.White, 1.5f, 4));
        btn.Pressed += action;
        return btn;
    }

    private static PanelContainer CreateBadge(string text, Color bg)
    {
        var badge = new PanelContainer();
        badge.AddThemeStyleboxOverride("panel", CreatePanelStyle(bg, bg.Lightened(0.2f), 1, 8));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_top", 2);
        margin.AddThemeConstantOverride("margin_bottom", 2);

        var lbl = new Label { Text = text };
        lbl.AddThemeFontSizeOverride("font_size", 10);
        lbl.AddThemeColorOverride("font_color", Colors.White);
        margin.AddChild(lbl);
        badge.AddChild(margin);
        return badge;
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
