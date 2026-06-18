using System;
using System.IO;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

/// <summary>
/// Main Menu screen displayed at game start. Allows the player to select
/// between starting a new game, loading a game, or exiting, designed with a
/// classic Civilization III Complete aesthetic.
/// </summary>
public partial class MainMenu : CanvasLayer
{
    /// <summary>Fired when the player clicks START GAME. Args: width, height, seed, playerCivId, aiCivId.</summary>
    public event Action<int, int, int, string, string>? OnStartGame;

    /// <summary>Fired when the player chooses to load an existing save.</summary>
    public event Action? OnLoadGame;

    // Map size definitions matching Civilization 3
    private static readonly MapSizeOption[] MapSizes = new[]
    {
        new MapSizeOption("TINY",     60,  60,  "A small continent — quick games, ideal for learning."),
        new MapSizeOption("SMALL",    80,  80,  "A modest world — shorter games with fewer rivals."),
        new MapSizeOption("STANDARD", 100, 100, "The classic experience — balanced exploration & warfare."),
        new MapSizeOption("LARGE",    140, 120, "A vast realm — more land to conquer and explore."),
        new MapSizeOption("HUGE",     180, 180, "An epic world — sprawling empires and long campaigns."),
    };

    private int _selectedIndex = 2; // Default: Standard
    private int _seed;
    private Button[] _sizeButtons = Array.Empty<Button>();
    private Label? _descriptionLabel;
    private Label? _dimensionsLabel;
    private SpinBox? _seedInput;
    private OptionButton? _playerCivInput;
    private OptionButton? _aiCivInput;

    // Containers for different screens
    private Control? _titleScreenContainer;
    private Control? _mapSelectionContainer;

