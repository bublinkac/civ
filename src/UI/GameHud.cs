using System;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

public partial class GameHud : CanvasLayer
{
    // Actions delegate
    public event Action<string>? OnActionTriggered;
    public event Action<int, int>? OnMinimapTileSelected;

    private GameSimulation? _sim;

    // Sub-panels
    private PanelContainer? _bottomDock;
    private MinimapCtrl? _minimap;
    private HBoxContainer? _actionsBox;
    private Label? _detailsLabel;
    private Button? _endTurnButton;

    // Top buttons and modals
    private PanelContainer? _topBar;
    private PanelContainer? _civilopediaModal;
    private PanelContainer? _gameMenuModal;

    // Game End overlay and labels
    private PanelContainer? _gameEndOverlay;
    private Label? _endGameTitle;
    private Label? _endGameReason;
    private Label? _endGameStats;

    // Confirmation Dialog modal and labels
    private PanelContainer? _confirmModal;
    private Label? _confirmTitleLabel;
    private Label? _confirmMessageLabel;
    private Button? _confirmYesButton;
    private Button? _confirmNoButton;
    private Action? _pendingConfirmAction;

    public override void _Ready()
    {
        Layer = 10;

        // Create the top bar (utility buttons)
        SetupTopBar();

        // Create the main bottom dock
        SetupBottomDock();

        // Create modal overlays
        SetupModals();
    }

