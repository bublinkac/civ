using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CivGame.Core;

namespace CivGame.UI;

public enum CivilopediaCategory
{
    Units,
    Buildings,
    Wonders,
    Technologies,
    TerrainAndResources,
    Governments,
    Concepts
}

public class CivilopediaArticle
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public CivilopediaCategory Category { get; set; }
    public string SubCategory { get; set; } = "";
    public string Icon { get; set; } = "📖";
    public string Era { get; set; } = "";
    public Dictionary<string, string> KeyStats { get; set; } = new();
    public List<string> AbilityBadges { get; set; } = new();
    public string? RequiredTechId { get; set; }
    public string? RequiredTechName { get; set; }
    public List<string> RequiredResources { get; set; } = new();
    public string? UpgradesToId { get; set; }
    public string? UpgradesToName { get; set; }
    public string? UpgradesFromId { get; set; }
    public string? UpgradesFromName { get; set; }
    public List<(string TargetId, string Label)> RelatedArticles { get; set; } = new();
    public string StrategyAdvice { get; set; } = "";
    public string HistoricalLore { get; set; } = "";
}

/// <summary>
/// Modern, clean, and intuitive Civilopedia Dialog.
/// Replaces the legacy clunky interface with a responsive split-view, real-time search,
/// category pills, quick stat matrices, ability badges, and interactive cross-links.
/// </summary>
public partial class CivilopediaDialog : PanelContainer
{
    private readonly GameSimulation _sim;
    private readonly List<CivilopediaArticle> _articles = new();
    private CivilopediaCategory _currentCategory = CivilopediaCategory.Units;
    private string _currentSubFilter = "Všetky";
    private string _searchQuery = "";
    private CivilopediaArticle? _selectedArticle;

    // UI Nodes
    private LineEdit _searchBar = null!;
    private HBoxContainer _categoryBar = null!;
    private HBoxContainer _subFilterBar = null!;
    private VBoxContainer _articlesList = null!;
    private Label _resultsCountLabel = null!;
    private ScrollContainer _detailScroll = null!;
    private VBoxContainer _detailContent = null!;

    public event Action? OnClosed;

