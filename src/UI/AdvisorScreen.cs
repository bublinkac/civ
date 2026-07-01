using System;
using System.Collections.Generic;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Civ3-style unified advisor screen. All advisors (F1–F5) open this same full-screen panel,
/// with a left sidebar for switching between tabs — matching the original Civ3 advisor faces sidebar.
/// </summary>
public partial class AdvisorScreen : PanelContainer
{
    public enum AdvisorTab { Domestic, Trade, Military, Foreign, Cultural, Science }

    private GameSimulation _sim;
    private AdvisorTab _currentTab;
    private VBoxContainer _sidebarVBox;
    private PanelContainer _contentArea;
    private Control? _currentContent;
    private Label _titleLabel;
    private readonly Dictionary<AdvisorTab, Button> _tabButtons = new();

    public event Action? OnClosed;

    // Tab definitions: label, shortcut hint, color
    private static readonly (AdvisorTab Tab, string Label, string Key, Color Color)[] TabDefs =
    {
        (AdvisorTab.Domestic, "🏠 Domestic", "F1", new Color(0.15f, 0.45f, 0.15f)),
        (AdvisorTab.Trade,    "💰 Trade",    "F2", new Color(0.50f, 0.35f, 0.10f)),
        (AdvisorTab.Military, "⚔️ Military", "F3", new Color(0.45f, 0.15f, 0.15f)),
        (AdvisorTab.Foreign,  "🌍 Foreign",  "F4", new Color(0.40f, 0.40f, 0.15f)),
        (AdvisorTab.Cultural, "🎭 Cultural", "F5", new Color(0.40f, 0.15f, 0.50f)),
        (AdvisorTab.Science,  "🔬 Science",  "F6", new Color(0.15f, 0.35f, 0.55f)),
    };