    private void SetupTopBar()
    {
        _topBar = new PanelContainer();
        _topBar.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _topBar.Position = new Vector2(12, 12);
        _topBar.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.08f, 0.08f, 0.12f, 0.85f), new Color(0.95f, 0.72f, 0.12f, 0.9f), 2, 4));

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);

        var menuBtn = CreateStyledButton("Menu", new Color(0.18f, 0.18f, 0.22f));
        menuBtn.Pressed += () => ToggleModal(_gameMenuModal);
        hbox.AddChild(menuBtn);

        var advisorsBtn = CreateStyledButton("👥 Advisors", new Color(0.15f, 0.35f, 0.35f));
        advisorsBtn.Pressed += () => OnActionTriggered?.Invoke("open_advisors");
        hbox.AddChild(advisorsBtn);

        var civilopediaBtn = CreateStyledButton("📚 Civilopedia", new Color(0.15f, 0.3f, 0.15f));
        civilopediaBtn.Pressed += () => ToggleModal(_civilopediaModal);
        hbox.AddChild(civilopediaBtn);

        var researchBtn = CreateStyledButton("🔬 Tech Tree", new Color(0.15f, 0.15f, 0.35f));
        researchBtn.Pressed += () => OnActionTriggered?.Invoke("open_tech_tree");
        hbox.AddChild(researchBtn);

        _topBar.AddChild(hbox);
        AddChild(_topBar);
    }

    private void SetupBottomDock()
    {
        _bottomDock = new PanelContainer();
        // Manually set anchors + offsets for bottom-wide positioning.
        // SetAnchorsPreset(BottomWide) doesn't work here because the node
        // has no parent yet, so offset calculation yields all zeros → zero height
        // and the control is pushed off-screen by CustomMinimumSize.
        _bottomDock.AnchorLeft = 0;
        _bottomDock.AnchorRight = 1;
        _bottomDock.AnchorTop = 1;
        _bottomDock.AnchorBottom = 1;
        _bottomDock.OffsetTop = -160;
        _bottomDock.OffsetBottom = 0;
        _bottomDock.OffsetLeft = 0;
        _bottomDock.OffsetRight = 0;
        _bottomDock.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.06f, 0.06f, 0.09f, 0.92f), new Color(0.95f, 0.72f, 0.12f, 0.95f), 3, 0));

        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 12);
        marginContainer.AddThemeConstantOverride("margin_right", 12);
        marginContainer.AddThemeConstantOverride("margin_top", 10);
        marginContainer.AddThemeConstantOverride("margin_bottom", 10);

        var mainHBox = new HBoxContainer();
        mainHBox.AddThemeConstantOverride("separation", 16);

        // 1. Bottom-Left Panel: Minimap (Bordered Frame)
        var minimapPanel = new PanelContainer();
        minimapPanel.CustomMinimumSize = new Vector2(240, 140);
        minimapPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0, 0, 0, 0.8f), new Color(0.5f, 0.5f, 0.5f, 0.5f), 2, 4));
        
        _minimap = new MinimapCtrl();
        _minimap.OnTileSelected += (x, y) => OnMinimapTileSelected?.Invoke(x, y);
        _minimap.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _minimap.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        minimapPanel.AddChild(_minimap);
        mainHBox.AddChild(minimapPanel);

        // 2. Bottom-Center Panel: Actions Panel
        var actionsPanel = new PanelContainer();
        actionsPanel.CustomMinimumSize = new Vector2(280, 140);
        actionsPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.12f, 0.12f, 0.16f, 0.6f), new Color(0.95f, 0.72f, 0.12f, 0.5f), 1, 4));
        
        var actionsVBox = new VBoxContainer();
        actionsVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        actionsVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var actionsTitle = new Label();
        actionsTitle.Text = "ACTIONS";
        actionsTitle.HorizontalAlignment = HorizontalAlignment.Center;
        actionsTitle.AddThemeFontSizeOverride("font_size", 11);
        actionsTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f, 0.7f));
        actionsVBox.AddChild(actionsTitle);

        var actionsScroll = new ScrollContainer();
        actionsScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        actionsScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        _actionsBox = new HBoxContainer();
        _actionsBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _actionsBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _actionsBox.Alignment = BoxContainer.AlignmentMode.Center;
        _actionsBox.AddThemeConstantOverride("separation", 10);

        actionsScroll.AddChild(_actionsBox);
        actionsVBox.AddChild(actionsScroll);
        actionsPanel.AddChild(actionsVBox);
        mainHBox.AddChild(actionsPanel);

        // 3. Bottom-Right Panel: Selection Details & Turn Panel
        var detailsPanel = new PanelContainer();
        detailsPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        detailsPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.12f, 0.12f, 0.16f, 0.6f), new Color(0.95f, 0.72f, 0.12f, 0.5f), 1, 4));

        var detailsHBox = new HBoxContainer();
        detailsHBox.AddThemeConstantOverride("separation", 12);

        // Details label (left-aligned)
        _detailsLabel = new Label();
        _detailsLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _detailsLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _detailsLabel.AddThemeFontSizeOverride("font_size", 14);
        _detailsLabel.Text = "Select a unit or city on the map to begin.";
        detailsHBox.AddChild(_detailsLabel);

        // End turn button (right-aligned)
        var turnBox = new VBoxContainer();
        turnBox.Alignment = BoxContainer.AlignmentMode.Center;

        _endTurnButton = CreateStyledButton("END TURN", new Color(0.12f, 0.55f, 0.22f), new Color(0.95f, 0.72f, 0.12f));
        _endTurnButton.CustomMinimumSize = new Vector2(140, 55);
        _endTurnButton.AddThemeFontSizeOverride("font_size", 16);
        _endTurnButton.Pressed += () => OnActionTriggered?.Invoke("end_turn");
        turnBox.AddChild(_endTurnButton);

        detailsHBox.AddChild(turnBox);
        detailsPanel.AddChild(detailsHBox);
        mainHBox.AddChild(detailsPanel);

        marginContainer.AddChild(mainHBox);
        _bottomDock.AddChild(marginContainer);
        AddChild(_bottomDock);
    }

    private void SetupModals()
    {
        // 1. Civilopedia modal
        _civilopediaModal = new PanelContainer();
        _civilopediaModal.CustomMinimumSize = new Vector2(650, 420);
        _civilopediaModal.AnchorLeft = 0.5f;
        _civilopediaModal.AnchorRight = 0.5f;
        _civilopediaModal.AnchorTop = 0.5f;
        _civilopediaModal.AnchorBottom = 0.5f;
        _civilopediaModal.GrowHorizontal = Control.GrowDirection.Both;
        _civilopediaModal.GrowVertical = Control.GrowDirection.Both;
        _civilopediaModal.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.08f, 0.08f, 0.12f, 0.98f), new Color(0.95f, 0.72f, 0.12f, 0.95f), 3, 8));
        _civilopediaModal.Visible = false;

        var marginPedia = new MarginContainer();
        marginPedia.AddThemeConstantOverride("margin_left", 16);
        marginPedia.AddThemeConstantOverride("margin_right", 16);
        marginPedia.AddThemeConstantOverride("margin_top", 16);
        marginPedia.AddThemeConstantOverride("margin_bottom", 16);

        var vboxPedia = new VBoxContainer();
        vboxPedia.AddThemeConstantOverride("separation", 12);

        var titleLabel = new Label();
        titleLabel.Text = "📚 CIVILOPEDIA - Strategy Guide";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        vboxPedia.AddChild(titleLabel);

        var tabContainer = new TabContainer();
        tabContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        // Tab: Terrain Yields
        var terrainLabel = new Label();
        terrainLabel.Text = "• GRASSLAND: Yields F:2 P:0 C:1. Excellent for food collection.\n" +
                           "• PLAINS: Yields F:1 P:1 C:1. Balanced starting terrain.\n" +
                           "• MOUNTAIN: Yields F:0 P:1 C:0. Impassable for movement, but worked for Production.\n" +
                           "• DESERT: Yields F:0 P:1 C:0. Dry terrain with minimal yields.\n" +
                           "• OCEAN: Yields F:1 P:0 C:2. Impassable for land units, contains high commerce.";
        terrainLabel.AddThemeFontSizeOverride("font_size", 14);
        terrainLabel.Name = "Terrain & Yields";
        tabContainer.AddChild(terrainLabel);

        // Tab: Improvements
        var improvementsLabel = new Label();
        improvementsLabel.Text = "Workers can build tile improvements to permanently boost tile yields:\n" +
                                 "• FARM [F]: Adds +1 Food. Buildable on Grassland / Plains.\n" +
                                 "• MINE [M]: Adds +1 Production. Buildable on Desert / Mountains.\n" +
                                 "• PLANTATION [L]: Adds +1 Commerce. Buildable on Grassland / Plains.\n\n" +
                                 "Note: Improvements take multiple turns to build (Farm: 3, Mine: 4, Plantation: 3).";
        improvementsLabel.AddThemeFontSizeOverride("font_size", 14);
        improvementsLabel.Name = "Improvements";
        tabContainer.AddChild(improvementsLabel);

        // Tab: Tech Tree
        var techLabel = new Label();
        techLabel.Text = "Commerce generated by cities is converted 1:1 into Science. Cycle research using T.\n" +
                        "• POTTERY (15 Sci): Unlocks Granary building.\n" +
                        "• CEREMONIAL BURIAL (15 Sci): Unlocks Monument building.\n" +
                        "• BRONZE WORKING (25 Sci): Unlocks Archer combat unit.\n\n" +
                        "MESSAGES: Granary preserves 50% food on city growth. Monument claims a massive 5x5 border territory!";
        techLabel.AddThemeFontSizeOverride("font_size", 14);
        techLabel.Name = "Technologies";
        tabContainer.AddChild(techLabel);

        // Tab: Diplomacy
        var diplomacyLabel = new Label();
        diplomacyLabel.Text = "• FACTIONS: Player, Barbarians, and AI Rival.\n" +
                               "• PEACE & WAR: Factions start at peace. Stacking on top of peaceful units is blocked.\n" +
                               "• TERRITORY INTRUSION: Crossing into another nation's border immediately triggers WAR!\n" +
                               "• COMBAT: Engagement calculates attack vs defense ratios. Units below 0 HP are destroyed.";
        diplomacyLabel.AddThemeFontSizeOverride("font_size", 14);
        diplomacyLabel.Name = "Diplomacy & Combat";
        tabContainer.AddChild(diplomacyLabel);

        vboxPedia.AddChild(tabContainer);

        var closePediaBtn = CreateStyledButton("CLOSE GUIDE", new Color(0.6f, 0.2f, 0.2f));
        closePediaBtn.Pressed += () => _civilopediaModal.Visible = false;
        closePediaBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        closePediaBtn.CustomMinimumSize = new Vector2(160, 36);
        vboxPedia.AddChild(closePediaBtn);

        marginPedia.AddChild(vboxPedia);
        _civilopediaModal.AddChild(marginPedia);
        AddChild(_civilopediaModal);

        // 2. Game Menu modal
        _gameMenuModal = new PanelContainer();
        _gameMenuModal.CustomMinimumSize = new Vector2(250, 400);
        _gameMenuModal.AnchorLeft = 0.5f;
        _gameMenuModal.AnchorRight = 0.5f;
        _gameMenuModal.AnchorTop = 0.5f;
        _gameMenuModal.AnchorBottom = 0.5f;
        _gameMenuModal.GrowHorizontal = Control.GrowDirection.Both;
        _gameMenuModal.GrowVertical = Control.GrowDirection.Both;
        _gameMenuModal.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.08f, 0.08f, 0.12f, 0.98f), new Color(0.95f, 0.72f, 0.12f, 0.95f), 3, 8));
        _gameMenuModal.Visible = false;

        var marginMenu = new MarginContainer();
        marginMenu.AddThemeConstantOverride("margin_left", 20);
        marginMenu.AddThemeConstantOverride("margin_right", 20);
        marginMenu.AddThemeConstantOverride("margin_top", 20);
        marginMenu.AddThemeConstantOverride("margin_bottom", 20);

        var vboxMenu = new VBoxContainer();
        vboxMenu.AddThemeConstantOverride("separation", 12);
        vboxMenu.Alignment = BoxContainer.AlignmentMode.Center;

        var menuTitle = new Label();
        menuTitle.Text = "GAME MENU";
        menuTitle.HorizontalAlignment = HorizontalAlignment.Center;
        menuTitle.AddThemeFontSizeOverride("font_size", 18);
        menuTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        vboxMenu.AddChild(menuTitle);

        var resumeBtn = CreateStyledButton("Resume Game", new Color(0.2f, 0.2f, 0.25f));
        resumeBtn.Pressed += () => _gameMenuModal.Visible = false;
        vboxMenu.AddChild(resumeBtn);

        var newGameBtn = CreateStyledButton("⚔️ New Game", new Color(0.12f, 0.5f, 0.2f));
        newGameBtn.Pressed += () => {
            ShowConfirmation(
                "NEW GAME", 
                "Are you sure you want to start a new game?\nAll current unsaved progress will be permanently lost.", 
                () => {
                    _gameMenuModal.Visible = false;
                    GetTree().ReloadCurrentScene();
                }
            );
        };
        vboxMenu.AddChild(newGameBtn);

        var saveBtn = CreateStyledButton("💾 Save Game", new Color(0.12f, 0.35f, 0.45f));
        saveBtn.Pressed += () => {
            _gameMenuModal.Visible = false;
            OnActionTriggered?.Invoke("save_game");
            ShowInfoPopup("SAVE COMPLETE", "Your game state has been saved successfully to Slot 1.");
        };
        vboxMenu.AddChild(saveBtn);

        var loadBtn = CreateStyledButton("📂 Load Game", new Color(0.35f, 0.2f, 0.45f));
        loadBtn.Pressed += () => {
            ShowConfirmation(
                "LOAD GAME", 
                "Are you sure you want to load the saved game?\nYour current unsaved game progress will be lost.", 
                () => OnActionTriggered?.Invoke("load_game")
            );
        };
        vboxMenu.AddChild(loadBtn);

        var debugBtn = CreateStyledButton("🔧 DEBUG: Ignore Prerequisites", new Color(0.5f, 0.2f, 0.5f));
        debugBtn.Pressed += () => {
            GameSimulation.DebugIgnorePrerequisites = !GameSimulation.DebugIgnorePrerequisites;
            debugBtn.Text = GameSimulation.DebugIgnorePrerequisites
                ? "🔧 DEBUG: Prerequisites IGNORED (ON)"
                : "🔧 DEBUG: Ignore Prerequisites (OFF)";
            ShowInfoPopup("DEBUG MODE", GameSimulation.DebugIgnorePrerequisites
                ? "All building/wonder prerequisites are now IGNORED. You can build anything!"
                : "All building/wonder prerequisites are now ENFORCED normally.");
        };
        vboxMenu.AddChild(debugBtn);

        var restartBtn = CreateStyledButton("Restart Map", new Color(0.15f, 0.4f, 0.15f));
        restartBtn.Pressed += () => {
            ShowConfirmation(
                "RESTART MAP",
                "Are you sure you want to restart the map?\nThe entire current map and progress will be permanently lost.",
                () => {
                    _gameMenuModal.Visible = false;
                    GetTree().ReloadCurrentScene();
                }
            );
        };
        vboxMenu.AddChild(restartBtn);

        var exitBtn = CreateStyledButton("Exit Game", new Color(0.6f, 0.15f, 0.15f));
        exitBtn.Pressed += () => {
            ShowConfirmation(
                "EXIT TO DESKTOP", 
                "Are you sure you want to quit to desktop?\nAny unsaved progress will be permanently lost.", 
                () => GetTree().Quit()
            );
        };
        vboxMenu.AddChild(exitBtn);

        marginMenu.AddChild(vboxMenu);
        _gameMenuModal.AddChild(marginMenu);
        AddChild(_gameMenuModal);

        // 3. Game End Fullscreen Overlay
        _gameEndOverlay = new PanelContainer();
        _gameEndOverlay.AnchorLeft = 0;
        _gameEndOverlay.AnchorRight = 1;
        _gameEndOverlay.AnchorTop = 0;
        _gameEndOverlay.AnchorBottom = 1;
        _gameEndOverlay.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.04f, 0.04f, 0.06f, 0.96f), new Color(0.95f, 0.72f, 0.12f, 0.95f), 4, 0));
        _gameEndOverlay.Visible = false;

        var centerContainer = new CenterContainer();
        centerContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        centerContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var card = new PanelContainer();
        card.CustomMinimumSize = new Vector2(500, 380);
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.08f, 0.08f, 0.12f, 0.98f), new Color(0.95f, 0.72f, 0.12f, 0.95f), 3, 8));

        var marginCard = new MarginContainer();
        marginCard.AddThemeConstantOverride("margin_left", 24);
        marginCard.AddThemeConstantOverride("margin_right", 24);
        marginCard.AddThemeConstantOverride("margin_top", 24);
        marginCard.AddThemeConstantOverride("margin_bottom", 24);

        var vboxCard = new VBoxContainer();
        vboxCard.AddThemeConstantOverride("separation", 16);

        _endGameTitle = new Label();
        _endGameTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _endGameTitle.AddThemeFontSizeOverride("font_size", 32);
        vboxCard.AddChild(_endGameTitle);

        _endGameReason = new Label();
        _endGameReason.HorizontalAlignment = HorizontalAlignment.Center;
        _endGameReason.AddThemeFontSizeOverride("font_size", 16);
        _endGameReason.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vboxCard.AddChild(_endGameReason);

        _endGameStats = new Label();
        _endGameStats.HorizontalAlignment = HorizontalAlignment.Center;
        _endGameStats.AddThemeFontSizeOverride("font_size", 14);
        vboxCard.AddChild(_endGameStats);

        var actionsHBox = new HBoxContainer();
        actionsHBox.Alignment = BoxContainer.AlignmentMode.Center;
        actionsHBox.AddThemeConstantOverride("separation", 16);

        var playAgainBtn = CreateStyledButton("PLAY AGAIN", new Color(0.12f, 0.55f, 0.22f), new Color(0.95f, 0.72f, 0.12f));
        playAgainBtn.CustomMinimumSize = new Vector2(160, 45);
        playAgainBtn.Pressed += () => {
            _gameEndOverlay.Visible = false;
            GetTree().ReloadCurrentScene();
        };
        actionsHBox.AddChild(playAgainBtn);

        var exitGameBtn = CreateStyledButton("EXIT GAME", new Color(0.6f, 0.15f, 0.15f), new Color(0.95f, 0.72f, 0.12f));
        exitGameBtn.CustomMinimumSize = new Vector2(160, 45);
        exitGameBtn.Pressed += () => GetTree().Quit();
        actionsHBox.AddChild(exitGameBtn);

        vboxCard.AddChild(actionsHBox);
        marginCard.AddChild(vboxCard);
        card.AddChild(marginCard);
        centerContainer.AddChild(card);
        _gameEndOverlay.AddChild(centerContainer);
        AddChild(_gameEndOverlay);

        // 4. Confirmation Dialog Modal
        _confirmModal = new PanelContainer();
        _confirmModal.CustomMinimumSize = new Vector2(340, 200);
        _confirmModal.AnchorLeft = 0.5f;
        _confirmModal.AnchorRight = 0.5f;
        _confirmModal.AnchorTop = 0.5f;
        _confirmModal.AnchorBottom = 0.5f;
        _confirmModal.GrowHorizontal = Control.GrowDirection.Both;
        _confirmModal.GrowVertical = Control.GrowDirection.Both;
        _confirmModal.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.1f, 0.1f, 0.14f, 0.99f), new Color(0.95f, 0.72f, 0.12f, 0.95f), 3, 6));
        _confirmModal.Visible = false;

        var marginConfirm = new MarginContainer();
        marginConfirm.AddThemeConstantOverride("margin_left", 20);
        marginConfirm.AddThemeConstantOverride("margin_right", 20);
        marginConfirm.AddThemeConstantOverride("margin_top", 20);
        marginConfirm.AddThemeConstantOverride("margin_bottom", 20);

        var vboxConfirm = new VBoxContainer();
        vboxConfirm.AddThemeConstantOverride("separation", 14);

        _confirmTitleLabel = new Label();
        _confirmTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _confirmTitleLabel.AddThemeFontSizeOverride("font_size", 16);
        _confirmTitleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.72f, 0.12f));
        vboxConfirm.AddChild(_confirmTitleLabel);

        _confirmMessageLabel = new Label();
        _confirmMessageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _confirmMessageLabel.AddThemeFontSizeOverride("font_size", 13);
        _confirmMessageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vboxConfirm.AddChild(_confirmMessageLabel);

        var confirmButtonsHBox = new HBoxContainer();
        confirmButtonsHBox.Alignment = BoxContainer.AlignmentMode.Center;
        confirmButtonsHBox.AddThemeConstantOverride("separation", 16);

        _confirmYesButton = CreateStyledButton("CONFIRM", new Color(0.12f, 0.5f, 0.2f));
        _confirmYesButton.Pressed += () =>
        {
            _confirmModal.Visible = false;
            _pendingConfirmAction?.Invoke();
            _pendingConfirmAction = null;
        };
        confirmButtonsHBox.AddChild(_confirmYesButton);

        _confirmNoButton = CreateStyledButton("CANCEL", new Color(0.6f, 0.2f, 0.2f));
        _confirmNoButton.Pressed += () =>
        {
            _confirmModal.Visible = false;
            _pendingConfirmAction = null;
            if (_gameMenuModal != null) _gameMenuModal.Visible = true;
        };
        confirmButtonsHBox.AddChild(_confirmNoButton);

        vboxConfirm.AddChild(confirmButtonsHBox);
        marginConfirm.AddChild(vboxConfirm);
        _confirmModal.AddChild(marginConfirm);
        AddChild(_confirmModal);
    }

    private void ShowConfirmation(string title, string message, Action onConfirm)
    {
        if (_confirmModal == null || _confirmTitleLabel == null || _confirmMessageLabel == null || _confirmYesButton == null || _confirmNoButton == null) return;

        _confirmTitleLabel.Text = title;
        _confirmMessageLabel.Text = message;
        _confirmYesButton.Text = "CONFIRM";
        _confirmNoButton.Visible = true;
        _pendingConfirmAction = onConfirm;

        _confirmModal.Visible = true;
        if (_gameMenuModal != null) _gameMenuModal.Visible = false;
    }

    private void ShowInfoPopup(string title, string message)
    {
        if (_confirmModal == null || _confirmTitleLabel == null || _confirmMessageLabel == null || _confirmYesButton == null || _confirmNoButton == null) return;

        _confirmTitleLabel.Text = title;
        _confirmMessageLabel.Text = message;
        _confirmYesButton.Text = "OK";
        _confirmNoButton.Visible = false;
        _pendingConfirmAction = () => {
            if (_gameMenuModal != null) _gameMenuModal.Visible = true;
        };

        _confirmModal.Visible = true;
        if (_gameMenuModal != null) _gameMenuModal.Visible = false;
    }

    private void ToggleModal(PanelContainer? modal)
    {
        if (modal == null) return;
        modal.Visible = !modal.Visible;

        // Bring to front
        if (modal.Visible)
        {
            if (modal == _civilopediaModal && _gameMenuModal != null) _gameMenuModal.Visible = false;
            if (modal == _gameMenuModal && _civilopediaModal != null) _civilopediaModal.Visible = false;
        }
    }

    public void Refresh(GameSimulation sim, string? selectedUnitId, string? selectedCityId = null)
    {
        _sim = sim;
        
        // Refresh Minimap
        _minimap?.SetSimulation(sim);

        // Check for Game End
        if (sim.EndState != GameEndState.None && _gameEndOverlay != null && _endGameTitle != null && _endGameReason != null && _endGameStats != null)
        {
            _gameEndOverlay.Visible = true;

            bool isVictory = sim.EndState == GameEndState.VictoryDomination || 
                             sim.EndState == GameEndState.VictoryScience || 
                             sim.EndState == GameEndState.VictoryScore;

            _endGameTitle.Text = isVictory ? "🏆 VICTORY!" : "💀 DEFEAT!";
            _endGameTitle.AddThemeColorOverride("font_color", isVictory ? new Color(0.12f, 0.85f, 0.3f) : new Color(0.85f, 0.15f, 0.15f));

            _endGameReason.Text = sim.EndState switch
            {
                GameEndState.VictoryDomination => "VICTORY BY DOMINATION!\nYour glorious armies have captured the enemy capital and united the world under your banner.",
                GameEndState.VictoryScience => "SCIENTIFIC VICTORY!\nYour scholars have unlocked all secrets of the universe, leading humanity into a futuristic golden age.",
                GameEndState.VictoryScore => "VICTORY BY SCORE!\nTime has run out, and your civilization stands as the undisputed peak of global accomplishment.",
                GameEndState.DefeatDomination => "DEFEAT!\nYour cities have fallen, and your empire has been erased from history.",
                GameEndState.DefeatScore => "DEFEAT BY SCORE!\nTime expired, and the AI Rival achieved a superior score. Your legacy is lost to time.",
                _ => ""
            };

            int playerCitiesCount = sim.Cities.Count(c => c.Faction == Faction.Player);
            int playerPop = sim.Cities.Where(c => c.Faction == Faction.Player).Sum(c => c.Population);
            int playerUnitsCount = sim.Units.Count(u => u.Faction == Faction.Player);
            int playerScore = sim.CalculateScore(Faction.Player);

            int aiCitiesCount = sim.Cities.Count(c => c.Faction == Faction.AiRival);
            int aiPop = sim.Cities.Where(c => c.Faction == Faction.AiRival).Sum(c => c.Population);
            int aiUnitsCount = sim.Units.Count(u => u.Faction == Faction.AiRival);
            int aiScore = sim.CalculateScore(Faction.AiRival);

            _endGameStats.Text = $"--- FINAL STATISTICS (Turn {sim.TurnNumber}) ---\n\n" +
                                 $"PLAYER: {playerScore} pts  |  AI RIVAL: {aiScore} pts\n\n" +
                                 $"Cities Controlled: {playerCitiesCount} vs {aiCitiesCount}\n" +
                                 $"Total Population: {playerPop} vs {aiPop}\n" +
                                 $"Active Units: {playerUnitsCount} vs {aiUnitsCount}\n" +
                                 $"Technologies Researched: {sim.Research.ResearchedTechIds.Count} vs {Math.Min(3, sim.TurnNumber / 15)}";
        }

        // Clear existing action buttons
        if (_actionsBox != null)
        {
            foreach (var child in _actionsBox.GetChildren())
            {
                child.QueueFree();
            }
        }

        if (_detailsLabel == null) return;

        // 1. If a Unit is selected
        if (!string.IsNullOrEmpty(selectedUnitId))
        {
            Unit? unit = sim.Units.Find(u => u.Id == selectedUnitId);
            if (unit != null)
            {
                string factionStr = unit.Faction == Faction.Player ? "" : $" [{unit.Faction}]";
                string statusStr = "";
                if (unit.IsSleeping) statusStr = " | Status: SLEEPING";
                else if (unit.IsFortified) statusStr = " | Status: FORTIFIED (+25% Defense)";
                else if (unit.Type == UnitType.Worker && unit.IsWorkerBuilding()) statusStr = $" | Status: CONSTRUCTING {unit.ImprovementUnderConstruction!.Name.ToUpper()} ({unit.ConstructionTurnsRemaining} turns left)";

                string details = $"[UNIT] {unit.Type.ToString().ToUpper()}{factionStr}\n" +
                                 $"Position: ({unit.X}, {unit.Y}) | Health: {unit.Health}/{unit.MaxHealth} | MP: {unit.RemainingMovement}/{unit.MaxMovement}{statusStr}";

                var tile = sim.Map.GetTile(unit.X, unit.Y);
                if (tile != null)
                {
                    string impName = tile.Improvement != null ? $" ({tile.Improvement.Name})" : "";
                    string roadName = tile.HasRoad ? " [ROAD]" : "";
                    details += $"\nTerrain: {tile.Terrain.ToString().ToUpper()}{impName.ToUpper()}{roadName} | Yields: {tile.TotalYield}";
                }

                _detailsLabel.Text = details;

                // Populate action buttons if player unit
                if (unit.Faction == Faction.Player && _actionsBox != null)
                {
                    if (unit.IsSleeping || unit.IsFortified)
                    {
                        var wakeBtn = CreateActionButton("☀️", "Wake Up", "W", () => OnActionTriggered?.Invoke("wake"), new Color(0.15f, 0.3f, 0.45f));
                        _actionsBox.AddChild(wakeBtn);
                    }
                    else
                    {
                        if (unit.Type == UnitType.Worker && unit.IsWorkerBuilding())
                        {
                            var cancelBtn = CreateActionButton("❌", "Stop Work", "ESC", () => {
                                unit.CancelImprovement();
                                Refresh(sim, selectedUnitId, selectedCityId);
                            }, new Color(0.6f, 0.2f, 0.2f));
                            _actionsBox.AddChild(cancelBtn);
                        }
                        else
                        {
                            if (unit.Type == UnitType.Settler && sim.CanBuildCity(unit) && unit.HasMovementRemaining())
                            {
                                var btn = CreateActionButton("🏛️", "Found City", "B", () => OnActionTriggered?.Invoke("settle"), new Color(0.15f, 0.4f, 0.15f));
                                _actionsBox.AddChild(btn);
                            }
                            else if (unit.Type == UnitType.Worker && unit.HasMovementRemaining())
                            {
                                if (tile != null)
                                {
                                    if (tile.Improvement == null)
                                    {
                                        if (new Farm().CanBeBuiltOn(tile.Terrain))
                                        {
                                            var btn = CreateActionButton("🌾", "Irrigate", "I", () => OnActionTriggered?.Invoke("farm"), new Color(0.35f, 0.3f, 0.15f));
                                            _actionsBox.AddChild(btn);
                                        }
                                        if (new Mine().CanBeBuiltOn(tile.Terrain))
                                        {
                                            var btn = CreateActionButton("⛏️", "Build Mine", "M", () => OnActionTriggered?.Invoke("mine"), new Color(0.3f, 0.3f, 0.35f));
                                            _actionsBox.AddChild(btn);
                                        }
                                        if (new Plantation().CanBeBuiltOn(tile.Terrain))
                                        {
                                            var btn = CreateActionButton("🍇", "Plantation", "L", () => OnActionTriggered?.Invoke("plantation"), new Color(0.15f, 0.35f, 0.35f));
                                            _actionsBox.AddChild(btn);
                                        }
                                    }
                                    if (!tile.HasRoad)
                                    {
                                        var roadBtn = CreateActionButton("🛣️", "Build Road", "R", () => OnActionTriggered?.Invoke("road"), new Color(0.25f, 0.25f, 0.3f));
                                        _actionsBox.AddChild(roadBtn);
                                    }
                                }
                            }

                            // Standard actions for all awake units (Fortify & Sleep)
                            var fortifyBtn = CreateActionButton("🛡️", "Fortify", "F", () => OnActionTriggered?.Invoke("fortify"), new Color(0.3f, 0.15f, 0.15f));
                            _actionsBox.AddChild(fortifyBtn);

                            var sleepBtn = CreateActionButton("💤", "Sleep", "S", () => OnActionTriggered?.Invoke("sleep"), new Color(0.2f, 0.12f, 0.25f));
                            _actionsBox.AddChild(sleepBtn);
                        }
                    }
                }
                return;
            }
        }

        // 2. If a City is selected
        if (!string.IsNullOrEmpty(selectedCityId))
        {
            City? city = sim.Cities.Find(c => c.Id == selectedCityId);
            if (city != null)
            {
                string factionStr = city.Faction == Faction.Player ? "" : $" [{city.Faction}]";
                string projectInfo = city.CurrentProject == ProductionProject.None 
                    ? $"Idle (Stored Prod: {city.StoredProduction})" 
                    : $"{city.CurrentProject.ToString().ToUpper()} ({city.CurrentProductionProgress}/{city.GetProjectCost(city.CurrentProject)} Prod)";

                string foodSign = city.LastTurnNetFood >= 0 ? "+" : "";
                string foodTrend = $"{foodSign}{city.LastTurnNetFood}/turn";
                string buildingsList = city.Buildings.Count > 0 
                    ? string.Join(", ", city.Buildings.Select(b => b.Name.ToUpper())) 
                    : "NONE";

                _detailsLabel.Text = $"[CITY] {city.Name.ToUpper()}{factionStr} (Population: {city.Population})\n" +
                                     $"Food: {city.StoredFood}/{city.FoodNeededForGrowth} ({foodTrend}) | Stored Prod: {city.StoredProduction} | Commerce: {city.StoredCommerce}\n" +
                                     $"Active Queue: {projectInfo}\n" +
                                     $"Completed Structures: {buildingsList}";

                // Populate action button for player city
                if (city.Faction == Faction.Player && _actionsBox != null)
                {
                    var btn = CreateActionButton("🔄", "Cycle Project", "P", () => OnActionTriggered?.Invoke("cycle_production"), new Color(0.15f, 0.15f, 0.35f));
                    _actionsBox.AddChild(btn);
                }
                return;
            }
        }

        // 3. No selection (Empire Summary)
        int empCitiesCount = sim.Cities.Count(c => c.Faction == Faction.Player);
        int empPop = sim.Cities.Where(c => c.Faction == Faction.Player).Sum(c => c.Population);
        int empUnitsCount = sim.Units.Count(u => u.Faction == Faction.Player);
        string diplStr = sim.IsAtWarWithAi ? "🔴 WAR WITH AI RIVAL" : "Peace with AI Rival";

        _detailsLabel.Text = $"--- EMPIRE SUMMARY (Turn {sim.TurnNumber}/{GameSimulation.MaxTurnLimit}) ---\n" +
                             $"Diplomacy: {diplStr}  |  Cities: {empCitiesCount} (Total Pop: {empPop})  |  Active Units: {empUnitsCount}\n" +
                             $"Research: {sim.Research.GetResearchStatusString()}\n" +
                             $"Tip: Press [SPACE] or click END TURN to finish your turn and restore movement points.";
    }

    private Button CreateActionButton(string icon, string actionName, string hotkey, Action action, Color bg)
    {
        var btn = new Button();
        btn.Text = $"{icon} {actionName}\n[{hotkey}]";
        btn.CustomMinimumSize = new Vector2(110, 55);
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(bg, new Color(0.95f, 0.72f, 0.12f, 0.8f), 1, 4));
        btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(bg.Lightened(0.2f), new Color(1, 0.8f, 0.2f), 2, 4));
        btn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(bg.Darkened(0.2f), new Color(0.95f, 0.72f, 0.12f), 1, 4));
        btn.Pressed += () => action();
        return btn;
    }

    private Button CreateStyledButton(string text, Color bg, Color? border = null)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(90, 32);
        btn.AddThemeFontSizeOverride("font_size", 13);
        
        Color borderColor = border ?? bg.Lightened(0.3f);
        btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(bg, borderColor, 1, 4));
        btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(bg.Lightened(0.15f), Colors.White, 1.5f, 4));
        btn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(bg.Darkened(0.15f), borderColor, 1, 4));
        return btn;
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
}