    public CivilopediaDialog(GameSimulation sim)
    {
        _sim = sim;
        Name = "CivilopediaDialog";

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        // Dark obsidian backdrop with subtle Civ-gold border
        AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.06f, 0.08f, 0.12f, 0.98f),
            new Color(0.85f, 0.68f, 0.22f, 0.95f),
            borderWidth: 2,
            cornerRadius: 8
        ));

        BuildAllArticles();
        SetupLayout();

        // Select first article in default category
        SelectCategory(CivilopediaCategory.Units);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Escape || keyEvent.Keycode == Key.F12)
            {
                Close();
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
        // 1. TOP HEADER BAR: Title, Search Bar, Close Button
        // ══════════════════════════════════════════════════════════
        var headerHBox = new HBoxContainer();
        headerHBox.AddThemeConstantOverride("separation", 16);
        headerHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var titleVBox = new VBoxContainer();
        titleVBox.AddThemeConstantOverride("separation", 0);
        var titleLabel = new Label
        {
            Text = "📚 CIVILOPEDIA",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.78f, 0.25f));
        titleVBox.AddChild(titleLabel);

        var subtitleLabel = new Label
        {
            Text = "Moderná encyklopédia & taktický kódex civilizácie",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
        subtitleLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.70f, 0.78f));
        titleVBox.AddChild(subtitleLabel);
        headerHBox.AddChild(titleVBox);

        // Search Input
        var searchContainer = new HBoxContainer();
        searchContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        searchContainer.Alignment = BoxContainer.AlignmentMode.Center;

        _searchBar = new LineEdit
        {
            PlaceholderText = "🔍 Hľadať jednotku, budovu, technológiu, surovinu, koncept...",
            CustomMinimumSize = new Vector2(420, 36),
            ClearButtonEnabled = true
        };
        _searchBar.AddThemeFontSizeOverride("font_size", 13);
        _searchBar.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.12f, 0.15f, 0.22f), new Color(0.35f, 0.40f, 0.52f), 1, 6));
        _searchBar.AddThemeStyleboxOverride("focus", CreatePanelStyle(new Color(0.14f, 0.18f, 0.26f), new Color(0.95f, 0.75f, 0.25f), 1.5f, 6));
        _searchBar.TextChanged += OnSearchTextChanged;
        searchContainer.AddChild(_searchBar);
        headerHBox.AddChild(searchContainer);

        // Close Button
        var closeBtn = new Button
        {
            Text = "✕ Zavrieť [Esc]",
            CustomMinimumSize = new Vector2(130, 36)
        };
        closeBtn.AddThemeFontSizeOverride("font_size", 13);
        closeBtn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.45f, 0.18f, 0.18f), new Color(0.75f, 0.30f, 0.30f), 1, 6));
        closeBtn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.60f, 0.22f, 0.22f), Colors.White, 1.5f, 6));
        closeBtn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(new Color(0.35f, 0.12f, 0.12f), new Color(0.75f, 0.30f, 0.30f), 1, 6));
        closeBtn.Pressed += Close;
        headerHBox.AddChild(closeBtn);

        rootVBox.AddChild(headerHBox);

        // ══════════════════════════════════════════════════════════
        // 2. CATEGORY PILL TABS
        // ══════════════════════════════════════════════════════════
        _categoryBar = new HBoxContainer();
        _categoryBar.AddThemeConstantOverride("separation", 8);
        _categoryBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        AddCategoryPill(CivilopediaCategory.Units, "⚔️ Jednotky");
        AddCategoryPill(CivilopediaCategory.Buildings, "🏛️ Budovy");
        AddCategoryPill(CivilopediaCategory.Wonders, "⭐ Divy sveta");
        AddCategoryPill(CivilopediaCategory.Technologies, "🔬 Veda & Technológie");
        AddCategoryPill(CivilopediaCategory.TerrainAndResources, "🌲 Terén & Suroviny");
        AddCategoryPill(CivilopediaCategory.Governments, "⚖️ Formy vlády");
        AddCategoryPill(CivilopediaCategory.Concepts, "💡 Herné koncepty");

        rootVBox.AddChild(_categoryBar);

        // Thin Separator Line
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 4);
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat { BgColor = new Color(0.25f, 0.30f, 0.40f, 0.5f) });
        rootVBox.AddChild(sep);

        // ══════════════════════════════════════════════════════════
        // 3. MAIN SPLIT-VIEW (Sidebar + Detail)
        // ══════════════════════════════════════════════════════════
        var splitHBox = new HBoxContainer();
        splitHBox.AddThemeConstantOverride("separation", 16);
        splitHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        splitHBox.SizeFlagsVertical = SizeFlags.ExpandFill;

        // LEFT SIDEBAR (Width: 330px)
        var sidebarVBox = new VBoxContainer();
        sidebarVBox.CustomMinimumSize = new Vector2(330, 0);
        sidebarVBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        sidebarVBox.AddThemeConstantOverride("separation", 8);

        // Sub-filter chips row
        _subFilterBar = new HBoxContainer();
        _subFilterBar.AddThemeConstantOverride("separation", 4);
        sidebarVBox.AddChild(_subFilterBar);

        // Results count
        _resultsCountLabel = new Label
        {
            Text = "Zoznam položiek",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _resultsCountLabel.AddThemeFontSizeOverride("font_size", 11);
        _resultsCountLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.60f, 0.70f));
        sidebarVBox.AddChild(_resultsCountLabel);

        // Article List ScrollContainer
        var listScroll = new ScrollContainer();
        listScroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        listScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        listScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

        _articlesList = new VBoxContainer();
        _articlesList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _articlesList.AddThemeConstantOverride("separation", 4);
        listScroll.AddChild(_articlesList);
        sidebarVBox.AddChild(listScroll);

        splitHBox.AddChild(sidebarVBox);

        // Vertical Separator
        var vsep = new VSeparator();
        vsep.AddThemeStyleboxOverride("separator", new StyleBoxFlat { BgColor = new Color(0.25f, 0.30f, 0.40f, 0.5f) });
        splitHBox.AddChild(vsep);

        // RIGHT DETAIL VIEW
        _detailScroll = new ScrollContainer();
        _detailScroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _detailScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _detailScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

        _detailContent = new VBoxContainer();
        _detailContent.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _detailContent.AddThemeConstantOverride("separation", 16);
        _detailScroll.AddChild(_detailContent);

        splitHBox.AddChild(_detailScroll);

        rootVBox.AddChild(splitHBox);
    }

    private void AddCategoryPill(CivilopediaCategory cat, string title)
    {
        var btn = new Button
        {
            Text = title,
            CustomMinimumSize = new Vector2(120, 32)
        };
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.Pressed += () => SelectCategory(cat);
        _categoryBar.AddChild(btn);
    }

    private void UpdateCategoryPillStyles()
    {
        foreach (Node child in _categoryBar.GetChildren())
        {
            if (child is Button btn)
            {
                bool isSelected = btn.Text.StartsWith(GetCategoryPrefix(_currentCategory));
                if (isSelected)
                {
                    btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.85f, 0.68f, 0.22f), Colors.White, 1.5f, 16));
                    btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.92f, 0.75f, 0.30f), Colors.White, 2f, 16));
                    btn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(new Color(0.75f, 0.58f, 0.15f), Colors.White, 1.5f, 16));
                    btn.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
                    btn.AddThemeColorOverride("font_hover_color", new Color(0.08f, 0.08f, 0.08f));
                }
                else
                {
                    btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.12f, 0.15f, 0.22f), new Color(0.25f, 0.30f, 0.42f), 1, 16));
                    btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.18f, 0.22f, 0.32f), Colors.White, 1.5f, 16));
                    btn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(new Color(0.10f, 0.12f, 0.18f), new Color(0.25f, 0.30f, 0.42f), 1, 16));
                    btn.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
                    btn.AddThemeColorOverride("font_hover_color", Colors.White);
                }
            }
        }
    }

    private static string GetCategoryPrefix(CivilopediaCategory cat) => cat switch
    {
        CivilopediaCategory.Units => "⚔️",
        CivilopediaCategory.Buildings => "🏛️",
        CivilopediaCategory.Wonders => "⭐",
        CivilopediaCategory.Technologies => "🔬",
        CivilopediaCategory.TerrainAndResources => "🌲",
        CivilopediaCategory.Governments => "⚖️",
        CivilopediaCategory.Concepts => "💡",
        _ => ""
    };

    private void SelectCategory(CivilopediaCategory cat)
    {
        _currentCategory = cat;
        _currentSubFilter = "Všetky";
        UpdateCategoryPillStyles();
        BuildSubFilterChips();
        RefreshArticleList();

        // Select first available article
        var first = GetFilteredArticles().FirstOrDefault();
        if (first != null) DisplayArticle(first);
    }

    private void BuildSubFilterChips()
    {
        foreach (Node child in _subFilterBar.GetChildren()) child.QueueFree();

        List<string> chips = new() { "Všetky" };
        switch (_currentCategory)
        {
            case CivilopediaCategory.Units:
                chips.AddRange(new[] { "Pozemné", "Námorné", "Letecké", "Civilné" });
                break;
            case CivilopediaCategory.Buildings:
                chips.AddRange(new[] { "Základné", "Ekonomika", "Veda", "Kultúra & Obrana" });
                break;
            case CivilopediaCategory.Wonders:
                chips.AddRange(new[] { "Veľké divy", "Malé divy" });
                break;
            case CivilopediaCategory.Technologies:
                chips.AddRange(new[] { "Starovek", "Stredovek", "Priemyselná", "Moderná" });
                break;
            case CivilopediaCategory.TerrainAndResources:
                chips.AddRange(new[] { "Terény", "Strategické", "Luxusné", "Bonusové" });
                break;
        }

        if (chips.Count > 1)
        {
            foreach (var chip in chips)
            {
                var btn = new Button
                {
                    Text = chip,
                    CustomMinimumSize = new Vector2(50, 24)
                };
                btn.AddThemeFontSizeOverride("font_size", 10);
                string currentChip = chip;
                btn.Pressed += () =>
                {
                    _currentSubFilter = currentChip;
                    UpdateSubFilterChipStyles();
                    RefreshArticleList();
                    var first = GetFilteredArticles().FirstOrDefault();
                    if (first != null) DisplayArticle(first);
                };
                _subFilterBar.AddChild(btn);
            }
            UpdateSubFilterChipStyles();
            _subFilterBar.Visible = true;
        }
        else
        {
            _subFilterBar.Visible = false;
        }
    }

    private void UpdateSubFilterChipStyles()
    {
        foreach (Node child in _subFilterBar.GetChildren())
        {
            if (child is Button btn)
            {
                bool active = btn.Text == _currentSubFilter;
                if (active)
                {
                    btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.25f, 0.45f, 0.65f), Colors.White, 1, 10));
                    btn.AddThemeColorOverride("font_color", Colors.White);
                }
                else
                {
                    btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.12f, 0.15f, 0.22f), new Color(0.25f, 0.30f, 0.40f), 1, 10));
                    btn.AddThemeColorOverride("font_color", new Color(0.65f, 0.70f, 0.80f));
                }
            }
        }
    }

    private void OnSearchTextChanged(string text)
    {
        _searchQuery = text.Trim().ToLowerInvariant();
        RefreshArticleList();
        var first = GetFilteredArticles().FirstOrDefault();
        if (first != null) DisplayArticle(first);
    }

    private IEnumerable<CivilopediaArticle> GetFilteredArticles()
    {
        IEnumerable<CivilopediaArticle> query = _articles;

        // If search is empty, filter by category
        if (string.IsNullOrEmpty(_searchQuery))
        {
            query = query.Where(a => a.Category == _currentCategory);

            if (_currentSubFilter != "Všetky")
            {
                query = query.Where(a => a.SubCategory.Contains(_currentSubFilter, StringComparison.OrdinalIgnoreCase) ||
                                         a.Era.Contains(_currentSubFilter, StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            // Global search across all categories
            query = query.Where(a =>
                a.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.SubCategory.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.Era.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.StrategyAdvice.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.HistoricalLore.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
            );
        }

        return query.OrderBy(a => a.Era).ThenBy(a => a.Title);
    }

    private void RefreshArticleList()
    {
        foreach (Node child in _articlesList.GetChildren()) child.QueueFree();

        var filtered = GetFilteredArticles().ToList();
        _resultsCountLabel.Text = $"Nájdených {filtered.Count} položiek";

        foreach (var art in filtered)
        {
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(0, 44),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left
            };

            bool isSelected = _selectedArticle != null && _selectedArticle.Id == art.Id;
            if (isSelected)
            {
                btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.20f, 0.25f, 0.36f), new Color(0.95f, 0.75f, 0.25f), 1.5f, 6));
                btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.24f, 0.30f, 0.42f), new Color(1.0f, 0.85f, 0.35f), 2f, 6));
            }
            else
            {
                btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.10f, 0.12f, 0.18f), new Color(0.18f, 0.22f, 0.30f), 1, 6));
                btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.15f, 0.18f, 0.26f), new Color(0.40f, 0.45f, 0.58f), 1, 6));
            }

            var itemHBox = new HBoxContainer();
            itemHBox.AddThemeConstantOverride("separation", 10);
            itemHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            itemHBox.SizeFlagsVertical = SizeFlags.ShrinkCenter;

            var iconLabel = new Label { Text = art.Icon };
            iconLabel.AddThemeFontSizeOverride("font_size", 18);
            itemHBox.AddChild(iconLabel);

            var textVBox = new VBoxContainer();
            textVBox.AddThemeConstantOverride("separation", 0);
            textVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var titleLbl = new Label { Text = art.Title };
            titleLbl.AddThemeFontSizeOverride("font_size", 13);
            titleLbl.AddThemeColorOverride("font_color", isSelected ? new Color(0.96f, 0.82f, 0.35f) : Colors.White);
            textVBox.AddChild(titleLbl);

            var subLbl = new Label { Text = $"{art.SubCategory} • {art.Era}" };
            subLbl.AddThemeFontSizeOverride("font_size", 10);
            subLbl.AddThemeColorOverride("font_color", new Color(0.55f, 0.60f, 0.70f));
            textVBox.AddChild(subLbl);

            itemHBox.AddChild(textVBox);
            btn.AddChild(itemHBox);

            var captured = art;
            btn.Pressed += () =>
            {
                DisplayArticle(captured);
                RefreshArticleList(); // Refresh active highlight
            };

            _articlesList.AddChild(btn);
        }
    }

    public void NavigateToArticle(string articleId)
    {
        var art = _articles.FirstOrDefault(a => a.Id.Equals(articleId, StringComparison.OrdinalIgnoreCase) ||
                                                a.Title.Equals(articleId, StringComparison.OrdinalIgnoreCase));
        if (art != null)
        {
            _currentCategory = art.Category;
            UpdateCategoryPillStyles();
            BuildSubFilterChips();
            DisplayArticle(art);
            RefreshArticleList();
        }
    }

    private void DisplayArticle(CivilopediaArticle art)
    {
        _selectedArticle = art;
        foreach (Node child in _detailContent.GetChildren()) child.QueueFree();

        // 1. HERO HEADER BANNER
        var heroBox = new PanelContainer();
        heroBox.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.11f, 0.14f, 0.20f), new Color(0.25f, 0.30f, 0.42f), 1, 8));

        var heroMargin = new MarginContainer();
        heroMargin.AddThemeConstantOverride("margin_left", 18);
        heroMargin.AddThemeConstantOverride("margin_right", 18);
        heroMargin.AddThemeConstantOverride("margin_top", 14);
        heroMargin.AddThemeConstantOverride("margin_bottom", 14);

        var heroHBox = new HBoxContainer();
        heroHBox.AddThemeConstantOverride("separation", 18);

        var heroIcon = new Label { Text = art.Icon };
        heroIcon.AddThemeFontSizeOverride("font_size", 42);
        heroIcon.VerticalAlignment = VerticalAlignment.Center;
        heroHBox.AddChild(heroIcon);

        var heroTextVBox = new VBoxContainer();
        heroTextVBox.AddThemeConstantOverride("separation", 4);
        heroTextVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var heroTitle = new Label { Text = art.Title };
        heroTitle.AddThemeFontSizeOverride("font_size", 22);
        heroTitle.AddThemeColorOverride("font_color", new Color(0.96f, 0.82f, 0.35f));
        heroTextVBox.AddChild(heroTitle);

        // Badges row
        var badgesHBox = new HBoxContainer();
        badgesHBox.AddThemeConstantOverride("separation", 8);

        if (!string.IsNullOrEmpty(art.Era))
        {
            badgesHBox.AddChild(CreateBadge(art.Era, GetEraColor(art.Era)));
        }
        if (!string.IsNullOrEmpty(art.SubCategory))
        {
            badgesHBox.AddChild(CreateBadge(art.SubCategory, new Color(0.20f, 0.35f, 0.50f)));
        }

        heroTextVBox.AddChild(badgesHBox);
        heroHBox.AddChild(heroTextVBox);
        heroMargin.AddChild(heroHBox);
        heroBox.AddChild(heroMargin);
        _detailContent.AddChild(heroBox);

        // 2. QUICK STATS MATRIX
        if (art.KeyStats.Count > 0)
        {
            var statsHBox = new HBoxContainer();
            statsHBox.AddThemeConstantOverride("separation", 8);
            statsHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            foreach (var kvp in art.KeyStats)
            {
                var statBox = new PanelContainer();
                statBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                statBox.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.09f, 0.11f, 0.16f), new Color(0.20f, 0.24f, 0.35f), 1, 6));

                var statMargin = new MarginContainer();
                statMargin.AddThemeConstantOverride("margin_left", 8);
                statMargin.AddThemeConstantOverride("margin_right", 8);
                statMargin.AddThemeConstantOverride("margin_top", 8);
                statMargin.AddThemeConstantOverride("margin_bottom", 8);

                var statVBox = new VBoxContainer();
                statVBox.AddThemeConstantOverride("separation", 2);

                var keyLbl = new Label
                {
                    Text = kvp.Key.ToUpperInvariant(),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                keyLbl.AddThemeFontSizeOverride("font_size", 10);
                keyLbl.AddThemeColorOverride("font_color", new Color(0.60f, 0.65f, 0.75f));
                statVBox.AddChild(keyLbl);

                var valLbl = new Label
                {
                    Text = kvp.Value,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                valLbl.AddThemeFontSizeOverride("font_size", 16);
                valLbl.AddThemeColorOverride("font_color", Colors.White);
                statVBox.AddChild(valLbl);

                statMargin.AddChild(statVBox);
                statBox.AddChild(statMargin);
                statsHBox.AddChild(statBox);
            }

            _detailContent.AddChild(statsHBox);
        }

        // 3. ABILITY BADGES
        if (art.AbilityBadges.Count > 0)
        {
            var abilitiesBox = new HFlowContainer();
            abilitiesBox.AddThemeConstantOverride("h_separation", 8);
            abilitiesBox.AddThemeConstantOverride("v_separation", 6);

            foreach (var ab in art.AbilityBadges)
            {
                abilitiesBox.AddChild(CreateBadge(ab, new Color(0.25f, 0.35f, 0.25f), Colors.LightGreen));
            }
            _detailContent.AddChild(abilitiesBox);
        }

        // 4. REQUIREMENTS & CROSS-LINKS ROW
        bool hasReqs = !string.IsNullOrEmpty(art.RequiredTechName) || art.RequiredResources.Count > 0 || !string.IsNullOrEmpty(art.UpgradesToName) || !string.IsNullOrEmpty(art.UpgradesFromName) || art.RelatedArticles.Count > 0;
        if (hasReqs)
        {
            var reqBox = new PanelContainer();
            reqBox.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.09f, 0.12f, 0.17f), new Color(0.22f, 0.28f, 0.38f), 1, 6));

            var reqMargin = new MarginContainer();
            reqMargin.AddThemeConstantOverride("margin_left", 14);
            reqMargin.AddThemeConstantOverride("margin_right", 14);
            reqMargin.AddThemeConstantOverride("margin_top", 10);
            reqMargin.AddThemeConstantOverride("margin_bottom", 10);

            var reqVBox = new VBoxContainer();
            reqVBox.AddThemeConstantOverride("separation", 6);

            var reqTitle = new Label { Text = "🔗 PREDPOKLADY, SUROVINY & VYLEPŠENIA" };
            reqTitle.AddThemeFontSizeOverride("font_size", 12);
            reqTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.25f));
            reqVBox.AddChild(reqTitle);

            var linksHBox = new HBoxContainer();
            linksHBox.AddThemeConstantOverride("separation", 12);

            if (!string.IsNullOrEmpty(art.RequiredTechName))
            {
                var techBtn = CreateLinkButton($"🔬 {art.RequiredTechName}", () => NavigateToArticle(art.RequiredTechId ?? art.RequiredTechName));
                linksHBox.AddChild(techBtn);
            }

            foreach (var res in art.RequiredResources)
            {
                var resBtn = CreateLinkButton($"💎 {res}", () => NavigateToArticle(res));
                linksHBox.AddChild(resBtn);
            }

            if (!string.IsNullOrEmpty(art.UpgradesFromName))
            {
                var upFromBtn = CreateLinkButton($"⬆️ Vylepšenie z: {art.UpgradesFromName}", () => NavigateToArticle(art.UpgradesFromId ?? art.UpgradesFromName));
                linksHBox.AddChild(upFromBtn);
            }

            if (!string.IsNullOrEmpty(art.UpgradesToName))
            {
                var upToBtn = CreateLinkButton($"🚀 Vylepšuje sa na: {art.UpgradesToName}", () => NavigateToArticle(art.UpgradesToId ?? art.UpgradesToName));
                linksHBox.AddChild(upToBtn);
            }

            foreach (var (targetId, label) in art.RelatedArticles)
            {
                var relBtn = CreateLinkButton($"📖 {label}", () => NavigateToArticle(targetId));
                linksHBox.AddChild(relBtn);
            }

            reqVBox.AddChild(linksHBox);
            reqMargin.AddChild(reqVBox);
            reqBox.AddChild(reqMargin);
            _detailContent.AddChild(reqBox);
        }

        // 5. STRATEGY & TACTICAL ADVICE
        if (!string.IsNullOrEmpty(art.StrategyAdvice))
        {
            var stratBox = new VBoxContainer();
            stratBox.AddThemeConstantOverride("separation", 6);

            var stratTitle = new Label { Text = "🎯 Taktická príručka & herné využitie" };
            stratTitle.AddThemeFontSizeOverride("font_size", 14);
            stratTitle.AddThemeColorOverride("font_color", new Color(0.45f, 0.85f, 0.55f));
            stratBox.AddChild(stratTitle);

            var stratText = new RichTextLabel
            {
                Text = art.StrategyAdvice,
                FitContent = true,
                SelectionEnabled = true
            };
            stratText.AddThemeFontSizeOverride("normal_font_size", 13);
            stratText.AddThemeColorOverride("default_color", new Color(0.90f, 0.92f, 0.96f));
            stratBox.AddChild(stratText);

            _detailContent.AddChild(stratBox);
        }

        // 6. HISTORICAL LORE / CIVILOPEDIA ENTRY
        if (!string.IsNullOrEmpty(art.HistoricalLore))
        {
            var loreBox = new VBoxContainer();
            loreBox.AddThemeConstantOverride("separation", 6);

            var loreTitle = new Label { Text = "📜 Historický kódex & encyklopédia" };
            loreTitle.AddThemeFontSizeOverride("font_size", 14);
            loreTitle.AddThemeColorOverride("font_color", new Color(0.96f, 0.78f, 0.35f));
            loreBox.AddChild(loreTitle);

            var loreText = new RichTextLabel
            {
                Text = art.HistoricalLore,
                FitContent = true,
                SelectionEnabled = true
            };
            loreText.AddThemeFontSizeOverride("normal_font_size", 13);
            loreText.AddThemeColorOverride("default_color", new Color(0.82f, 0.85f, 0.90f));
            loreBox.AddChild(loreText);

            _detailContent.AddChild(loreBox);
        }
    }

    private static PanelContainer CreateBadge(string text, Color bg, Color? textCol = null)
    {
        var badge = new PanelContainer();
        badge.AddThemeStyleboxOverride("panel", CreatePanelStyle(bg, bg.Lightened(0.2f), 1, 10));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 3);
        margin.AddThemeConstantOverride("margin_bottom", 3);

        var lbl = new Label { Text = text };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", textCol ?? Colors.White);
        margin.AddChild(lbl);
        badge.AddChild(margin);
        return badge;
    }

    private static Button CreateLinkButton(string text, Action action)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 28)
        };
        btn.AddThemeFontSizeOverride("font_size", 11);
        btn.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color(0.15f, 0.22f, 0.32f), new Color(0.35f, 0.50f, 0.70f), 1, 6));
        btn.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.22f, 0.32f, 0.46f), Colors.White, 1.5f, 6));
        btn.AddThemeStyleboxOverride("pressed", CreatePanelStyle(new Color(0.12f, 0.18f, 0.26f), new Color(0.35f, 0.50f, 0.70f), 1, 6));
        btn.AddThemeColorOverride("font_color", new Color(0.85f, 0.92f, 1.0f));
        btn.Pressed += () =>
        {
            CivGame.Audio.AudioManager.Instance?.PlaySfx("res://assets/sounds/ui_click.wav");
            action();
        };
        return btn;
    }

    private static Color GetEraColor(string era) => era switch
    {
        "Starovek" or "Ancient" => new Color(0.55f, 0.35f, 0.15f),
        "Stredovek" or "MiddleAges" => new Color(0.20f, 0.45f, 0.40f),
        "Priemyselná" or "Industrial" => new Color(0.55f, 0.30f, 0.15f),
        "Moderná" or "Modern" => new Color(0.15f, 0.40f, 0.60f),
        _ => new Color(0.30f, 0.35f, 0.45f)
    };

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

    // ══════════════════════════════════════════════════════════════
    // ARTICLE REPOSITORY BUILDER
    // ══════════════════════════════════════════════════════════════
    private void BuildAllArticles()
    {
        _articles.Clear();
        BuildUnitArticles();
        BuildBuildingArticles();
        BuildWonderArticles();
        BuildTechArticles();
        BuildTerrainAndResourceArticles();
        BuildGovernmentArticles();
        BuildConceptArticles();
    }

    private void BuildUnitArticles()
    {
        foreach (var def in UnitDefinitionRegistry.GetAll())
        {
            var art = new CivilopediaArticle
            {
                Id = def.Type.ToString(),
                Title = def.Name,
                Category = CivilopediaCategory.Units,
                Era = GetEraName(def.Era),
                RequiredTechId = def.RequiredTechId,
                RequiredTechName = GetTechDisplayName(def.RequiredTechId),
                RequiredResources = def.RequiredResourceIds.Select(r => r.ToUpperInvariant()).ToList()
            };

            // Icon & SubCategory
            art.SubCategory = def.Domain switch
            {
                UnitDomain.Sea => "Námorná jednotka",
                UnitDomain.Air => "Letecká jednotka",
                _ => def.ShieldCost == 0 || def.AttackStrength == 0 && def.DefenseStrength == 0 ? "Civilná jednotka" : "Pozemná jednotka"
            };

            art.Icon = def.Domain switch
            {
                UnitDomain.Sea => def.IsStealth ? "🌊" : "⚓",
                UnitDomain.Air => "✈️",
                _ => def.Type switch
                {
                    UnitType.Settler => "🏛️",
                    UnitType.Worker => "🔨",
                    UnitType.Explorer => "🧭",
                    UnitType.Archer or UnitType.Bowman => "🏹",
                    UnitType.Spearman or UnitType.Pikeman => "🛡️",
                    UnitType.Horseman or UnitType.Knight or UnitType.Cavalry => "🐎",
                    UnitType.Catapult or UnitType.Cannon or UnitType.Artillery => "🎯",
                    UnitType.Tank or UnitType.ModernArmor => "🚜",
                    _ => "⚔️"
                }
            };

            // Key Stats
            art.KeyStats["Útok"] = def.AttackStrength.ToString();
            art.KeyStats["Obrana"] = def.DefenseStrength.ToString();
            art.KeyStats["Pohyb"] = def.Movement.ToString("0.#");
            art.KeyStats["Cena"] = $"{def.ShieldCost} 🔨";
            if (def.BombardStrength > 0)
            {
                art.KeyStats["Bombard"] = $"{def.BombardStrength} (D:{def.BombardRange})";
            }
            if (def.TransportCapacity > 0)
            {
                art.KeyStats["Kapacita"] = $"{def.TransportCapacity} j.";
            }
            if (def.OperationalRange > 0)
            {
                art.KeyStats["Dolet"] = $"{def.OperationalRange} pol.";
            }

            // Ability Badges
            if (def.IsStealth)
                art.AbilityBadges.Add("🌊 Hlbinný stealth (Neviditeľná pre bežné lode)");
            if (def.DetectsStealth)
                art.AbilityBadges.Add("📡 ASW Sonar (Detekuje ponorky do 2 políčok)");
            if (def.HasHiddenNationality)
                art.AbilityBadges.Add("🏴‍☠️ Korzár (Skrytá národnosť - útok bez vyhlásenia vojny)");
            if (def.IsStealth && def.Domain == UnitDomain.Sea)
                art.AbilityBadges.Add("🛡️ Núdzový ponor (50% šanca na únik pri smrteľnom zásahu)");
            if (def.Type == UnitType.Tank || def.Type == UnitType.Panzer || def.Type == UnitType.ModernArmor || def.Type == UnitType.MechInfantry || def.Type == UnitType.Army)
                art.AbilityBadges.Add("⚡ Bleskový útok (Blitz - viac útokov za ťah)");
            if (def.Domain == UnitDomain.Sea || def.Domain == UnitDomain.Air || def.Type == UnitType.CruiseMissile)
                art.AbilityBadges.Add("💥 Smrtiace bombardovanie (Lethal Bombard)");
            if (def.SinksInSeaOcean)
                art.AbilityBadges.Add("⚠️ Riziko potopenia na mori a oceáne");
            if (def.IsUniqueUnit)
                art.AbilityBadges.Add("👑 Unikátna jednotka civilizácie (Spúšťa Zlatý vek)");

            // Upgrade link
            if (def.UpgradesTo.HasValue)
            {
                art.UpgradesToId = def.UpgradesTo.Value.ToString();
                art.UpgradesToName = UnitDefinitionRegistry.Get(def.UpgradesTo.Value).Name;
            }

            // Tactical Advice & Lore
            art.StrategyAdvice = GetUnitTacticalAdvice(def);
            art.HistoricalLore = !string.IsNullOrEmpty(def.Description) ? def.Description : $"{def.Name} zohráva kľúčovú úlohu v arzenáli civilizácií v ére {art.Era}.";

            _articles.Add(art);
        }
    }

    private void BuildBuildingArticles()
    {
        foreach (var b in BuildingRegistry.All.Values.Where(b => !(b is Wonder)))
        {
            var art = new CivilopediaArticle
            {
                Id = b.Id,
                Title = b.Name,
                Category = CivilopediaCategory.Buildings,
                SubCategory = GetBuildingSubCategory(b),
                Icon = GetBuildingIcon(b),
                Era = GetTechEraForBuilding(b.RequiredTechId),
                RequiredTechId = b.RequiredTechId,
                RequiredTechName = GetTechDisplayName(b.RequiredTechId)
            };

            art.KeyStats["Cena"] = $"{b.ProductionCost} 🔨";
            art.KeyStats["Údržba"] = $"{b.MaintenanceCost} 💰/ťah";
            if (b.DefenseBonus > 0) art.KeyStats["Obrana"] = $"+{b.DefenseBonus}%";
            if (b.CommerceMultiplier > 1.0f) art.KeyStats["Obchod"] = $"+{(int)((b.CommerceMultiplier - 1.0f) * 100)}%";

            art.StrategyAdvice = GetBuildingTacticalAdvice(b);
            art.HistoricalLore = b.GetDescription();

            _articles.Add(art);
        }
    }

    private void BuildWonderArticles()
    {
        foreach (var b in BuildingRegistry.All.Values.Where(b => b is Wonder))
        {
            var w = (Wonder)b;
            var art = new CivilopediaArticle
            {
                Id = w.Id,
                Title = w.Name,
                Category = CivilopediaCategory.Wonders,
                SubCategory = w.IsNationalWonder ? "Malý div sveta" : "Veľký div sveta",
                Icon = w.IsNationalWonder ? "🎖️" : "⭐",
                Era = GetTechEraForBuilding(w.RequiredTechId),
                RequiredTechId = w.RequiredTechId,
                RequiredTechName = GetTechDisplayName(w.RequiredTechId)
            };

            art.KeyStats["Cena"] = $"{w.ProductionCost} 🔨";
            art.KeyStats["Kultúra"] = $"+{w.CulturePerTurn} /ťah";
            art.KeyStats["Údržba"] = "Bezplatná";

            if (w.AssociatedTraits.Length > 0)
            {
                foreach (var trait in w.AssociatedTraits)
                {
                    art.AbilityBadges.Add($"✨ Črta civilizácie: {trait}");
                }
            }

            art.StrategyAdvice = GetWonderTacticalAdvice(w);
            art.HistoricalLore = $"{w.Name} patrí medzi najslávnejšie architektonické pamiatky ľudstva, ktoré inšpirujú celé generácie.";

            _articles.Add(art);
        }
    }

    private void BuildTechArticles()
    {
        var tm = new TechManager();
        foreach (var tech in tm.AllTechnologies)
        {
            var art = new CivilopediaArticle
            {
                Id = tech.Id,
                Title = tech.Name,
                Category = CivilopediaCategory.Technologies,
                SubCategory = "Vedecký objav",
                Icon = !string.IsNullOrEmpty(tech.Icon) ? tech.Icon : "🔬",
                Era = GetEraName(tech.Era)
            };

            art.KeyStats["Cena vedy"] = $"{tech.ScienceCost} 💡";
            art.KeyStats["Éra"] = art.Era;

            if (tech.PrerequisiteIds.Length > 0)
            {
                foreach (var pre in tech.PrerequisiteIds)
                {
                    var preTech = tm.AllTechnologies.FirstOrDefault(t => t.Id == pre);
                    art.RelatedArticles.Add((pre, preTech != null ? preTech.Name : pre));
                }
            }

            // Find what units/buildings this tech unlocks
            var unlockedUnits = UnitDefinitionRegistry.GetAll().Where(u => u.RequiredTechId == tech.Id).Select(u => u.Name).ToList();
            var unlockedBuildings = BuildingRegistry.All.Values.Where(b => b.RequiredTechId == tech.Id).Select(b => b.Name).ToList();

            foreach (var u in unlockedUnits) art.AbilityBadges.Add($"⚔️ Odomyká jednotku: {u}");
            foreach (var b in unlockedBuildings) art.AbilityBadges.Add($"🏛️ Odomyká budovu: {b}");

            art.StrategyAdvice = $"Výskum technológie {tech.Name} otvára nové strategické možnosti v ére {art.Era}. Urýchľuje rozvoj armády a mestskej infraštruktúry.";
            art.HistoricalLore = !string.IsNullOrEmpty(tech.Description) ? tech.Description : $"Objav {tech.Name} zásadne zmenil spôsob, akým ľudské civilizácie organizovali svoje hospodárstvo a vojenskú moc.";

            _articles.Add(art);
        }
    }

    private void BuildTerrainAndResourceArticles()
    {
        // 1. Terrains
        foreach (var t in TerrainRegistry.All.Values)
        {
            bool isWater = t.Id == "coast" || t.Id == "sea" || t.Id == "ocean";
            var art = new CivilopediaArticle
            {
                Id = t.Id,
                Title = t.Name,
                Category = CivilopediaCategory.TerrainAndResources,
                SubCategory = "Terén",
                Icon = isWater ? "🌊" : (t.Id == "mountain" ? "⛰️" : (t.Id == "hills" ? "🏔️" : (t.Id == "forest" ? "🌲" : "🌾"))),
                Era = "Príroda"
            };

            art.KeyStats["Jedlo"] = t.BaseYield.Food.ToString();
            art.KeyStats["Produkcia"] = t.BaseYield.Production.ToString();
            art.KeyStats["Obchod"] = t.BaseYield.Commerce.ToString();
            art.KeyStats["Pohyb"] = $"{t.MovementCost} MP";
            if (t.DefenseBonusPercent > 0) art.KeyStats["Obrana"] = $"+{t.DefenseBonusPercent}%";

            bool canIrrigate = t.Id == "grassland" || t.Id == "plains" || t.Id == "floodplains";
            bool canMine = t.Id == "hills" || t.Id == "mountain" || t.Id == "desert" || t.Id == "plains" || t.Id == "grassland";
            bool canRoad = !isWater;

            if (canIrrigate) art.AbilityBadges.Add("💧 Možné zavlažovanie (Farma)");
            if (canMine) art.AbilityBadges.Add("⛏️ Možná ťažba (Baňa)");
            if (canRoad) art.AbilityBadges.Add("🛤️ Možná stavba cesty & železnice");

            art.StrategyAdvice = $"Terén {t.Name} poskytuje základné suroviny pre okolité mestá. Zlepšenia robotníkmi výrazne znásobia jeho potenciál.";
            art.HistoricalLore = $"Geografické podmienky typu {t.Name} od pradávna formovali smerovanie expanzie a poľnohospodárstva ľudských osídlení.";

            _articles.Add(art);
        }

        // 2. Resources
        foreach (var res in ResourceRegistry.All.Values)
        {
            var art = new CivilopediaArticle
            {
                Id = res.Id,
                Title = res.Name,
                Category = CivilopediaCategory.TerrainAndResources,
                SubCategory = res is StrategicResource ? "Strategická surovina" : (res is LuxuryResource ? "Luxusná surovina" : "Bonusová surovina"),
                Icon = res is StrategicResource ? "⛏️" : (res is LuxuryResource ? "💎" : "🌾"),
                Era = "Suroviny"
            };

            art.KeyStats["Bonus Jedlo"] = $"+{res.BonusYield.Food}";
            art.KeyStats["Bonus Štíty"] = $"+{res.BonusYield.Production}";
            art.KeyStats["Bonus Obchod"] = $"+{res.BonusYield.Commerce}";

            if (res is StrategicResource sr)
            {
                if (!string.IsNullOrEmpty(sr.RequiredTechId))
                {
                    art.RequiredTechId = sr.RequiredTechId;
                    art.RequiredTechName = GetTechDisplayName(sr.RequiredTechId);
                }
                art.AbilityBadges.Add("⚠️ Riziko vyčerpania ložiska (1 z 800 ťahov)");
                art.AbilityBadges.Add("🔁 Možnosť 20-ťahového medzinárodného obchodu");
                art.StrategyAdvice = $"Kľúčová surovina pre vojenskú prevahu. Bez kontroly nad {res.Name} nemožno vyrábať najmodernejšie armádne jednotky.";
            }
            else if (res is LuxuryResource)
            {
                art.AbilityBadges.Add("😄 Zvyšuje spokojnosť občanov v ríši");
                art.AbilityBadges.Add("💰 Tržnica násobí efekt luxusných surovín");
                art.StrategyAdvice = $"Luxusné suroviny sú najefektívnejším spôsobom na potlačenie občianskych nepokojov (Civil Disorder) v rastúcich metropolách.";
            }

            art.HistoricalLore = $"Prírodné bohatstvo {res.Name} bolo po stáročia príčinou obchodných vojen i hospodárskeho rozkvetu impérií.";
            _articles.Add(art);
        }
    }

    private void BuildGovernmentArticles()
    {
        var govs = new[]
        {
            (GovernmentType.Despotism, "Despotizmus", "Tyrania a autokratická vláda", "Neobmedzená", "Nútené práce (pop)", "Žiadna", "Základná forma vlády. Žiadny postih na armádu, ale produkčný trest na bohatých políčkach a vysoká korupcia."),
            (GovernmentType.Monarchy, "Monarchia", "Tradičná dedičná vláda kráľa", "Problémová", "Výkup zlatom", "Mierna", "Vynikajúca vláda na vedenie vojen v stredoveku. Podporuje veľkú armádu zadarmo a odstraňuje despotický trest."),
            (GovernmentType.Republic, "Republika", "Vláda voleného senátu a občanov", "Nízka", "Výkup zlatom", "Značná", "Obrovský bonus k obchodu (+1 obchod na každom ťaženom políčku). Vyžaduje opatrnosť pri dlhých vojnách pre vojnovú únavu."),
            (GovernmentType.Democracy, "Demokracia", "Plná vláda ľudu a slobodný trh", "Minimálna", "Výkup zlatom", "Vysoká", "Najvyššia efektivita výskumu a obchodu, robotníci pracujú o 50% rýchlejšie. Dlhá vojna však môže viesť k pádu vlády."),
            (GovernmentType.Communism, "Komunizmus", "Centrálne riadený štát a rovnostárstvo", "Rovnomerná (Communal)", "Nútené práce", "Žiadna", "Všetky mestá majú rovnakú nízku korupciu bez ohľadu na vzdialenosť od hlavného mesta. Ideálna pre obrovské kontinentálne ríše."),
            (GovernmentType.Feudalism, "Feudalizmus", "Vláda lénnych pánov a rytierov", "Problémová", "Nútené práce", "Mierna", "Masívna podpora armády v malých mestách. Ideálna pri expanzii a hustom osídlení."),
            (GovernmentType.Fascism, "Fašizmus", "Totalitná militaristická diktatúra", "Nízka", "Nútené práce", "Žiadna", "Najrýchlejší robotníci (200%), vysoká produkcia, žiadna vojnová únava. Mestá však strácajú časť populácie pri prevrate.")
        };

        foreach (var (type, name, sub, corr, hurry, wear, desc) in govs)
        {
            var art = new CivilopediaArticle
            {
                Id = type.ToString(),
                Title = name,
                Category = CivilopediaCategory.Governments,
                SubCategory = "Štátne zriadenie",
                Icon = "⚖️",
                Era = "Spoločnosť"
            };

            art.KeyStats["Korupcia"] = corr;
            art.KeyStats["Urýchlenie"] = hurry;
            art.KeyStats["Vojnová únava"] = wear;

            art.AbilityBadges.Add($"⚖️ Spôsob urýchlenia: {hurry}");
            art.AbilityBadges.Add($"🛡️ Vojnová únava: {wear}");

            art.StrategyAdvice = desc;
            art.HistoricalLore = $"Systém {name} predstavuje zásadný míľnik vo vývoji politického a hospodárskeho myslenia ľudskej spoločnosti.";

            _articles.Add(art);
        }
    }

    private void BuildConceptArticles()
    {
        var concepts = new[]
        {
            ("naval_combat_asw", "Námorný boj, ponorky & ASW Sonar", "Námorná taktika", "⚓",
             "Ponorky sú neviditeľné pre bežné lode. Pohyb cudzej lode na políčko neviditeľnej ponorky spôsobí v mieri kolíziu (Submarine Bump), odhalí ponorku a zastaví loď bez vojny. Vo vojne vyvolá prepad zo zálohy.\n\n" +
             "• ASW Sonar (Torpédoborce a AEGIS Krížniky) odhaľuje všetky ponorky v okruhu 2 políčok.\n" +
             "• Taktický ústup (Emergency Deep Dive): Brániaca sa ponorka má 50% šancu uniknúť pred loďami bez ASW núdzovým ponorom s 1 HP do bezpečia."),

            ("privateers_hidden_nationality", "Korzárske lode & Skrytá národnosť", "Námorná taktika", "🏴‍☠️",
             "Korzári (Privateers) plávajú pod čiernou neutrálnou vlajkou (Jolly Roger). Môžu napadnúť akékoľvek cudzie plavidlo bez toho, aby vyhlásili vojnu materským štátom.\n\n" +
             "• Prepadávanie cudzích lodí nezruší mierové zmluvy ani 20-ťahové obchodné dohody.\n" +
             "• Boje s korzármi nespôsobujú vojnovú únavu."),

            ("cultural_flips", "Kultúra, hranice & Kultúrny prevrat miest", "Kultúra & Diplomacia", "🎭",
             "Kultúra produkovaná chrámami, knižnicami a divmi rozširuje hranice mesta. Ak má susedné impérium výrazne vyššiu kultúru a vaše pohraničné mesto je ďaleko od hlavného mesta, hrozí mu kultúrny prevrat (Cultural Flip)!\n\n" +
             "• Kultúrny poradca (F5) zobrazuje mieru rizika prevratu (🟢 Bezpečné, 🟡 Varovanie, 🚨 VYSOKÉ RIZIKO).\n" +
             "• Silná vojenská posádka v meste potláča nepokoje a znižuje riziko prevratu."),

            ("trade_agreements_20turns", "Obchodné dohody & 20-ťahové bilaterálne pakty", "Diplomacia & Obchod", "📜",
             "Všetky zmluvy o strategických surovinách, luxusoch, platbách za ťah (GPT) a práve prechodu (Right of Passage) trvajú 20 ťahov.\n\n" +
             "• Obchodný poradca (F2) ponúka prehľad všetkých aktívnych zmlúv s odpočtom zostávajúcich ťahov.\n" +
             "• Porušenie zmluvy pred uplynutím 20 ťahov vážne poškodzuje medzinárodnú reputáciu civilizácie."),

            ("golden_age", "Zlatý vek (Golden Age)", "Ríša & Prosperita", "✨",
             "Zlatý vek trvá 20 ťahov a poskytuje mimoriadny bonus: +1 štít produkcie a +1 zlato na každom políčku, ktoré už generuje aspoň 1 štít alebo zlato!\n\n" +
             "• Spúšťa sa víťazstvom unikátnej jednotky danej civilizácie v boji.\n" +
             "• Alebo dokončením Veľkého divu sveta, ktorý pokrýva obe historické črty vašej civilizácie (napr. Militaristic + Commercial)."),

            ("corruption_waste", "Korupcia, plytvanie & Efektívnosť", "Ekonomika", "💰",
             "Korupcia znižuje komerčné príjmy a plytvanie znižuje produkciu štítov. Zvyšuje sa so vzdialenosťou od hlavného mesta a prekročením optimálneho počtu miest (OCN).\n\n" +
             "• Súd (Courthouse) a Policajná stanica (Police Station) znižujú korupciu v meste o 50%.\n" +
             "• Zakázaný palác (Forbidden Palace) slúži ako druhé administratívne centrum ríše."),

            ("pollution_volcanoes", "Znečistenie, sopky & Globálne otepľovanie", "Životné prostredie", "🌋",
             "Veľké priemyselné mestá a nárast populácie vytvárajú znečistenie (Pollution). Neodstránené znečistenie spôsobuje globálne otepľovanie a mení úrodné lúky na púšte.\n\n" +
             "• Robotníci môžu vyčistiť znečistené políčko za niekoľko ťahov.\n" +
             "• Sopky môžu náhodne explodovať a zničiť vylepšenia v okolitom okruhu."),

            ("railroad_logistics", "Železničná sieť & Moderná logistika", "Infraštruktúra", "🚆",
             "Cesty znižujú pohybové náklady na 1/3 MP. Objav parného pohonu a stavba železníc však prináša neobmedzený pohyb (0 MP)!\n\n" +
             "• Armády sa môžu v okamihu presunúť z jedného konca kontinentu na druhý.\n" +
             "• Železnica pridáva +1 jedlo k zavlaženým farmám a +1 produkciu k baniam.")
        };

        foreach (var (id, title, sub, icon, text) in concepts)
        {
            var art = new CivilopediaArticle
            {
                Id = id,
                Title = title,
                Category = CivilopediaCategory.Concepts,
                SubCategory = sub,
                Icon = icon,
                Era = "Pravidlá hry",
                StrategyAdvice = text,
                HistoricalLore = $"Dôkladné pochopenie tejto hernej mechaniky je kľúčom k dosiahnutiu víťazstva na vyšších obtiažnostiach."
            };

            _articles.Add(art);
        }
    }

    private static string GetEraName(TechEra era) => era switch
    {
        TechEra.Ancient => "Starovek",
        TechEra.MiddleAges => "Stredovek",
        TechEra.Industrial => "Priemyselná",
        TechEra.Modern => "Moderná",
        _ => "Starovek"
    };

    private static string GetTechDisplayName(string? techId)
    {
        if (string.IsNullOrEmpty(techId)) return "";
        var tm = new TechManager();
        var t = tm.AllTechnologies.FirstOrDefault(x => x.Id == techId);
        return t != null ? t.Name : techId;
    }

    private static string GetTechEraForBuilding(string? techId)
    {
        if (string.IsNullOrEmpty(techId)) return "Starovek";
        var tm = new TechManager();
        var t = tm.AllTechnologies.FirstOrDefault(x => x.Id == techId);
        return t != null ? GetEraName(t.Era) : "Starovek";
    }

    private static string GetBuildingSubCategory(Building b)
    {
        if (b.DefenseBonus > 0) return "Obranná stavba";
        if (b.CommerceMultiplier > 1.0f) return "Hospodárska stavba";
        return "Mestská stavba";
    }

    private static string GetBuildingIcon(Building b) => b.Id switch
    {
        "granary" => "🌾",
        "barracks" => "⚔️",
        "temple" => "🕊️",
        "library" => "📚",
        "marketplace" => "💰",
        "aqueduct" => "💧",
        "colosseum" => "🏟️",
        "courthouse" => "⚖️",
        "cathedral" => "⛪",
        "university" => "🎓",
        "bank" => "🏦",
        "harbor" => "⚓",
        "factory" => "🏭",
        "airport" => "✈️",
        "hospital" => "🏥",
        _ => "🏛️"
    };

    private static string GetBuildingTacticalAdvice(Building b) => b.Id switch
    {
        "granary" => "Sýpka uchováva 50% nazhromaždeného jedla po každom raste populácie. Nevyhnutná budova pre mestá s vysokým rastom, ktoré cvičia osadníkov a robotníkov.",
        "barracks" => "Kasárne umožňujú cvičiť skúsených vojakov (Veteran namiesto Regular) a zabezpečujú plné vyliečenie pozemných jednotiek za jediný ťah.",
        "temple" => "Chrám produkuje +2 kultúru a upokojuje 1 nespokojného občana. Kľúčový pre rýchlu expanziu hraníc a potlačenie kultúrneho prevratu.",
        "library" => "Knižnica zvyšuje výskumný výkon mesta o +50% a generuje +3 kultúru za ťah.",
        "marketplace" => "Tržnica zvyšuje komerčné daňové príjmy o +50% a výrazne zvyšuje účinok luxusných surovín na šťastie občanov.",
        "aqueduct" => "Akvadukt umožňuje mestu rásť nad populáciu 6 (až do veľkosti 12). Mestá pri rieke ho nepotrebujú.",
        "hospital" => "Nemocnica umožňuje rast metropoly nad populáciu 12.",
        "courthouse" => "Súd znižuje korupciu a plytvanie v meste o 50%, čím vracia vzdialeným mestám hospodársku silu.",
        _ => $"{b.Name} poskytuje mestu trvalé civilizačné a hospodárske výhody."
    };

    private static string GetWonderTacticalAdvice(Wonder w) => w.Id switch
    {
        "pyramids" => "Poskytuje bezplatnú sýpku v každom meste na kontinente. Jeden z najsilnejších divov v hre pre raketový rast populácie v staroveku.",
        "great_library" => "Udeľuje každú technológiu, ktorú objavili aspoň 2 iné známe civilizácie. Umožňuje nulové výdavky na vedu a masívnu akumuláciu zlata.",
        "leonardo_workshop" => "Znižuje náklady na vylepšovanie všetkých vojenských jednotiek o 50%. Šetrí tisíce zlatých pri prechode z rytierov na kavalériu či pechotu.",
        "hoover_dam" => "Bezplatná vodná elektráreň vo všetkých mestách na kontinente. Zvyšuje produkciu o +50% bez znečistenia z uhoľných elektrární.",
        "theory_evolution" => "Okamžite udelí 2 bezplatné technológie podľa vlastného výberu.",
        "heroic_epic" => "Zvyšuje šancu na zrodenie Veľkého vojenského vodcu z elitných víťazstiev o +50% a pridáva +25% útočný bonus.",
        _ => $"{w.Name} je mimoriadny div, ktorý natrvalo zmení priebeh hry v prospech staviteľa."
    };

    private static string GetUnitTacticalAdvice(UnitDefinition def)
    {
        if (def.IsStealth)
            return "Ponorka je neviditeľná pre nepriateľské povrchové lode. Využívajte ju na prepady nepriateľských konvojov a transportov. Pozor na torpédoborce s aktívnym ASW Sonarom!";
        if (def.DetectsStealth)
            return "Torpédoborec je vybavený aktívnym ASW Sonarom, ktorý lokalizuje nepriateľské ponorky do vzdialenosti 2 políčok a zabraňuje im v taktickom ústupe.";
        if (def.HasHiddenNationality)
            return "Korzár môže napádať cudzie plavidlá bez vyhlásenia vojny. Ideálny na pirátske narúšanie námorného obchodu bez diplomtických následkov.";
        if (def.BombardStrength > 0)
            return "Delostrelecká jednotka: Ostreľujte nepriateľské armády, mestské hradby a infraštruktúru z bezpečnej vzdialenosti pred začatím priameho útoku.";
        if (def.Domain == UnitDomain.Sea)
            return "Námorné plavidlo určené na kontrolu morí, ochranu transportov a ostreľovanie pobrežných cieľov.";
        if (def.Domain == UnitDomain.Air)
            return "Letecká jednotka poskytujúca strategický dosah na nepriateľské zázemie, prieskum a ničenie infraštruktúry.";
        if (def.Movement >= 2)
            return "Rýchla manévrová jednotka: Má schopnosť ustúpiť pred pomalšími obrancami a vďaka vysokej rýchlosti dokáže hlboko preniknúť za nepriateľské línie.";
        return "Štandardná armádna jednotka tvoriaca chrbticu vojenských zborov v príslušnej ére.";
    }
}