    public AdvisorScreen(GameSimulation sim, AdvisorTab initialTab = AdvisorTab.Domestic)
    {
        _sim = sim;
        _currentTab = initialTab;
        Name = "AdvisorScreen";

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        // Civ3 parchment background
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.91f, 0.87f, 0.78f, 1.0f),
            BorderWidthTop = 5, BorderWidthBottom = 5, BorderWidthLeft = 5, BorderWidthRight = 5,
            BorderColor = new Color(0.6f, 0.5f, 0.3f),
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        // Main layout: sidebar | content
        var rootHBox = new HBoxContainer();
        rootHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rootHBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        // --- LEFT SIDEBAR ---
        var sidebarPanel = new PanelContainer();
        sidebarPanel.CustomMinimumSize = new Vector2(160, 0);
        var sidebarStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.14f, 0.13f, 0.18f, 0.95f),
            BorderWidthRight = 2,
            BorderColor = new Color(0.5f, 0.45f, 0.3f)
        };
        sidebarPanel.AddThemeStyleboxOverride("panel", sidebarStyle);

        var sidebarMargin = new MarginContainer();
        sidebarMargin.AddThemeConstantOverride("margin_left", 8);
        sidebarMargin.AddThemeConstantOverride("margin_right", 8);
        sidebarMargin.AddThemeConstantOverride("margin_top", 12);
        sidebarMargin.AddThemeConstantOverride("margin_bottom", 12);

        _sidebarVBox = new VBoxContainer();
        _sidebarVBox.AddThemeConstantOverride("separation", 6);

        // Sidebar title
        var sidebarTitle = new Label { Text = "ADVISORS" };
        sidebarTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        sidebarTitle.AddThemeFontSizeOverride("font_size", 16);
        sidebarTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _sidebarVBox.AddChild(sidebarTitle);

        _sidebarVBox.AddChild(new HSeparator());

        // Tab buttons
        foreach (var def in TabDefs)
        {
            var btn = new Button();
            btn.Text = $"{def.Label}\n  {def.Key}";
            btn.CustomMinimumSize = new Vector2(140, 56);
            btn.Alignment = HorizontalAlignment.Left;
            btn.AddThemeFontSizeOverride("font_size", 13);
            btn.AddThemeColorOverride("font_color", Colors.White);

            var tab = def.Tab;
            btn.Pressed += () => SwitchTab(tab);
            _tabButtons[def.Tab] = btn;
            _sidebarVBox.AddChild(btn);
        }

        // Spacer
        _sidebarVBox.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        // Close button in sidebar
        var closeBtn = new Button { Text = "✕ Close" };
        closeBtn.CustomMinimumSize = new Vector2(140, 40);
        closeBtn.AddThemeColorOverride("font_color", Colors.White);
        closeBtn.AddThemeFontSizeOverride("font_size", 14);
        var closeBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.5f, 0.18f, 0.18f),
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
        };
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        closeBtn.Pressed += () => { OnClosed?.Invoke(); QueueFree(); };
        _sidebarVBox.AddChild(closeBtn);

        sidebarMargin.AddChild(_sidebarVBox);
        sidebarPanel.AddChild(sidebarMargin);
        rootHBox.AddChild(sidebarPanel);

        // --- RIGHT CONTENT AREA ---
        var contentVBox = new VBoxContainer();
        contentVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        contentVBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        // Title header
        _titleLabel = new Label { Text = "D O M E S T I C   A D V I S O R" };
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.15f, 0.12f, 0.08f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        var titleMargin = new MarginContainer();
        titleMargin.AddThemeConstantOverride("margin_top", 8);
        titleMargin.AddThemeConstantOverride("margin_bottom", 4);
        titleMargin.AddChild(_titleLabel);
        contentVBox.AddChild(titleMargin);

        contentVBox.AddChild(new HSeparator());

        // Content container — ScrollContainer so tall content can scroll
        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;

        _contentArea = new PanelContainer();
        _contentArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _contentArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        // Transparent bg — content panels provide their own look
        var contentStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
        _contentArea.AddThemeStyleboxOverride("panel", contentStyle);

        scrollContainer.AddChild(_contentArea);
        contentVBox.AddChild(scrollContainer);

        rootHBox.AddChild(contentVBox);
        AddChild(rootHBox);

        // Load initial tab
        SwitchTab(initialTab);
    }

    public void SwitchTab(AdvisorTab tab)
    {
        _currentTab = tab;

        // Update title
        _titleLabel.Text = tab switch
        {
            AdvisorTab.Domestic => "D O M E S T I C   A D V I S O R",
            AdvisorTab.Trade    => "T R A D E   A D V I S O R",
            AdvisorTab.Military => "M I L I T A R Y   A D V I S O R",
            AdvisorTab.Foreign  => "F O R E I G N   A D V I S O R",
            AdvisorTab.Cultural => "C U L T U R A L   A D V I S O R",
            AdvisorTab.Science  => "S C I E N C E   A D V I S O R",
            _ => "A D V I S O R"
        };

        // Update sidebar button styles
        foreach (var def in TabDefs)
        {
            if (_tabButtons.TryGetValue(def.Tab, out var btn))
            {
                bool isActive = def.Tab == tab;
                btn.AddThemeStyleboxOverride("normal", CreateTabStyle(def.Color, isActive));
                btn.AddThemeStyleboxOverride("hover", CreateTabStyle(def.Color.Lightened(0.15f), isActive));
            }
        }

        // Remove current content
        if (_currentContent != null && IsInstanceValid(_currentContent))
        {
            _currentContent.QueueFree();
            _currentContent = null;
        }

        // Create new content
        _currentContent = CreateAdvisorContent(tab);
        if (_currentContent != null)
        {
            _contentArea.AddChild(_currentContent);
        }
    }

    private Control CreateAdvisorContent(AdvisorTab tab)
    {
        return tab switch
        {
            AdvisorTab.Domestic => new DomesticAdvisorPanel(_sim, embedded: true),
            AdvisorTab.Trade    => new TradeAdvisorPanel(_sim, embedded: true),
            AdvisorTab.Military => new MilitaryAdvisorPanel(_sim, embedded: true),
            AdvisorTab.Foreign  => new ForeignAdvisorPanel(_sim, embedded: true),
            AdvisorTab.Cultural => new CulturalAdvisorPanel(_sim, embedded: true),
            AdvisorTab.Science  => new TechTreePanel(_sim, embedded: true),
            _ => new Label { Text = "Unknown advisor" }
        };
    }

    private static StyleBoxFlat CreateTabStyle(Color color, bool active)
    {
        return new StyleBoxFlat
        {
            BgColor = active ? color : color.Darkened(0.4f),
            BorderWidthLeft = active ? 4 : 0,
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthRight = 1,
            BorderColor = active ? new Color(0.95f, 0.72f, 0.12f) : new Color(0.3f, 0.3f, 0.3f, 0.5f),
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
            ContentMarginLeft = 8, ContentMarginTop = 4, ContentMarginBottom = 4
        };
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.F1: SwitchTab(AdvisorTab.Domestic); break;
                case Key.F2: SwitchTab(AdvisorTab.Trade); break;
                case Key.F3: SwitchTab(AdvisorTab.Military); break;
                case Key.F4: SwitchTab(AdvisorTab.Foreign); break;
                case Key.F5: SwitchTab(AdvisorTab.Cultural); break;
                case Key.F6: SwitchTab(AdvisorTab.Science); break;
                case Key.Escape: OnClosed?.Invoke(); QueueFree(); break;
            }
            GetViewport().SetInputAsHandled();
        }
    }
}
