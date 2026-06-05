using System;
using Godot;

namespace CivGame.UI;

/// <summary>
/// Main Menu screen displayed at game start. Allows the player to select
/// a map size (Civilization 3 style) and seed before starting a new game.
/// </summary>
public partial class MainMenu : CanvasLayer
{
    /// <summary>Fired when the player clicks START GAME. Args: width, height, seed.</summary>
    public event Action<int, int, int>? OnStartGame;

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
    public override void _Ready()
    {
        Layer = 20; // Above game HUD

        _seed = new Random().Next(1, 99999);

        // Full-screen dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.04f, 0.04f, 0.06f, 1.0f);
        bg.AnchorLeft = 0;
        bg.AnchorRight = 1;
        bg.AnchorTop = 0;
        bg.AnchorBottom = 1;
        bg.OffsetLeft = 0;
        bg.OffsetRight = 0;
        bg.OffsetTop = 0;
        bg.OffsetBottom = 0;
        AddChild(bg);

        // Center container for the menu card
        var center = new CenterContainer();
        center.AnchorLeft = 0;
        center.AnchorRight = 1;
        center.AnchorTop = 0;
        center.AnchorBottom = 1;
        center.OffsetLeft = 0;
        center.OffsetRight = 0;
        center.OffsetTop = 0;
        center.OffsetBottom = 0;
        center.GrowHorizontal = Control.GrowDirection.Both;
        center.GrowVertical = Control.GrowDirection.Both;
        AddChild(center);

        // Main card panel
        var card = new PanelContainer();
        card.CustomMinimumSize = new Vector2(620, 520);
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.07f, 0.07f, 0.10f, 0.98f),
            new Color(0.95f, 0.72f, 0.12f, 0.9f), 3, 10));
        center.AddChild(card);

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
            int idx = i; // capture for closure
            var btn = new Button();
            btn.Text = MapSizes[i].Name;
            btn.CustomMinimumSize = new Vector2(95, 42);
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.Pressed += () => SelectSize(idx);
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

        // START GAME button
        var startBtn = new Button();
        startBtn.Text = "⚔️  START NEW GAME  ⚔️";
        startBtn.CustomMinimumSize = new Vector2(300, 55);
        startBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        startBtn.AddThemeFontSizeOverride("font_size", 18);
        startBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(
            new Color(0.12f, 0.55f, 0.22f), new Color(0.95f, 0.72f, 0.12f, 0.9f), 2, 6));
        startBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(
            new Color(0.15f, 0.65f, 0.28f), new Color(1f, 0.85f, 0.3f), 3, 6));
        startBtn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(
            new Color(0.08f, 0.4f, 0.15f), new Color(0.95f, 0.72f, 0.12f), 2, 6));
        startBtn.Pressed += OnStartPressed;
        mainVBox.AddChild(startBtn);

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

    private void OnStartPressed()
    {
        var opt = MapSizes[_selectedIndex];
        int seed = (int)(_seedInput?.Value ?? _seed);
        OnStartGame?.Invoke(opt.Width, opt.Height, seed);
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

    private record struct MapSizeOption(string Name, int Width, int Height, string Description);
}