// Sibling class: Custom painted lightweight Minimap Control
public partial class MinimapCtrl : Control
{
    public event Action<int, int>? OnTileSelected;
    private GameSimulation? _sim;

    public void SetSimulation(GameSimulation sim)
    {
        _sim = sim;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_sim == null) return;

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            HandleMinimapClick(mouseButton.Position);
        }
        else if (@event is InputEventMouseMotion mouseMotion && mouseMotion.ButtonMask == MouseButtonMask.Left)
        {
            HandleMinimapClick(mouseMotion.Position);
        }
    }

    private void HandleMinimapClick(Vector2 clickPos)
    {
        if (_sim == null) return;
        int w = _sim.Map.Width;
        int h = _sim.Map.Height;
        Vector2 size = Size;

        if (size.X <= 0 || size.Y <= 0) return;

        int tx = Mathf.Clamp((int)(clickPos.X / size.X * w), 0, w - 1);
        int ty = Mathf.Clamp((int)(clickPos.Y / size.Y * h), 0, h - 1);

        OnTileSelected?.Invoke(tx, ty);
    }

    public override void _Draw()
    {
        if (_sim == null) return;

        int w = _sim.Map.Width;
        int h = _sim.Map.Height;

        Vector2 size = Size;
        float scaleX = size.X / w;
        float scaleY = size.Y / h;

        // Draw terrain cells
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var fog = _sim.VisibilityGrid[x, y];
                if (fog == FogState.Unexplored)
                {
                    DrawRect(new Rect2(x * scaleX, y * scaleY, scaleX, scaleY), Colors.Black);
                    continue;
                }

                var tile = _sim.Map.GetTile(x, y);
                Color color = tile?.Terrain.Color ?? Colors.Black;

                if (fog == FogState.Shrouded)
                {
                    color = color.Darkened(0.5f);
                }

                DrawRect(new Rect2(x * scaleX, y * scaleY, scaleX, scaleY), color);
            }
        }

        // Draw cities (if explored)
        foreach (var city in _sim.Cities)
        {
            if (_sim.VisibilityGrid[city.X, city.Y] != FogState.Unexplored)
            {
                Color dotColor = city.Faction switch
                {
                    Faction.Player => Colors.Gold,
                    Faction.AiRival => new Color(0.7f, 0.2f, 1.0f), // Purple
                    _ => Colors.Red
                };
                DrawCircle(new Vector2(city.X * scaleX + scaleX / 2, city.Y * scaleY + scaleY / 2), scaleX, dotColor);
            }
        }

        // Draw units (if visible)
        foreach (var unit in _sim.Units)
        {
            if (_sim.VisibilityGrid[unit.X, unit.Y] == FogState.Visible)
            {
                Color dotColor = unit.Faction switch
                {
                    Faction.Player => Colors.White,
                    Faction.AiRival => Colors.Magenta,
                    Faction.Barbarian => Colors.Red,
                    _ => Colors.Gray
                };
                DrawCircle(new Vector2(unit.X * scaleX + scaleX / 2, unit.Y * scaleY + scaleY / 2), scaleX * 0.75f, dotColor);
            }
        }
    }
}