    public override void _Ready()
    {
        Layer = 20; // Above game HUD

        _seed = new Random().Next(1, 99999);

        // 1. Dark metallic-bronze background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.05f, 0.06f, 1.0f);
        bg.AnchorLeft = 0;
        bg.AnchorRight = 1;
        bg.AnchorTop = 0;
        bg.AnchorBottom = 1;
        bg.OffsetLeft = 0;
        bg.OffsetRight = 0;
        bg.OffsetTop = 0;
        bg.OffsetBottom = 0;
        AddChild(bg);

        // Let's create a subtle pattern background (just a centered metallic look)
        var backgroundPanel = new Panel();
        backgroundPanel.AnchorLeft = 0.1f;
        backgroundPanel.AnchorRight = 0.9f;
        backgroundPanel.AnchorTop = 0.1f;
        backgroundPanel.AnchorBottom = 0.9f;
        backgroundPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.08f, 0.08f, 0.10f, 0.2f),
            new Color(0.2f, 0.18f, 0.15f, 0.15f), 1, 20));
        AddChild(backgroundPanel);

        // 2. Build Title Screen Container
        BuildTitleScreen();

        // 3. Build Map Selection Container (hidden by default)
        BuildMapSelectionScreen();
    }

    private void BuildTitleScreen()
    {
        _titleScreenContainer = new Control();
        _titleScreenContainer.AnchorLeft = 0;
        _titleScreenContainer.AnchorRight = 1;
        _titleScreenContainer.AnchorTop = 0;
        _titleScreenContainer.AnchorBottom = 1;
        _titleScreenContainer.OffsetLeft = 0;
        _titleScreenContainer.OffsetRight = 0;
        _titleScreenContainer.OffsetTop = 0;
        _titleScreenContainer.OffsetBottom = 0;
        AddChild(_titleScreenContainer);

        // Center split layout: Title on top, left aligned menu options and vault graphic on right
        var mainLayout = new VBoxContainer();
        mainLayout.AnchorLeft = 0;
        mainLayout.AnchorRight = 1;
        mainLayout.AnchorTop = 0;
        mainLayout.AnchorBottom = 1;
        mainLayout.AddThemeConstantOverride("separation", 30);
        _titleScreenContainer.AddChild(mainLayout);

        // Top spacer
        var topSpacer = new Control();
        topSpacer.CustomMinimumSize = new Vector2(0, 40);
        mainLayout.AddChild(topSpacer);

        // ==========================================
        // HEADER: SID MEIER'S CIVILIZATION III COMPLETE style
        // ==========================================
        var headerVBox = new VBoxContainer();
        headerVBox.AddThemeConstantOverride("separation", -5);
        headerVBox.Alignment = BoxContainer.AlignmentMode.Center;
        mainLayout.AddChild(headerVBox);

        var subTitle = new Label();
        subTitle.Text = "S I D   M E I E R ' S";
        subTitle.HorizontalAlignment = HorizontalAlignment.Center;
        subTitle.AddThemeFontSizeOverride("font_size", 16);
        subTitle.AddThemeColorOverride("font_color", new Color(0.92f, 0.75f, 0.3f)); // Bronze-gold
        headerVBox.AddChild(subTitle);

        var mainTitle = new Label();
        mainTitle.Text = "CIVILIZATION III";
        mainTitle.HorizontalAlignment = HorizontalAlignment.Center;
        mainTitle.AddThemeFontSizeOverride("font_size", 72);
        mainTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.08f, 0.08f)); // Rich red
        // Add a nice subtle gold outline to the main title
        mainTitle.AddThemeColorOverride("font_outline_color", new Color(0.92f, 0.75f, 0.3f));
        mainTitle.AddThemeConstantOverride("outline_size", 6);
        headerVBox.AddChild(mainTitle);

        var subTitleComplete = new Label();
        subTitleComplete.Text = "C O M P L E T E";
        subTitleComplete.HorizontalAlignment = HorizontalAlignment.Center;
        subTitleComplete.AddThemeFontSizeOverride("font_size", 22);
        subTitleComplete.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.85f)); // Metallic silver
        subTitleComplete.AddThemeColorOverride("font_outline_color", new Color(0.2f, 0.2f, 0.22f));
        subTitleComplete.AddThemeConstantOverride("outline_size", 3);
        headerVBox.AddChild(subTitleComplete);

        // Middle layout: Split into Left Button List and Right decorative Vault Wheel
        var bodyHBox = new HBoxContainer();
        bodyHBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        bodyHBox.AddThemeConstantOverride("separation", 100);
        bodyHBox.Alignment = BoxContainer.AlignmentMode.Center;
        mainLayout.AddChild(bodyHBox);

        // LEFT: Vertically stacked options matching Civ 3 menu
        var menuVBox = new VBoxContainer();
        menuVBox.AddThemeConstantOverride("separation", 4);
        menuVBox.Alignment = BoxContainer.AlignmentMode.Center;
        bodyHBox.AddChild(menuVBox);

        var saveSystem = new JsonSaveSystem();
        bool hasSave = saveSystem.SaveExists("save_slot_1");

        // 11 Classic Civ 3 Complete items
        AddClassicMenuButton(menuVBox, "Play Last World", false, null);
        AddClassicMenuButton(menuVBox, "New Game", true, OnNewGameClicked);
        AddClassicMenuButton(menuVBox, "Quick Start", false, null);
        AddClassicMenuButton(menuVBox, "Load Game", hasSave, OnLoadGameClicked, !hasSave ? "NO SAVED GAME" : null);
        AddClassicMenuButton(menuVBox, "Conquests!", false, null);
        AddClassicMenuButton(menuVBox, "Civ-Content", false, null);
        AddClassicMenuButton(menuVBox, "Hall of Fame", false, null);
        AddClassicMenuButton(menuVBox, "Preferences", false, null);
        AddClassicMenuButton(menuVBox, "Multiplayer", false, null);
        AddClassicMenuButton(menuVBox, "Credits", false, null);
        AddClassicMenuButton(menuVBox, "Exit", true, OnExitClicked);

        // RIGHT: Decorative metal Vault Wheel to replicate the authentic vault background
        var vaultContainer = new CenterContainer();
        vaultContainer.CustomMinimumSize = new Vector2(300, 300);
        bodyHBox.AddChild(vaultContainer);

        var vaultPanel = new Panel();
        vaultPanel.CustomMinimumSize = new Vector2(240, 240);
        vaultPanel.AddThemeStyleboxOverride("panel", CreateCirclePanelStyle(
            new Color(0.12f, 0.12f, 0.15f), new Color(0.35f, 0.32f, 0.3f), 4));
        vaultContainer.AddChild(vaultPanel);

        // Inside vault panel, draw crossbars (handles)
        for (int i = 0; i < 3; i++)
        {
            var bar = new ColorRect();
            bar.Color = new Color(0.28f, 0.26f, 0.25f);
            bar.CustomMinimumSize = new Vector2(210, 16);
            bar.PivotOffset = new Vector2(105, 8);
            bar.RotationDegrees = i * 60;
            // Center inside the panel
            bar.Position = new Vector2(120 - 105, 120 - 8);
            vaultPanel.AddChild(bar);
        }

        // Central metal knob
        var centerKnob = new Panel();
        centerKnob.CustomMinimumSize = new Vector2(70, 70);
        centerKnob.Position = new Vector2(120 - 35, 120 - 35);
        centerKnob.AddThemeStyleboxOverride("panel", CreateCirclePanelStyle(
            new Color(0.22f, 0.22f, 0.25f), new Color(0.6f, 0.55f, 0.5f), 3));
        vaultPanel.AddChild(centerKnob);

        // Core bolt
        var coreBolt = new Panel();
        coreBolt.CustomMinimumSize = new Vector2(24, 24);
        coreBolt.Position = new Vector2(35 - 12, 35 - 12);
        coreBolt.AddThemeStyleboxOverride("panel", CreateCirclePanelStyle(
            new Color(0.4f, 0.4f, 0.42f), new Color(0.8f, 0.75f, 0.7f), 2));
        centerKnob.AddChild(coreBolt);

        // Footer info (Copyright look)
        var footer = new Label();
        footer.Text = "© 2026 CivGame. Built under Godot Engine & C#. Original aesthetics inspired by Firaxis Games.";
        footer.HorizontalAlignment = HorizontalAlignment.Center;
        footer.AddThemeFontSizeOverride("font_size", 11);
        footer.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.42f, 0.8f));
        mainLayout.AddChild(footer);

        var bottomSpacer = new Control();
        bottomSpacer.CustomMinimumSize = new Vector2(0, 15);
        mainLayout.AddChild(bottomSpacer);
    }

    private void AddClassicMenuButton(Container parent, string text, bool enabled, Action? action, string? statusSuffix = null)
    {
        var hBox = new HBoxContainer();
        hBox.AddThemeConstantOverride("separation", 15);
        parent.AddChild(hBox);

        // Bullet point: Metallic sphere
        var bullet = new Panel();
        bullet.CustomMinimumSize = new Vector2(16, 16);
        bullet.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        
        Color bulletBg = enabled ? new Color(0.65f, 0.65f, 0.7f) : new Color(0.25f, 0.25f, 0.28f);
        Color bulletBorder = enabled ? new Color(0.92f, 0.75f, 0.3f, 0.9f) : new Color(0.15f, 0.15f, 0.17f);
        bullet.AddThemeStyleboxOverride("panel", CreateCirclePanelStyle(bulletBg, bulletBorder, 2));
        hBox.AddChild(bullet);

        // Button itself
        var btn = new Button();
        btn.Text = string.IsNullOrEmpty(statusSuffix) ? text : $"{text} ({statusSuffix})";
        btn.Flat = true;
        btn.CustomMinimumSize = new Vector2(250, 32);
        btn.Alignment = HorizontalAlignment.Left;
        btn.AddThemeFontSizeOverride("font_size", 18);

        // Standard custom styles to make it clean
        btn.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());

        if (enabled)
        {
            btn.AddThemeColorOverride("font_color", new Color(0.72f, 0.72f, 0.75f));
            btn.AddThemeColorOverride("font_hover_color", new Color(0.98f, 0.95f, 0.9f));
            btn.AddThemeColorOverride("font_pressed_color", new Color(0.92f, 0.75f, 0.3f));
            
            // Connect pressed signal
            if (action != null)
            {
                btn.Pressed += action;
            }

            // Interactive bullet light-up on hover
            btn.MouseEntered += () =>
            {
                bullet.AddThemeStyleboxOverride("panel", CreateCirclePanelStyle(
                    new Color(0.92f, 0.75f, 0.3f), Colors.White, 2));
            };
            btn.MouseExited += () =>
            {
                bullet.AddThemeStyleboxOverride("panel", CreateCirclePanelStyle(bulletBg, bulletBorder, 2));
            };
        }
        else
        {
            btn.Disabled = true;
            btn.AddThemeColorOverride("font_disabled_color", new Color(0.38f, 0.38f, 0.40f));
        }

        hBox.AddChild(btn);
    }

    private void BuildMapSelectionScreen()
    {
        _mapSelectionContainer = new CenterContainer();
        _mapSelectionContainer.AnchorLeft = 0;
        _mapSelectionContainer.AnchorRight = 1;
        _mapSelectionContainer.AnchorTop = 0;
        _mapSelectionContainer.AnchorBottom = 1;
        _mapSelectionContainer.OffsetLeft = 0;
        _mapSelectionContainer.OffsetRight = 0;
        _mapSelectionContainer.OffsetTop = 0;
        _mapSelectionContainer.OffsetBottom = 0;
        _mapSelectionContainer.Visible = false; // Hidden initially
        AddChild(_mapSelectionContainer);

        // Main card panel
        var card = new PanelContainer();
        card.CustomMinimumSize = new Vector2(620, 520);
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.07f, 0.07f, 0.10f, 0.98f),
            new Color(0.95f, 0.72f, 0.12f, 0.9f), 3, 10));
        _mapSelectionContainer.AddChild(card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_top", 28);
        margin.AddThemeConstantOverride("margin_bottom", 28);
        card.AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 16);
        margin.AddChild(mainVBox);

        // Title
        var title = new Label();
        title.Text = "⚔️  CIVILIZATION  ⚔️";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        mainVBox.AddChild(title);

        // Subtitle
        var subtitle = new Label();
        subtitle.Text = "SELECT MAP SIZE";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f, 0.8f));
        mainVBox.AddChild(subtitle);

        // Separator
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 4);
        mainVBox.AddChild(sep);

        // Map size buttons row
        var sizesHBox = new HBoxContainer();
        sizesHBox.AddThemeConstantOverride("separation", 8);
        sizesHBox.Alignment = BoxContainer.AlignmentMode.Center;

        _sizeButtons = new Button[MapSizes.Length];
        for (int i = 0; i < MapSizes.Length; i++)
        {
            int sizeIdx = i; // capture for closure
            var btn = new Button();
            btn.Text = MapSizes[i].Name;
            btn.CustomMinimumSize = new Vector2(95, 42);
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.Pressed += () => SelectSize(sizeIdx);
            sizesHBox.AddChild(btn);
            _sizeButtons[i] = btn;
        }
        mainVBox.AddChild(sizesHBox);

        // Description label
        _descriptionLabel = new Label();
        _descriptionLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _descriptionLabel.AddThemeFontSizeOverride("font_size", 13);
        _descriptionLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f, 0.9f));
        _descriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        mainVBox.AddChild(_descriptionLabel);

        // Dimensions label
        _dimensionsLabel = new Label();
        _dimensionsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dimensionsLabel.AddThemeFontSizeOverride("font_size", 12);
        _dimensionsLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f, 0.7f));
        mainVBox.AddChild(_dimensionsLabel);

        // Separator
        var sep2 = new HSeparator();
        sep2.AddThemeConstantOverride("separation", 4);
        mainVBox.AddChild(sep2);

        // Civilizations selection row
        var civHBox = new HBoxContainer();
        civHBox.AddThemeConstantOverride("separation", 12);
        civHBox.Alignment = BoxContainer.AlignmentMode.Center;

        var playerCivLabel = new Label();
        playerCivLabel.Text = "You:";
        playerCivLabel.AddThemeFontSizeOverride("font_size", 14);
        playerCivLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        civHBox.AddChild(playerCivLabel);

        _playerCivInput = new OptionButton();
        _playerCivInput.CustomMinimumSize = new Vector2(160, 32);
        _playerCivInput.AddThemeFontSizeOverride("font_size", 13);
        civHBox.AddChild(_playerCivInput);

        var aiCivLabel = new Label();
        aiCivLabel.Text = "Rival:";
        aiCivLabel.AddThemeFontSizeOverride("font_size", 14);
        aiCivLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.08f, 0.08f));
        civHBox.AddChild(aiCivLabel);

        _aiCivInput = new OptionButton();
        _aiCivInput.CustomMinimumSize = new Vector2(160, 32);
        _aiCivInput.AddThemeFontSizeOverride("font_size", 13);
        civHBox.AddChild(_aiCivInput);

        // Populate dropdowns with registered civilizations
        int defaultPlayerIdx = 0;
        int defaultAiIdx = 0;
        int idx = 0;
        foreach (var civ in CivilizationRegistry.BaseGame)
        {
            _playerCivInput.AddItem($"{civ.Name} ({civ.LeaderName})");
            _playerCivInput.SetItemMetadata(idx, civ.Id);
            _aiCivInput.AddItem($"{civ.Name} ({civ.LeaderName})");
            _aiCivInput.SetItemMetadata(idx, civ.Id);

            if (civ.Id == "rome") defaultPlayerIdx = idx;
            if (civ.Id == "babylon") defaultAiIdx = idx;
            idx++;
        }
        foreach (var civ in CivilizationRegistry.PlayTheWorld)
        {
            _playerCivInput.AddItem($"{civ.Name} ({civ.LeaderName})");
            _playerCivInput.SetItemMetadata(idx, civ.Id);
            _aiCivInput.AddItem($"{civ.Name} ({civ.LeaderName})");
            _aiCivInput.SetItemMetadata(idx, civ.Id);

            if (civ.Id == "rome") defaultPlayerIdx = idx;
            if (civ.Id == "babylon") defaultAiIdx = idx;
            idx++;
        }
        foreach (var civ in CivilizationRegistry.Conquests)
        {
            _playerCivInput.AddItem($"{civ.Name} ({civ.LeaderName})");
            _playerCivInput.SetItemMetadata(idx, civ.Id);
            _aiCivInput.AddItem($"{civ.Name} ({civ.LeaderName})");
            _aiCivInput.SetItemMetadata(idx, civ.Id);

            if (civ.Id == "rome") defaultPlayerIdx = idx;
            if (civ.Id == "babylon") defaultAiIdx = idx;
            idx++;
        }

        _playerCivInput.Selected = defaultPlayerIdx;
        _aiCivInput.Selected = defaultAiIdx;

        mainVBox.AddChild(civHBox);

        // Seed row
        var seedHBox = new HBoxContainer();
        seedHBox.AddThemeConstantOverride("separation", 12);
        seedHBox.Alignment = BoxContainer.AlignmentMode.Center;

        var seedLabel = new Label();
        seedLabel.Text = "Map Seed:";
        seedLabel.AddThemeFontSizeOverride("font_size", 14);
        seedLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        seedHBox.AddChild(seedLabel);

        _seedInput = new SpinBox();
        _seedInput.MinValue = 1;
        _seedInput.MaxValue = 99999;
        _seedInput.Step = 1;
        _seedInput.Value = _seed;
        _seedInput.CustomMinimumSize = new Vector2(130, 0);
        _seedInput.AddThemeFontSizeOverride("font_size", 14);
        seedHBox.AddChild(_seedInput);

        var randomBtn = new Button();
        randomBtn.Text = "🎲 Random";
        randomBtn.CustomMinimumSize = new Vector2(90, 32);
        randomBtn.AddThemeFontSizeOverride("font_size", 12);
        randomBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(
            new Color(0.2f, 0.2f, 0.25f), new Color(0.5f, 0.5f, 0.5f, 0.5f), 1, 4));
        randomBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(
            new Color(0.3f, 0.3f, 0.35f), Colors.White, 1, 4));
        randomBtn.Pressed += () =>
        {
            _seed = new Random().Next(1, 99999);
            if (_seedInput != null) _seedInput.Value = _seed;
        };
        seedHBox.AddChild(randomBtn);

        mainVBox.AddChild(seedHBox);

        // Spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 8);
        mainVBox.AddChild(spacer);

        // Action Buttons Row (BACK and START)
        var actionsHBox = new HBoxContainer();
        actionsHBox.AddThemeConstantOverride("separation", 16);
        actionsHBox.Alignment = BoxContainer.AlignmentMode.Center;
        mainVBox.AddChild(actionsHBox);

        // BACK button
        var backBtn = new Button();
        backBtn.Text = "◀  BACK";
        backBtn.CustomMinimumSize = new Vector2(130, 48);
        backBtn.AddThemeFontSizeOverride("font_size", 16);
        backBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(
            new Color(0.22f, 0.22f, 0.25f), new Color(0.5f, 0.5f, 0.5f, 0.6f), 2, 6));
        backBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(
            new Color(0.28f, 0.28f, 0.32f), Colors.White, 2, 6));
        backBtn.Pressed += OnBackToTitleClicked;
        actionsHBox.AddChild(backBtn);

        // START GAME button
        var startBtn = new Button();
        startBtn.Text = "⚔️  START NEW GAME  ⚔️";
        startBtn.CustomMinimumSize = new Vector2(280, 48);
        startBtn.AddThemeFontSizeOverride("font_size", 16);
        startBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(
            new Color(0.12f, 0.55f, 0.22f), new Color(0.95f, 0.72f, 0.12f, 0.9f), 2, 6));
        startBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(
            new Color(0.15f, 0.65f, 0.28f), new Color(1f, 0.85f, 0.3f), 3, 6));
        startBtn.Pressed += OnStartPressed;
        actionsHBox.AddChild(startBtn);

        // Apply initial selection
        SelectSize(_selectedIndex);
    }

    private void SelectSize(int index)
    {
        _selectedIndex = index;

        // Update button visuals
        for (int i = 0; i < _sizeButtons.Length; i++)
        {
            bool selected = (i == index);
            _sizeButtons[i].AddThemeStyleboxOverride("normal", CreatePanelStyle(
                selected ? new Color(0.15f, 0.35f, 0.55f) : new Color(0.15f, 0.15f, 0.2f),
                selected ? new Color(0.95f, 0.72f, 0.12f, 0.95f) : new Color(0.4f, 0.4f, 0.4f, 0.5f),
                selected ? 2 : 1, 5));
            _sizeButtons[i].AddThemeStyleboxOverride("hover", CreatePanelStyle(
                selected ? new Color(0.2f, 0.4f, 0.6f) : new Color(0.22f, 0.22f, 0.28f),
                selected ? new Color(1f, 0.85f, 0.3f) : Colors.White,
                2, 5));
            _sizeButtons[i].AddThemeColorOverride("font_color",
                selected ? new Color(0.95f, 0.72f, 0.12f) : new Color(0.75f, 0.75f, 0.75f));
        }

        // Update description
        var opt = MapSizes[index];
        if (_descriptionLabel != null)
            _descriptionLabel.Text = opt.Description;
        if (_dimensionsLabel != null)
            _dimensionsLabel.Text = $"Map Dimensions: {opt.Width} × {opt.Height}  ({opt.Width * opt.Height:N0} tiles)";
    }

    private void OnNewGameClicked()
    {
        if (_titleScreenContainer != null) _titleScreenContainer.Visible = false;
        if (_mapSelectionContainer != null) _mapSelectionContainer.Visible = true;
    }

    private void OnBackToTitleClicked()
    {
        if (_mapSelectionContainer != null) _mapSelectionContainer.Visible = false;
        if (_titleScreenContainer != null) _titleScreenContainer.Visible = true;
    }

    private void OnLoadGameClicked()
    {
        OnLoadGame?.Invoke();
    }

    private void OnExitClicked()
    {
        GetTree().Quit();
    }

    private void OnStartPressed()
    {
        var opt = MapSizes[_selectedIndex];
        int seed = (int)(_seedInput?.Value ?? _seed);
        
        string playerCivId = "rome";
        string aiCivId = "babylon";

        if (_playerCivInput != null && _playerCivInput.Selected >= 0)
        {
            playerCivId = (string)_playerCivInput.GetItemMetadata(_playerCivInput.Selected);
        }
        if (_aiCivInput != null && _aiCivInput.Selected >= 0)
        {
            aiCivId = (string)_aiCivInput.GetItemMetadata(_aiCivInput.Selected);
        }

        OnStartGame?.Invoke(opt.Width, opt.Height, seed, playerCivId, aiCivId);
    }

    private static StyleBoxFlat CreatePanelStyle(Color bg, Color border, float borderWidth = 2, float cornerRadius = 6)
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

    private static StyleBoxFlat CreateCirclePanelStyle(Color bg, Color border, float borderWidth = 2)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = (int)borderWidth,
            BorderWidthTop = (int)borderWidth,
            BorderWidthRight = (int)borderWidth,
            BorderWidthBottom = (int)borderWidth,
            CornerRadiusTopLeft = 1000, // Makes it a perfect circle
            CornerRadiusTopRight = 1000,
            CornerRadiusBottomLeft = 1000,
            CornerRadiusBottomRight = 1000,
            AntiAliasing = true
        };
    }

    private record struct MapSizeOption(string Name, int Width, int Height, string Description);
}
